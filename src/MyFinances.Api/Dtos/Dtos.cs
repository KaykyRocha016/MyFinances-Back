using System;
using System.Collections.Generic;

namespace MyFinances.Api.Dtos;

public record CreateExpenseRequest(
    string Description,
    decimal Amount,
    DateTime Date,
    int UserId,
    int CategoryId,
    List<CreateExpenseSplitRequest>? Splits,
    int TotalInstallments = 1,
    int? CycleId = null
);

public record UpdateExpenseRequest(
    string Description,
    decimal Amount,
    int CategoryId,
    DateTime? Date = null,
    bool UpdateAllInstallments = false,
    int? UserId = null,
    List<CreateExpenseSplitRequest>? Splits = null
);

public record CreateExpenseSplitRequest(
    int? UserId,
    decimal Amount,
    string? GuestName = null
);

public record ExpenseDto(
    int Id,
    string Description,
    decimal Amount,
    DateTime Date,
    UserDto Payer,
    CategoryDto Category,
    List<ExpenseSplitDto> Splits,
    int CycleId,
    int InstallmentNumber = 1,
    int TotalInstallments = 1,
    Guid? InstallmentGroupId = null
);

public record ExpenseSplitDto(
    int Id,
    int? UserId,
    string UserName,
    decimal Amount
);

public record UserDto(
    int Id,
    string Name,
    decimal Income,
    int HouseholdId
);

public record CreateUserRequest(
    string Name,
    decimal Income,
    int HouseholdId
);

public record CategoryDto(
    int Id,
    string Name,
    string DivisionType
);

public record CreateCategoryRequest(
    string Name,
    string DivisionType
);

public record DashboardDto(
    decimal TotalOverall,
    List<UserBalanceDto> Balances,
    string StatusBalance
);

public record UserBalanceDto(
    int UserId,
    string Name,
    decimal TotalPaid,
    decimal TotalResponsibility,
    decimal NetBalance
);

public record CreateHouseholdRequest(
    string Name
);

public record HouseholdDto(
    int Id,
    string Name
);

public record CreateCycleRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int HouseholdId
);

public record UpdateCycleRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

public record CycleDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    int HouseholdId
);
