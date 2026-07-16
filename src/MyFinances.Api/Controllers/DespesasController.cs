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
public class DespesasController : ControllerBase
{
    private readonly AppDbContext _context;

    public DespesasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DespesaDto>>> GetDespesas()
    {
        var despesas = await _context.Despesas
            .Include(d => d.Usuario)
            .Include(d => d.Categoria)
            .Include(d => d.Rateios)
                .ThenInclude(r => r.Usuario)
            .OrderByDescending(d => d.Data)
            .ToListAsync();

        var result = despesas.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DespesaDto>> CreateDespesa(CreateDespesaRequest request)
    {
        // 1. Validate inputs
        var payer = await _context.Usuarios.FindAsync(request.UsuarioId);
        if (payer == null)
        {
            return BadRequest("Usuário pagador não encontrado.");
        }

        var category = await _context.Categorias.FindAsync(request.CategoriaId);
        if (category == null)
        {
            return BadRequest("Categoria não encontrada.");
        }

        if (request.Valor <= 0)
        {
            return BadRequest("O valor da despesa deve ser maior que zero.");
        }

        // 2. Create the Despesa
        var despesa = new Despesa
        {
            Descricao = request.Descricao,
            Valor = request.Valor,
            Data = request.Data.ToUniversalTime(),
            UsuarioId = request.UsuarioId,
            CategoriaId = request.CategoriaId
        };

        // 3. Calculate and populate DespesasRateio
        var rateios = new List<DespesaRateio>();

        switch (category.TipoDivisao.ToUpper())
        {
            case "PROPORCIONAL":
                var allUsers = await _context.Usuarios.ToListAsync();
                var userIncomes = allUsers.Select(u => (u.Id, u.Renda)).ToList();
                var proportionalSplits = DistributeProportionally(request.Valor, userIncomes);
                foreach (var split in proportionalSplits)
                {
                    rateios.Add(new DespesaRateio
                    {
                        UsuarioId = split.UserId,
                        Valor = split.Value
                    });
                }
                break;

            case "INDIVIDUAL":
                // 100% of expense belongs to the payer
                rateios.Add(new DespesaRateio
                {
                    UsuarioId = request.UsuarioId,
                    Valor = request.Valor
                });
                break;

            case "CUSTOMIZADO":
                if (request.Rateios == null || !request.Rateios.Any())
                {
                    return BadRequest("Para despesas com divisão exata/customizada, você deve informar o rateio na requisição.");
                }

                var sumSplits = request.Rateios.Sum(r => r.Valor);
                if (Math.Abs(sumSplits - request.Valor) > 0.01m)
                {
                    return BadRequest($"A soma dos rateios ({sumSplits}) deve ser exatamente igual ao valor total da despesa ({request.Valor}).");
                }

                // Verify that all users in rateio exist
                var rateioUserIds = request.Rateios.Select(r => r.UsuarioId).Distinct().ToList();
                var existingUsersCount = await _context.Usuarios.CountAsync(u => rateioUserIds.Contains(u.Id));
                if (existingUsersCount != rateioUserIds.Count)
                {
                    return BadRequest("Um ou mais usuários informados no rateio não foram encontrados.");
                }

                foreach (var splitDto in request.Rateios)
                {
                    rateios.Add(new DespesaRateio
                    {
                        UsuarioId = splitDto.UsuarioId,
                        Valor = splitDto.Valor
                    });
                }
                break;

            default:
                return BadRequest($"Tipo de divisão desconhecido: {category.TipoDivisao}");
        }

        despesa.Rateios = rateios;
        _context.Despesas.Add(despesa);
        await _context.SaveChangesAsync();

        // Fetch again to ensure everything is populated correctly for the response
        var createdDespesa = await _context.Despesas
            .Include(d => d.Usuario)
            .Include(d => d.Categoria)
            .Include(d => d.Rateios)
                .ThenInclude(r => r.Usuario)
            .FirstAsync(d => d.Id == despesa.Id);

        return CreatedAtAction(nameof(GetDespesas), MapToDto(createdDespesa));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDespesa(int id)
    {
        var despesa = await _context.Despesas.FindAsync(id);
        if (despesa == null)
        {
            return NotFound();
        }

        _context.Despesas.Remove(despesa);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static DespesaDto MapToDto(Despesa d)
    {
        return new DespesaDto(
            d.Id,
            d.Descricao,
            d.Valor,
            d.Data,
            new UsuarioDto(d.Usuario!.Id, d.Usuario.Nome, d.Usuario.Renda),
            new CategoriaDto(d.Categoria!.Id, d.Categoria.Nome, d.Categoria.TipoDivisao),
            d.Rateios.Select(r => new DespesaRateioDto(
                r.Id,
                r.UsuarioId,
                r.Usuario!.Nome,
                r.Valor
            )).ToList()
        );
    }

    private static List<(int UserId, decimal Value)> DistributeProportionally(decimal total, List<(int UserId, decimal Income)> users)
    {
        if (!users.Any()) return new List<(int UserId, decimal Value)>();
        
        var totalIncome = users.Sum(u => u.Income);
        if (totalIncome == 0)
        {
            // Split equally if everyone has 0 income
            return DistributeEqually(total, users.Select(u => u.UserId).ToList());
        }

        var splits = new List<(int UserId, decimal Value)>();
        decimal accumulated = 0;
        
        for (int i = 0; i < users.Count; i++)
        {
            decimal splitVal;
            if (i == users.Count - 1)
            {
                // Last user gets the remainder to avoid rounding difference
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
