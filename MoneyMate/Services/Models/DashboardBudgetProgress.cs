namespace MoneyMate.Services.Models;

public sealed class DashboardBudgetProgress
{
    public int BudgetId { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryColor { get; set; } = "#6793AE";

    public string CategoryIcon { get; set; } = "💰";

    public decimal BudgetAmount { get; set; }

    public decimal SpentAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public decimal ConsumedPercentage { get; set; }

    public bool IsExceeded { get; set; }
}
