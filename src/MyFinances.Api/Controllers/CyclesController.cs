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
public class CyclesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CyclesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CycleDto>>> GetCycles([FromQuery] int householdId)
    {
        var cycles = await _context.Cycles
            .Where(c => c.HouseholdId == householdId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new CycleDto(c.Id, c.Name, c.StartDate, c.EndDate, c.IsActive, c.HouseholdId))
            .ToListAsync();

        return Ok(cycles);
    }

    [HttpPost]
    public async Task<ActionResult<CycleDto>> CreateCycle(CreateCycleRequest request)
    {
        var householdExists = await _context.Households.AnyAsync(n => n.Id == request.HouseholdId);
        if (!householdExists)
        {
            return BadRequest("Núcleo não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("O nome do ciclo é obrigatório.");
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest("A data de início deve ser anterior à data de fim.");
        }

        var cycle = new Cycle
        {
            Name = request.Name,
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate.ToUniversalTime(),
            IsActive = true,
            HouseholdId = request.HouseholdId
        };

        _context.Cycles.Add(cycle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCycles), new { householdId = cycle.HouseholdId }, new CycleDto(
            cycle.Id,
            cycle.Name,
            cycle.StartDate,
            cycle.EndDate,
            cycle.IsActive,
            cycle.HouseholdId
        ));
    }

    [HttpPost("{id}/fechar")]
    public async Task<IActionResult> CloseCycle(int id)
    {
        var cycle = await _context.Cycles.FindAsync(id);
        if (cycle == null)
        {
            return NotFound("Ciclo não encontrado.");
        }

        cycle.IsActive = false;
        cycle.EndDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCycle(int id, UpdateCycleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("O nome do ciclo é obrigatório.");
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest("A data de início deve ser anterior à data de fim.");
        }

        var cycle = await _context.Cycles.FindAsync(id);
        if (cycle == null)
        {
            return NotFound("Ciclo não encontrado.");
        }

        cycle.Name = request.Name;
        cycle.StartDate = request.StartDate.ToUniversalTime();
        cycle.EndDate = request.EndDate.ToUniversalTime();
        cycle.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
