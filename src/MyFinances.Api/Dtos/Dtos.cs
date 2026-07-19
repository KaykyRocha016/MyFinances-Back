using System;
using System.Collections.Generic;

namespace MyFinances.Api.Dtos;

public record CreateDespesaRequest(
    string Descricao,
    decimal Valor,
    DateTime Data,
    int UsuarioId,
    int CategoriaId,
    List<CreateDespesaRateioRequest>? Rateios
);

public record CreateDespesaRateioRequest(
    int UsuarioId,
    decimal Valor
);

public record DespesaDto(
    int Id,
    string Descricao,
    decimal Valor,
    DateTime Data,
    UsuarioDto Payer,
    CategoriaDto Categoria,
    List<DespesaRateioDto> Rateios,
    int CicloId
);

public record DespesaRateioDto(
    int Id,
    int UsuarioId,
    string UsuarioNome,
    decimal Valor
);

public record UsuarioDto(
    int Id,
    string Nome,
    decimal Renda,
    int NucleoId
);

public record CreateUsuarioRequest(
    string Nome,
    decimal Renda,
    int NucleoId
);

public record CategoriaDto(
    int Id,
    string Nome,
    string TipoDivisao
);

public record CreateCategoriaRequest(
    string Nome,
    string TipoDivisao
);

public record DashboardDto(
    decimal TotalGeral,
    List<UserBalanceDto> Balances,
    string StatusBalance
);

public record UserBalanceDto(
    int UsuarioId,
    string Nome,
    decimal TotalPago,
    decimal TotalResponsabilidade,
    decimal SaldoLiquido
);

public record CreateNucleoRequest(
    string Nome
);

public record NucleoDto(
    int Id,
    string Nome
);

public record CreateCicloRequest(
    string Nome,
    DateTime DataInicio,
    DateTime DataFim,
    int NucleoId
);

public record UpdateCicloRequest(
    string Nome,
    DateTime DataInicio,
    DateTime DataFim,
    bool Ativo
);

public record CicloDto(
    int Id,
    string Nome,
    DateTime DataInicio,
    DateTime DataFim,
    bool Ativo,
    int NucleoId
);
