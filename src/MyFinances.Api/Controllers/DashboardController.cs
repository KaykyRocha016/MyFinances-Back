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
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var users = await _context.Usuarios.ToListAsync();
        var despesas = await _context.Despesas.ToListAsync();
        var rateios = await _context.DespesasRateio.ToListAsync();

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

        var dashboard = new DashboardDto(
            totalGeral,
            balances,
            statusBalance
        );

        return Ok(dashboard);
    }
}
