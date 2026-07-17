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
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios([FromQuery] int? nucleoId)
    {
        IQueryable<Usuario> query = _context.Usuarios;
        if (nucleoId.HasValue)
        {
            query = query.Where(u => u.NucleoId == nucleoId.Value);
        }

        var users = await query
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Renda, u.NucleoId))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> CreateUsuario(CreateUsuarioRequest request)
    {
        var nucleoExists = await _context.Nucleos.AnyAsync(n => n.Id == request.NucleoId);
        if (!nucleoExists)
        {
            return BadRequest("Núcleo de destino não encontrado.");
        }

        var user = new Usuario
        {
            Nome = request.Nome,
            Renda = request.Renda,
            NucleoId = request.NucleoId
        };

        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsuarios), new { nucleoId = user.NucleoId }, new UsuarioDto(user.Id, user.Nome, user.Renda, user.NucleoId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(int id, UsuarioDto request)
    {
        var user = await _context.Usuarios.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var nucleoExists = await _context.Nucleos.AnyAsync(n => n.Id == request.NucleoId);
        if (!nucleoExists)
        {
            return BadRequest("Núcleo de destino não encontrado.");
        }

        user.Nome = request.Nome;
        user.Renda = request.Renda;
        user.NucleoId = request.NucleoId;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
