namespace MoneyMate.Models;

/// <summary>
/// Agregat de depenses par categorie pour les graphiques statistiques.
/// </summary>
public sealed class CategoryStatsDto
{
    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryColor { get; init; } = "#6B7A8F";

    public string CategoryIcon { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public decimal Percentage { get; init; }

    public int ExpensesCount { get; init; }
}
