namespace MoneyMate.Models.DTOs;

public class CategorySummaryDto
{
    public int CategoryId { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public double Percentage { get; set; }

    public string ColorHex { get; set; } = "#6793AE";

    public string Icon { get; set; } = "💰";

    public string PercentageLabel => $"{Percentage:0.#} %";
}
