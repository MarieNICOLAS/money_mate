using MoneyMate.Components;

namespace MoneyMate.ViewModels.Dashboard;

public sealed class DashboardCategoryItemViewModel
{
    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public int ExpensesCount { get; init; }

    public string AmountDisplay { get; init; } = string.Empty;

    public string PercentageDisplay { get; init; } = string.Empty;

    public string ExpensesCountText { get; init; } = string.Empty;

    public Color SegmentColor { get; init; } = Colors.LightGray;

    public DonutChartSegment ChartSegment { get; init; } = new();
}
