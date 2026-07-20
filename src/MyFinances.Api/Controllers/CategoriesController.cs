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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.DivisionType))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("O nome da categoria é obrigatório.");
        }

        var divisionType = request.DivisionType.ToUpper();
        if (divisionType != "PROPORCIONAL" && divisionType != "INDIVIDUAL" && divisionType != "CUSTOMIZADO")
        {
            return BadRequest("Tipo de divisão inválido. Use PROPORCIONAL, INDIVIDUAL ou CUSTOMIZADO.");
        }

        var category = new Category
        {
            Name = request.Name,
            DivisionType = divisionType
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, new CategoryDto(category.Id, category.Name, category.DivisionType));
    }
}
