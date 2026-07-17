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

        return CreatedAtAction(nameof(GetNucleos), new { id = nucleo.Id }, new NucleoDto(nucleo.Id, nucleo.Nome));
    }
}
