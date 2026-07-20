using System;
using System.Collections.Generic;

namespace MyFinances.Api.Models;

public class Expense
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int CycleId { get; set; }
    public Cycle? Cycle { get; set; }

    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
