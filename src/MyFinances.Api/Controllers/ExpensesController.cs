using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinances.Api.Data;
using MyFinances.Api.Dtos;
using MyFinances.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinances.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
        [FromQuery] int? cycleId,
        [FromQuery] int? userId,
        [FromQuery] int? categoryId)
    {
        IQueryable<Expense> query = _context.Expenses
            .Include(d => d.User)
            .Include(d => d.Category)
            .Include(d => d.Splits)
                .ThenInclude(r => r.User);

        if (cycleId.HasValue)
        {
            query = query.Where(d => d.CycleId == cycleId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId.Value || d.Splits.Any(r => r.UserId == userId.Value));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == categoryId.Value);
        }

        var expenses = await query
            .OrderByDescending(d => d.Date)
            .ToListAsync();

        var result = expenses.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(CreateExpenseRequest request)
    {
        var payer = await _context.Users.FindAsync(request.UserId);
        if (payer == null)
        {
            return BadRequest("Usuário pagador não encontrado.");
        }

        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Categoria não encontrada.");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("O valor da despesa deve ser maior que zero.");
        }

        int totalInstallments = request.TotalInstallments > 0 ? request.TotalInstallments : 1;

        var activeCycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.HouseholdId == payer.HouseholdId && c.IsActive);

        if (activeCycle == null)
        {
            return BadRequest("Não há nenhum ciclo de gastos ativo para o núcleo deste usuário. Abra um ciclo de gastos primeiro.");
        }

        Guid? installmentGroupId = totalInstallments > 1 ? Guid.NewGuid() : null;
        decimal baseInstallmentAmount = Math.Round(request.Amount / totalInstallments, 2);

        var createdExpenses = new List<Expense>();
        Cycle currentCycle = activeCycle;

        var allUsers = await _context.Users
            .Where(u => u.HouseholdId == payer.HouseholdId)
            .ToListAsync();

        for (int i = 1; i <= totalInstallments; i++)
        {
            // The last installment gets the remainder to avoid rounding differences
            decimal installmentAmount = (i == totalInstallments)
                ? request.Amount - (baseInstallmentAmount * (totalInstallments - 1))
                : baseInstallmentAmount;

            var targetDate = request.Date.AddMonths(i - 1).ToUniversalTime();

            Cycle cycleForInstallment;
            if (i == 1)
            {
                cycleForInstallment = activeCycle;
            }
            else
            {
                cycleForInstallment = await GetOrCreateCycleForInstallment(payer.HouseholdId, currentCycle, targetDate);
                currentCycle = cycleForInstallment;
            }

            var expense = new Expense
            {
                Description = totalInstallments > 1 ? $"{request.Description} ({i}/{totalInstallments})" : request.Description,
                Amount = installmentAmount,
                Date = targetDate,
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                CycleId = cycleForInstallment.Id,
                InstallmentNumber = i,
                TotalInstallments = totalInstallments,
                InstallmentGroupId = installmentGroupId
            };

            var splits = new List<ExpenseSplit>();

            switch (category.DivisionType.ToUpper())
            {
                case "PROPORCIONAL":
                    if (request.Splits != null && request.Splits.Any())
                    {
                        // Calculate proportional split per user for this installment amount
                        decimal accumulatedProp = 0m;
                        for (int j = 0; j < request.Splits.Count; j++)
                        {
                            var splitDto = request.Splits[j];
                            decimal userPortion;
                            if (j == request.Splits.Count - 1)
                            {
                                userPortion = installmentAmount - accumulatedProp;
                            }
                            else
                            {
                                userPortion = request.Amount > 0 
                                    ? Math.Round((splitDto.Amount / request.Amount) * installmentAmount, 2)
                                    : 0m;
                                accumulatedProp += userPortion;
                            }

                            splits.Add(new ExpenseSplit
                            {
                                UserId = splitDto.UserId,
                                Amount = userPortion
                            });
                        }
                    }
                    else
                    {
                        var userIncomes = allUsers.Select(u => (u.Id, u.Income)).ToList();
                        var proportionalSplits = DistributeProportionally(installmentAmount, userIncomes);
                        foreach (var split in proportionalSplits)
                        {
                            splits.Add(new ExpenseSplit
                            {
                                UserId = split.UserId,
                                Amount = split.Value
                            });
                        }
                    }
                    break;

                case "INDIVIDUAL":
                    splits.Add(new ExpenseSplit
                    {
                        UserId = request.UserId,
                        Amount = installmentAmount
                    });
                    break;

                case "CUSTOMIZADO":
                    if (request.Splits == null || !request.Splits.Any())
                    {
                        return BadRequest("Para despesas com divisão exata/customizada, você deve informar o rateio na requisição.");
                    }

                    decimal accumulatedCustom = 0m;
                    for (int j = 0; j < request.Splits.Count; j++)
                    {
                        var splitDto = request.Splits[j];
                        decimal userPortion;
                        if (j == request.Splits.Count - 1)
                        {
                            userPortion = installmentAmount - accumulatedCustom;
                        }
                        else
                        {
                            userPortion = request.Amount > 0 
                                ? Math.Round((splitDto.Amount / request.Amount) * installmentAmount, 2)
                                : 0m;
                            accumulatedCustom += userPortion;
                        }

                        splits.Add(new ExpenseSplit
                        {
                            UserId = splitDto.UserId,
                            Amount = userPortion
                        });
                    }
                    break;

                default:
                    return BadRequest($"Tipo de divisão desconhecido: {category.DivisionType}");
            }

            expense.Splits = splits;
            _context.Expenses.Add(expense);
            createdExpenses.Add(expense);
        }

        await _context.SaveChangesAsync();

        var firstCreatedId = createdExpenses[0].Id;
        var createdExpense = await _context.Expenses
            .Include(d => d.User)
            .Include(d => d.Category)
            .Include(d => d.Splits)
                .ThenInclude(r => r.User)
            .FirstAsync(d => d.Id == firstCreatedId);

        return CreatedAtAction(nameof(GetExpenses), MapToDto(createdExpense));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, UpdateExpenseRequest request)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound("Despesa não encontrada.");
        }

        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Categoria não encontrada.");
        }

        if (request.UpdateAllInstallments && expense.InstallmentGroupId.HasValue)
        {
            var allInstallments = await _context.Expenses
                .Where(e => e.InstallmentGroupId == expense.InstallmentGroupId.Value)
                .ToListAsync();

            foreach (var inst in allInstallments)
            {
                // Preserve (i/N) suffix if present
                if (inst.TotalInstallments > 1)
                {
                    inst.Description = $"{request.Description} ({inst.InstallmentNumber}/{inst.TotalInstallments})";
                }
                else
                {
                    inst.Description = request.Description;
                }
                inst.CategoryId = request.CategoryId;
            }
        }
        else
        {
            expense.Description = request.Description;
            expense.Amount = request.Amount;
            expense.CategoryId = request.CategoryId;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id, [FromQuery] bool deleteAllInstallments = false)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        if (deleteAllInstallments && expense.InstallmentGroupId.HasValue)
        {
            var allInstallments = await _context.Expenses
                .Where(e => e.InstallmentGroupId == expense.InstallmentGroupId.Value)
                .ToListAsync();

            _context.Expenses.RemoveRange(allInstallments);
        }
        else
        {
            _context.Expenses.Remove(expense);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Cycle> GetOrCreateCycleForInstallment(int householdId, Cycle previousCycle, DateTime targetDate)
    {
        // 1. Check if there is an existing cycle after previousCycle
        var nextCycle = await _context.Cycles
            .Where(c => c.HouseholdId == householdId && c.StartDate > previousCycle.StartDate)
            .OrderBy(c => c.StartDate)
            .FirstOrDefaultAsync();

        if (nextCycle != null)
        {
            return nextCycle;
        }

        // 2. If no next cycle exists, automatically create a new cycle
        var newStart = previousCycle.EndDate > targetDate ? previousCycle.EndDate : targetDate;
        var newEnd = newStart.AddMonths(1);
        string monthLabel = targetDate.ToString("MMM yyyy");

        var createdCycle = new Cycle
        {
            Name = $"Ciclo {monthLabel}",
            StartDate = newStart,
            EndDate = newEnd,
            IsActive = false,
            HouseholdId = householdId
        };

        _context.Cycles.Add(createdCycle);
        await _context.SaveChangesAsync();

        return createdCycle;
    }

    private static ExpenseDto MapToDto(Expense d)
    {
        return new ExpenseDto(
            d.Id,
            d.Description,
            d.Amount,
            d.Date,
            new UserDto(d.User!.Id, d.User.Name, d.User.Income, d.User.HouseholdId),
            new CategoryDto(d.Category!.Id, d.Category.Name, d.Category.DivisionType),
            d.Splits.Select(r => new ExpenseSplitDto(
                r.Id,
                r.UserId,
                r.User!.Name,
                r.Amount
            )).ToList(),
            d.CycleId,
            d.InstallmentNumber,
            d.TotalInstallments,
            d.InstallmentGroupId
        );
    }

    private static List<(int UserId, decimal Value)> DistributeProportionally(decimal total, List<(int UserId, decimal Income)> users)
    {
        if (!users.Any()) return new List<(int UserId, decimal Value)>();
        
        var totalIncome = users.Sum(u => u.Income);
        if (totalIncome == 0)
        {
            return DistributeEqually(total, users.Select(u => u.UserId).ToList());
        }

        var splits = new List<(int UserId, decimal Value)>();
        decimal accumulated = 0;
        
        for (int i = 0; i < users.Count; i++)
        {
            decimal splitVal;
            if (i == users.Count - 1)
            {
                splitVal = total - accumulated;
            }
            else
            {
                splitVal = Math.Round(total * (users[i].Income / totalIncome), 2);
                accumulated += splitVal;
            }
            splits.Add((users[i].UserId, splitVal));
        }
        
        return splits;
    }

    private static List<(int UserId, decimal Value)> DistributeEqually(decimal total, List<int> userIds)
    {
        if (!userIds.Any()) return new List<(int UserId, decimal Value)>();
        
        var splits = new List<(int UserId, decimal Value)>();
        decimal share = Math.Round(total / userIds.Count, 2);
        decimal accumulated = 0;
        
        for (int i = 0; i < userIds.Count; i++)
        {
            decimal splitVal;
            if (i == userIds.Count - 1)
            {
                splitVal = total - accumulated;
            }
            else
            {
                splitVal = share;
                accumulated += splitVal;
            }
            splits.Add((userIds[i], splitVal));
        }
        
        return splits;
    }
}
