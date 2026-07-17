using System;

namespace MyFinances.Api.Models;

public class Ciclo
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativo { get; set; }

    public int NucleoId { get; set; }
    public Nucleo? Nucleo { get; set; }
}
