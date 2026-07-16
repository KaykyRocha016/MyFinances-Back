using System;
using System.Collections.Generic;

namespace MyFinances.Api.Models;

public class Despesa
{
    public int Id { get; set; }
    public required string Descricao { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public ICollection<DespesaRateio> Rateios { get; set; } = new List<DespesaRateio>();
}
