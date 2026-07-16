namespace MyFinances.Api.Models;

public class DespesaRateio
{
    public int Id { get; set; }
    public int DespesaId { get; set; }
    public Despesa? Despesa { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public decimal Valor { get; set; }
}
