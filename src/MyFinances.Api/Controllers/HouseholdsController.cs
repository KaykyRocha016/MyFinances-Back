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
public class HouseholdsController : ControllerBase
{
    private readonly AppDbContext _context;

    public HouseholdsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HouseholdDto>>> GetHouseholds()
    {
        var households = await _context.Households
            .Select(n => new HouseholdDto(n.Id, n.Name))
            .ToListAsync();

        return Ok(households);
    }

    [HttpPost]
    public async Task<ActionResult<HouseholdDto>> CreateHousehold(CreateHouseholdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("O nome do núcleo é obrigatório.");
        }

        var household = new Household
        {
            Name = request.Name
        };

        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        // Automatically create a default active cycle for the new Household
        var defaultCycle = new Cycle
        {
            Name = "Ciclo Inicial",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            IsActive = true,
            HouseholdId = household.Id
        };
        _context.Cycles.Add(defaultCycle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHouseholds), new { id = household.Id }, new HouseholdDto(household.Id, household.Name));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHousehold(int id, CreateHouseholdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("O nome do núcleo é obrigatório.");
        }

        var household = await _context.Households.FindAsync(id);
        if (household == null)
        {
            return NotFound();
        }

        household.Name = request.Name;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHousehold(int id)
    {
        var household = await _context.Households.FindAsync(id);
        if (household == null)
        {
            return NotFound();
        }

        _context.Households.Remove(household);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
