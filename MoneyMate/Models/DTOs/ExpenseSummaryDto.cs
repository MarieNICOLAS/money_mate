namespace MoneyMate.Models.DTOs;

public class ExpenseSummaryDto
{
    public decimal TotalExpenses { get; set; }

    public decimal TotalIncome { get; set; }

    public decimal Balance { get; set; }

    public decimal PreviousMonthVariationPercent { get; set; }

    public string VariationLabel { get; set; } = "0 %";

    public string VariationColor { get; set; } = "#4F7993";

    public IReadOnlyList<CategorySummaryDto> TopCategories { get; set; } = [];
}
