namespace MoneyMate.ViewModels.Dashboard;

public sealed class CategoryBudget
{
    public int BudgetId { get; init; }

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string Icon { get; init; } = "💰";

    public decimal SpentAmount { get; init; }

    public decimal BudgetAmount { get; init; }

    public decimal RemainingAmount { get; init; }

    public double Progress { get; init; }

    public string AmountDisplay { get; init; } = string.Empty;

    public string RemainingDisplay { get; init; } = string.Empty;

    public string PercentageDisplay { get; init; } = string.Empty;

    public Color AccentColor { get; init; } = Colors.LightGray;

    public Color ProgressColor { get; init; } = Color.FromArgb("#5CB85C");
}
