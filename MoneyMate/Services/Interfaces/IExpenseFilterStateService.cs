using MoneyMate.Models.DTOs;

namespace MoneyMate.Services.Interfaces;

public interface IExpenseFilterStateService
{
    ExpenseFilterDto CurrentFilter { get; }

    long Version { get; }

    void SetFilter(ExpenseFilterDto filter);

    void Reset();
}
