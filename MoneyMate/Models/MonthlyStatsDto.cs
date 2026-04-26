namespace MoneyMate.Models;

/// <summary>
/// Resume mensuel prepare pour l'ecran Statistiques.
/// </summary>
public sealed class MonthlyStatsDto
{
    public DateTime PeriodStart { get; init; }

    public DateTime PeriodEnd { get; init; }

    public decimal IncomeAmount { get; init; }

    public decimal ExpenseAmount { get; init; }

    public decimal NetBalance { get; init; }

    public int ExpensesCount { get; init; }

    public bool HasIncomeSource { get; init; }
}
