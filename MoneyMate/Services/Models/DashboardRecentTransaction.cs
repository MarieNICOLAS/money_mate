namespace MoneyMate.Services.Models;

public sealed class DashboardRecentTransaction
{
    public int ExpenseId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryColor { get; set; } = "#6B7A8F";

    public string CategoryIcon { get; set; } = "💰";

    public string Note { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime DateOperation { get; set; }
}
