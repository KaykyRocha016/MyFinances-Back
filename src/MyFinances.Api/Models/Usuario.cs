namespace MyFinances.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public decimal Renda { get; set; }
}
