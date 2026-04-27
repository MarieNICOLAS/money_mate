namespace MoneyMate.Models.DTOs;

public class CalendarDayDto
{
    public DateTime Date { get; set; }

    public int DayNumber { get; set; }

    public bool IsCurrentMonth { get; set; }

    public bool IsToday { get; set; }

    public bool IsSelected { get; set; }

    public bool HasExpense { get; set; }

    public bool HasIncome { get; set; }

    public bool HasFixedCharge { get; set; }

    public decimal ExpenseTotal { get; set; }

    public decimal IncomeTotal { get; set; }

    public decimal Balance { get; set; }

    public string DotColor { get; set; } = "Transparent";

    public bool HasOperation => HasExpense || HasIncome || HasFixedCharge;

    public string DayBackgroundColor => IsSelected ? "#6793AE" : "#FFFFFF";

    public string DayTextColor => IsSelected
        ? "#FFFFFF"
        : IsCurrentMonth
            ? "#222222"
            : "#B7B7B7";

    public string DayBorderColor => IsToday && !IsSelected ? "#6793AE" : "Transparent";

    public double DayOpacity => IsCurrentMonth ? 1d : 0.55d;
}
