namespace MoneyMate.Models.DTOs;

public class ExpenseListItemDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string FormattedAmount { get; set; } = string.Empty;

    public DateTime OperationDate { get; set; }

    public string FormattedDate { get; set; } = string.Empty;

    public string Type { get; set; } = "Dépense";

    public bool IsIncome { get; set; }

    public bool IsExpense { get; set; }

    public bool IsTransfer { get; set; }

    public bool IsFixedCharge { get; set; }

    public string Icon { get; set; } = "💰";

    public string IconBackgroundColor { get; set; } = "#EEF2F5";

    public string AmountColor { get; set; } = "#D9534F";

    public string Devise { get; set; } = "EUR";

    public string TypeBadgeColor => IsIncome ? "#EAF7EA" : IsTransfer ? "#EEF2F5" : IsFixedCharge ? "#FFF2EC" : "#FFF0EE";

    public string SignedFormattedAmount => IsIncome ? $"+{FormattedAmount}" : IsTransfer ? FormattedAmount : $"-{FormattedAmount}";
}
