using System;

namespace MyFinances.Api.Models;

public class Cycle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }
}
