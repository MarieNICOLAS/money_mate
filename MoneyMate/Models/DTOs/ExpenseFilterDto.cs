namespace MoneyMate.Models.DTOs;

public class ExpenseFilterDto
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string OperationType { get; set; } = "Toutes";

    public List<int> CategoryIds { get; set; } = [];

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public string PaymentMethod { get; set; } = "Tous";

    public bool? IsFixedCharge { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public string SortBy { get; set; } = "Date";

    public bool SortDescending { get; set; } = true;

    public ExpenseFilterDto Clone()
        => new()
        {
            StartDate = StartDate,
            EndDate = EndDate,
            OperationType = OperationType,
            CategoryIds = [.. CategoryIds],
            MinAmount = MinAmount,
            MaxAmount = MaxAmount,
            PaymentMethod = PaymentMethod,
            IsFixedCharge = IsFixedCharge,
            SearchText = SearchText,
            SortBy = SortBy,
            SortDescending = SortDescending
        };
}
