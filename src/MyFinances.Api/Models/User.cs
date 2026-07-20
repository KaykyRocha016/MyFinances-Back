namespace MyFinances.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Income { get; set; }

    public int HouseholdId { get; set; }
    public Household? Household { get; set; }
}
