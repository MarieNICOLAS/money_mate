namespace MoneyMate.Models.DTOs;

public class CategoryFilterItemDto
{
    public int CategoryId { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Icon { get; set; } = "💰";

    public string ColorHex { get; set; } = "#6793AE";

    public bool IsSelected { get; set; } = true;

    public string SelectionMark => IsSelected ? "✓" : string.Empty;

    public string BackgroundColor => IsSelected ? "#EEF2F5" : "#FFFFFF";

    public string BorderColor => IsSelected ? "#6793AE" : "#D6E1EA";
}
