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
public class CiclosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CiclosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CicloDto>>> GetCiclos([FromQuery] int nucleoId)
    {
        var ciclos = await _context.Ciclos
            .Where(c => c.NucleoId == nucleoId)
            .OrderByDescending(c => c.DataInicio)
            .Select(c => new CicloDto(c.Id, c.Nome, c.DataInicio, c.DataFim, c.Ativo, c.NucleoId))
            .ToListAsync();

        return Ok(ciclos);
    }

    [HttpPost]
    public async Task<ActionResult<CicloDto>> CreateCiclo(CreateCicloRequest request)
    {
        var nucleoExists = await _context.Nucleos.AnyAsync(n => n.Id == request.NucleoId);
        if (!nucleoExists)
        {
            return BadRequest("Núcleo não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("O nome do ciclo é obrigatório.");
        }

        if (request.DataInicio >= request.DataFim)
        {
            return BadRequest("A data de início deve ser anterior à data de fim.");
        }

        // Deactivate other active cycles of this nucleo
        var activeCycles = await _context.Ciclos
            .Where(c => c.NucleoId == request.NucleoId && c.Ativo)
            .ToListAsync();

        foreach (var activeCycle in activeCycles)
        {
            activeCycle.Ativo = false;
        }

        var ciclo = new Ciclo
        {
            Nome = request.Nome,
            DataInicio = request.DataInicio.ToUniversalTime(),
            DataFim = request.DataFim.ToUniversalTime(),
            Ativo = true,
            NucleoId = request.NucleoId
        };

        _context.Ciclos.Add(ciclo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCiclos), new { nucleoId = ciclo.NucleoId }, new CicloDto(
            ciclo.Id,
            ciclo.Nome,
            ciclo.DataInicio,
            ciclo.DataFim,
            ciclo.Ativo,
            ciclo.NucleoId
        ));
    }

    [HttpPost("{id}/fechar")]
    public async Task<IActionResult> FecharCiclo(int id)
    {
        var ciclo = await _context.Ciclos.FindAsync(id);
        if (ciclo == null)
        {
            return NotFound("Ciclo não encontrado.");
        }

        ciclo.Ativo = false;
        ciclo.DataFim = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
