using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinances.Api.Data;
using MyFinances.Api.Dtos;
using MyFinances.Api.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinances.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NucleosController : ControllerBase
{
    private readonly AppDbContext _context;

    public NucleosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NucleoDto>>> GetNucleos()
    {
        var nucleos = await _context.Nucleos
            .Select(n => new NucleoDto(n.Id, n.Nome))
            .ToListAsync();

        return Ok(nucleos);
    }

    [HttpPost]
    public async Task<ActionResult<NucleoDto>> CreateNucleo(CreateNucleoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("O nome do núcleo é obrigatório.");
        }

        var nucleo = new Nucleo
        {
            Nome = request.Nome
        };

        _context.Nucleos.Add(nucleo);
        await _context.SaveChangesAsync();

        // Automatically create a default active cycle for the new Nucleo
        var defaultCycle = new Ciclo
        {
            Nome = "Ciclo Inicial",
            DataInicio = System.DateTime.UtcNow,
            DataFim = System.DateTime.UtcNow.AddYears(1),
            Ativo = true,
            NucleoId = nucleo.Id
        };
        _context.Ciclos.Add(defaultCycle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetNucleos), new { id = nucleo.Id }, new NucleoDto(nucleo.Id, nucleo.Nome));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNucleo(int id, CreateNucleoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("O nome do núcleo é obrigatório.");
        }

        var nucleo = await _context.Nucleos.FindAsync(id);
        if (nucleo == null)
        {
            return NotFound();
        }

        nucleo.Nome = request.Nome;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNucleo(int id)
    {
        var nucleo = await _context.Nucleos.FindAsync(id);
        if (nucleo == null)
        {
            return NotFound();
        }

        _context.Nucleos.Remove(nucleo);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
