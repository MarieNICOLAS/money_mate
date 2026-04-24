using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente le budget global d'un utilisateur pour un mois donné.
    /// </summary>
    [Table("Budgets")]
    public class Budget
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Indexed]
        public int UserId { get; set; }

        [NotNull]
        public decimal Amount { get; set; }

        [NotNull, MaxLength(20)]
        public string PeriodType { get; set; } = "Monthly";

        [NotNull]
        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime? EndDate { get; set; }

        [NotNull]
        public bool IsActive { get; set; } = true;

        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Ignore]
        public User? User { get; set; }

        [Indexed]
        public int CategoryId { get; set; }

        [Ignore]
        public int Year => StartDate.Year;

        [Ignore]
        public int Month => StartDate.Month;

        [Ignore]
        public string MonthLabel => StartDate.ToString("MMMM yyyy");

        public void NormalizeToMonthlyPeriod()
        {
            StartDate = new DateTime(StartDate.Year, StartDate.Month, 1);
            EndDate = StartDate.AddMonths(1).AddDays(-1);
            PeriodType = "Monthly";
        }

        public decimal CalculateBudgetPercentage(decimal totalExpenses)
        {
            if (Amount <= 0)
                return 0;

            return Math.Min(100, (totalExpenses / Amount) * 100);
        }
    }
}
