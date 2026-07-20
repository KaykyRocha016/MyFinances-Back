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

        var activeCycle = await _context.Cycles
            .FirstOrDefaultAsync(c => c.HouseholdId == payer.HouseholdId && c.IsActive);

        if (activeCycle == null)
        {
            return BadRequest("Não há nenhum ciclo de gastos ativo para o núcleo deste usuário. Abra um ciclo de gastos primeiro.");
        }

        var expense = new Expense
        {
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date.ToUniversalTime(),
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            CycleId = activeCycle.Id
        };

        var splits = new List<ExpenseSplit>();

        switch (category.DivisionType.ToUpper())
        {
            case "PROPORCIONAL":
                if (request.Splits != null && request.Splits.Any())
                {
                    var sumSplitsProp = request.Splits.Sum(r => r.Amount);
                    if (Math.Abs(sumSplitsProp - request.Amount) > 0.02m)
                    {
                        return BadRequest($"A soma dos rateios personalizados ({sumSplitsProp}) deve ser exatamente igual ao valor total da despesa ({request.Amount}).");
                    }

                    var splitUserIdsProp = request.Splits.Select(r => r.UserId).Distinct().ToList();
                    var existingUsersCountProp = await _context.Users.CountAsync(u => splitUserIdsProp.Contains(u.Id));
                    if (existingUsersCountProp != splitUserIdsProp.Count)
                    {
                        return BadRequest("Um ou mais usuários informados no rateio não foram encontrados.");
                    }

                    foreach (var splitDto in request.Splits)
                    {
                        splits.Add(new ExpenseSplit
                        {
                            UserId = splitDto.UserId,
                            Amount = splitDto.Amount
                        });
                    }
                }
                else
                {
                    var allUsers = await _context.Users
                        .Where(u => u.HouseholdId == payer.HouseholdId)
                        .ToListAsync();
                        
                    var userIncomes = allUsers.Select(u => (u.Id, u.Income)).ToList();
                    var proportionalSplits = DistributeProportionally(request.Amount, userIncomes);
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
                    Amount = request.Amount
                });
                break;

            case "CUSTOMIZADO":
                if (request.Splits == null || !request.Splits.Any())
                {
                    return BadRequest("Para despesas com divisão exata/customizada, você deve informar o rateio na requisição.");
                }

                var sumSplits = request.Splits.Sum(r => r.Amount);
                if (Math.Abs(sumSplits - request.Amount) > 0.02m)
                {
                    return BadRequest($"A soma dos rateios ({sumSplits}) deve ser exatamente igual ao valor total da despesa ({request.Amount}).");
                }

                var splitUserIds = request.Splits.Select(r => r.UserId).Distinct().ToList();
                var existingUsersCount = await _context.Users.CountAsync(u => splitUserIds.Contains(u.Id));
                if (existingUsersCount != splitUserIds.Count)
                {
                    return BadRequest("Um ou mais usuários informados no rateio não foram encontrados.");
                }

                foreach (var splitDto in request.Splits)
                {
                    splits.Add(new ExpenseSplit
                    {
                        UserId = splitDto.UserId,
                        Amount = splitDto.Amount
                    });
                }
                break;

            default:
                return BadRequest($"Tipo de divisão desconhecido: {category.DivisionType}");
        }

        expense.Splits = splits;
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        var createdExpense = await _context.Expenses
            .Include(d => d.User)
            .Include(d => d.Category)
            .Include(d => d.Splits)
                .ThenInclude(r => r.User)
            .FirstAsync(d => d.Id == expense.Id);

        return CreatedAtAction(nameof(GetExpenses), MapToDto(createdExpense));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return NoContent();
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
            d.CycleId
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
