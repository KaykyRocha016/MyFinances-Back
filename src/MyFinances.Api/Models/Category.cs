using System;

namespace MyFinances.Api.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string DivisionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
