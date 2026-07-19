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
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaDto>>> GetCategorias()
    {
        var categories = await _context.Categorias
            .Select(c => new CategoriaDto(c.Id, c.Nome, c.TipoDivisao))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> CreateCategoria(CreateCategoriaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("O nome da categoria é obrigatório.");
        }

        var divisionType = request.TipoDivisao.ToUpper();
        if (divisionType != "PROPORCIONAL" && divisionType != "INDIVIDUAL" && divisionType != "CUSTOMIZADO")
        {
            return BadRequest("Tipo de divisão inválido. Use PROPORCIONAL, INDIVIDUAL ou CUSTOMIZADO.");
        }

        var category = new Categoria
        {
            Nome = request.Nome,
            TipoDivisao = divisionType
        };

        _context.Categorias.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategorias), new { id = category.Id }, new CategoriaDto(category.Id, category.Nome, category.TipoDivisao));
    }
}
