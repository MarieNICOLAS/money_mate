namespace MoneyMate.Services.Models
{
    /// <summary>
    /// Représente un résumé des indicateurs principaux du tableau de bord.
    /// </summary>
    public class DashboardSummary
    {
        /// <summary>
        /// Total des dépenses du mois courant.
        /// </summary>
        public decimal CurrentMonthExpenses { get; set; }

        /// <summary>
        /// Nombre total de dépenses du mois courant.
        /// </summary>
        public int CurrentMonthExpensesCount { get; set; }

        /// <summary>
        /// Nombre de budgets actifs.
        /// </summary>
        public int ActiveBudgetsCount { get; set; }

        /// <summary>
        /// Nombre de charges fixes actives.
        /// </summary>
        public int ActiveFixedChargesCount { get; set; }

        /// <summary>
        /// Nombre d'alertes actives.
        /// </summary>
        public int ActiveAlertsCount { get; set; }

        /// <summary>
        /// Nombre d'alertes actuellement déclenchées.
        /// </summary>
        public int TriggeredAlertsCount { get; set; }

        public decimal CurrentMonthBudget { get; set; }

        public decimal CurrentMonthBalance { get; set; }

        /// <summary>
        /// Total des dépenses du mois précédent.
        /// </summary>
        public decimal PreviousMonthExpenses { get; set; }

        /// <summary>
        /// Différence de dépenses entre le mois courant et le mois précédent.
        /// </summary>
        public decimal ExpensesDeltaFromPreviousMonth { get; set; }

        /// <summary>
        /// Nombre de budgets à risque ou dépassés.
        /// </summary>
        public int BudgetsAtRiskCount { get; set; }

        /// <summary>
        /// Top catégories du mois courant par dépense.
        /// </summary>
        public List<DashboardCategorySpending> TopCategories { get; set; } = [];

        public List<DashboardRecentTransaction> RecentTransactions { get; set; } = [];
    }
}
