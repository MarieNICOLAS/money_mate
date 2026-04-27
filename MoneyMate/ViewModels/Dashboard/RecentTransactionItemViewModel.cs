namespace MoneyMate.ViewModels.Dashboard;

public sealed class RecentTransactionItemViewModel
{
    public int ExpenseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string AmountDisplay { get; init; } = string.Empty;

    public Color AmountColor { get; init; } = Color.FromArgb("#D9534F");

    public string DateDisplay { get; init; } = string.Empty;

    public Color AccentColor { get; init; } = Colors.LightGray;

    public string Icon { get; init; } = "💰";
}
