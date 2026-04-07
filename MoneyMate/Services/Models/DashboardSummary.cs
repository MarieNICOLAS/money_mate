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
    }
}
