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
    public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] int nucleoId, [FromQuery] int? cicloId)
    {
        // 1. Resolve which cycle we are querying
        int resolvedCicloId = 0;
        string cycleName = "Ciclo";
        bool hasActiveCycle = false;

        if (cicloId.HasValue)
        {
            var ciclo = await _context.Ciclos.FindAsync(cicloId.Value);
            if (ciclo != null && ciclo.NucleoId == nucleoId)
            {
                resolvedCicloId = ciclo.Id;
                cycleName = ciclo.Nome;
                hasActiveCycle = ciclo.Ativo;
            }
        }
        else
        {
            var activeCiclo = await _context.Ciclos
                .FirstOrDefaultAsync(c => c.NucleoId == nucleoId && c.Ativo);
            
            if (activeCiclo != null)
            {
                resolvedCicloId = activeCiclo.Id;
                cycleName = activeCiclo.Nome;
                hasActiveCycle = true;
            }
        }

        // 2. Fetch users in this specific nucleo
        var users = await _context.Usuarios
            .Where(u => u.NucleoId == nucleoId)
            .ToListAsync();

        if (resolvedCicloId == 0)
        {
            // If no cycle found/active, return empty dashboard
            return Ok(new DashboardDto(
                0m,
                users.Select(u => new UserBalanceDto(u.Id, u.Nome, 0m, 0m, 0m)).ToList(),
                "Nenhum ciclo ativo cadastrado. Crie um ciclo para iniciar."
            ));
        }

        // 3. Fetch expenses and rateios belonging to the resolved cycle
        var despesas = await _context.Despesas
            .Where(d => d.CicloId == resolvedCicloId)
            .ToListAsync();

        var despesaIds = despesas.Select(d => d.Id).ToList();

        var rateios = await _context.DespesasRateio
            .Where(r => despesaIds.Contains(r.DespesaId))
            .ToListAsync();

        decimal totalGeral = despesas.Sum(d => d.Valor);

        var balances = new List<UserBalanceDto>();

        foreach (var user in users)
        {
            decimal totalPago = despesas.Where(d => d.UsuarioId == user.Id).Sum(d => d.Valor);
            decimal totalResponsabilidade = rateios.Where(r => r.UsuarioId == user.Id).Sum(r => r.Valor);
            decimal saldoLiquido = totalPago - totalResponsabilidade;

            balances.Add(new UserBalanceDto(
                user.Id,
                user.Nome,
                totalPago,
                totalResponsabilidade,
                saldoLiquido
            ));
        }

        string statusBalance = "Resumo de saldos calculado.";
        if (balances.Count == 2)
        {
            var user1 = balances[0];
            var user2 = balances[1];

            if (user1.SaldoLiquido > 0)
            {
                statusBalance = $"{user2.Nome} deve {Math.Abs(user1.SaldoLiquido):C} para {user1.Nome}";
            }
            else if (user2.SaldoLiquido > 0)
            {
                statusBalance = $"{user1.Nome} deve {Math.Abs(user2.SaldoLiquido):C} para {user2.Nome}";
            }
            else
            {
                statusBalance = "Os saldos estão totalmente equilibrados!";
            }
        }
        else if (balances.Count > 2)
        {
            // Summarize for multi-user nucleos
            var debtors = balances.Where(b => b.SaldoLiquido < 0).ToList();
            var creditors = balances.Where(b => b.SaldoLiquido > 0).ToList();
            
            if (debtors.Any() && creditors.Any())
            {
                var mainCreditor = creditors.OrderByDescending(c => c.SaldoLiquido).First();
                var totalOwed = creditors.Sum(c => c.SaldoLiquido);
                statusBalance = $"{mainCreditor.Nome} e outros têm {totalOwed:C} a receber no total.";
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
            totalGeral,
            balances,
            statusBalance
        );

        return Ok(dashboard);
    }
}
