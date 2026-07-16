namespace MyFinances.Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string TipoDivisao { get; set; } // PROPORCIONAL, INDIVIDUAL, CUSTOMIZADO
}
