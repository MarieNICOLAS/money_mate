using MoneyMate.Models;

namespace MoneyMate.Services.Models;

/// <summary>
/// Données préparées pour l'affichage mobile de la liste des catégories.
/// </summary>
public sealed class CategoryListItemDto
{
    public required Category Category { get; init; }

    public decimal BudgetAmount { get; init; }

    public decimal SpentAmount { get; init; }

    public decimal ThresholdPercentage { get; init; } = 100m;

    public decimal ThresholdAmount { get; init; }

    public decimal RemainingBeforeThreshold { get; init; }

    public decimal ConsumedPercentage { get; init; }

    public bool HasAlertThreshold { get; init; }

    public string ThresholdStatus { get; init; } = "OK";
}
