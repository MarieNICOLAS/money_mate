using MoneyMate.Helpers;

namespace MoneyMate.Models.DTOs;

public class CalendarOperationDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Icon { get; set; } = "💰";

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string Type { get; set; } = "Dépense";

    public bool IsFixedCharge { get; set; }

    public string StatusLabel { get; set; } = "À venir";

    public string AmountColor { get; set; } = "#D9534F";

    public string BadgeBackgroundColor { get; set; } = "#FFF0EE";

    public string FormattedAmount => CurrencyHelper.Format(Amount);

    public string SignedFormattedAmount
    {
        get
        {
            string amount = CurrencyHelper.Format(Math.Abs(Amount));
            return IsIncome ? $"+{amount}" : $"-{amount}";
        }
    }

    public bool IsExpense => string.Equals(Type, "Dépense", StringComparison.OrdinalIgnoreCase);

    public bool IsIncome => string.Equals(Type, "Revenu", StringComparison.OrdinalIgnoreCase);

    public bool IsTransfer => string.Equals(Type, "Transfert", StringComparison.OrdinalIgnoreCase);
}
