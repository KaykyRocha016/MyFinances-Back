using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinances.Api.Data;
using MyFinances.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinances.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] int householdId, [FromQuery] int? cycleId)
    {
        // 1. Resolve which cycle we are querying
        int resolvedCycleId = 0;
        string cycleName = "Cycle";
        bool hasActiveCycle = false;

        if (cycleId.HasValue)
        {
            var cycle = await _context.Cycles.FindAsync(cycleId.Value);
            if (cycle != null && cycle.HouseholdId == householdId)
            {
                resolvedCycleId = cycle.Id;
                cycleName = cycle.Name;
                hasActiveCycle = cycle.IsActive;
            }
        }
        else
        {
            var activeCycle = await _context.Cycles
                .FirstOrDefaultAsync(c => c.HouseholdId == householdId && c.IsActive);
            
            if (activeCycle != null)
            {
                resolvedCycleId = activeCycle.Id;
                cycleName = activeCycle.Name;
                hasActiveCycle = true;
            }
        }

        // 2. Fetch users in this specific household
        var users = await _context.Users
            .Where(u => u.HouseholdId == householdId)
            .ToListAsync();

        if (resolvedCycleId == 0)
        {
            // If no cycle found/active, return empty dashboard
            return Ok(new DashboardDto(
                0m,
                users.Select(u => new UserBalanceDto(u.Id, u.Name, 0m, 0m, 0m)).ToList(),
                "Nenhum ciclo ativo cadastrado. Crie um ciclo para iniciar."
            ));
        }

        // 3. Fetch expenses and splits belonging to the resolved cycle
        var expenses = await _context.Expenses
            .Where(d => d.CycleId == resolvedCycleId)
            .ToListAsync();

        var expenseIds = expenses.Select(d => d.Id).ToList();

        var splits = await _context.ExpenseSplits
            .Where(r => expenseIds.Contains(r.ExpenseId))
            .ToListAsync();

        decimal totalOverall = expenses.Sum(d => d.Amount);

        var balances = new List<UserBalanceDto>();

        foreach (var user in users)
        {
            decimal totalPaid = expenses.Where(d => d.UserId == user.Id).Sum(d => d.Amount);
            decimal totalResponsibility = splits.Where(r => r.UserId == user.Id).Sum(r => r.Amount);
            decimal netBalance = totalPaid - totalResponsibility;

            balances.Add(new UserBalanceDto(
                user.Id,
                user.Name,
                totalPaid,
                totalResponsibility,
                netBalance
            ));
        }

        string statusBalance = "Resumo de saldos calculado.";
        if (balances.Count == 2)
        {
            var user1 = balances[0];
            var user2 = balances[1];

            if (user1.NetBalance > 0)
            {
                statusBalance = $"{user2.Name} deve {Math.Abs(user1.NetBalance):C} para {user1.Name}";
            }
            else if (user2.NetBalance > 0)
            {
                statusBalance = $"{user1.Name} deve {Math.Abs(user2.NetBalance):C} para {user2.Name}";
            }
            else
            {
                statusBalance = "Os saldos estão totalmente equilibrados!";
            }
        }
        else if (balances.Count > 2)
        {
            // Summarize for multi-user households
            var debtors = balances.Where(b => b.NetBalance < 0).ToList();
            var creditors = balances.Where(b => b.NetBalance > 0).ToList();
            
            if (debtors.Any() && creditors.Any())
            {
                var mainCreditor = creditors.OrderByDescending(c => c.NetBalance).First();
                var totalOwed = creditors.Sum(c => c.NetBalance);
                statusBalance = $"{mainCreditor.Name} e outros têm {totalOwed:C} a receber no total.";
            }
            else
            {
                statusBalance = "Os saldos estão equilibrados!";
            }
        }
        else
        {
            statusBalance = "Adicione mais membros ao núcleo para ver os acertos.";
        }

        if (!hasActiveCycle)
        {
            statusBalance += " (Ciclo Encerrado)";
        }

        var dashboard = new DashboardDto(
            totalOverall,
            balances,
            statusBalance
        );

        return Ok(dashboard);
    }
}
