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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] int? householdId)
    {
        IQueryable<User> query = _context.Users;
        if (householdId.HasValue)
        {
            query = query.Where(u => u.HouseholdId == householdId.Value);
        }

        var users = await query
            .Select(u => new UserDto(u.Id, u.Name, u.Income, u.HouseholdId))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        var householdExists = await _context.Households.AnyAsync(n => n.Id == request.HouseholdId);
        if (!householdExists)
        {
            return BadRequest("Núcleo de destino não encontrado.");
        }

        var user = new User
        {
            Name = request.Name,
            Income = request.Income,
            HouseholdId = request.HouseholdId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new { householdId = user.HouseholdId }, new UserDto(user.Id, user.Name, user.Income, user.HouseholdId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UserDto request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var householdExists = await _context.Households.AnyAsync(n => n.Id == request.HouseholdId);
        if (!householdExists)
        {
            return BadRequest("Núcleo de destino não encontrado.");
        }

        user.Name = request.Name;
        user.Income = request.Income;
        user.HouseholdId = request.HouseholdId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
