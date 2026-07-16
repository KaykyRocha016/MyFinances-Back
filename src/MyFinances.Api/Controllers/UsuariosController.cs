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
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios()
    {
        var users = await _context.Usuarios
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Renda))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> CreateUsuario(UsuarioDto request)
    {
        var user = new Usuario
        {
            Nome = request.Nome,
            Renda = request.Renda
        };

        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsuarios), new UsuarioDto(user.Id, user.Nome, user.Renda));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(int id, UsuarioDto request)
    {
        var user = await _context.Usuarios.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Nome = request.Nome;
        user.Renda = request.Renda;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
