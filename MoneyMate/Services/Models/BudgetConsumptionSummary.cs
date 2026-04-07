using MoneyMate.Models;

namespace MoneyMate.Services.Models
{
    /// <summary>
    /// Représente l'état de consommation d'un budget.
    /// </summary>
    public class BudgetConsumptionSummary
    {
        /// <summary>
        /// Budget concerné.
        /// </summary>
        public Budget? Budget { get; set; }

        /// <summary>
        /// Montant consommé sur la période du budget.
        /// </summary>
        public decimal ConsumedAmount { get; set; }

        /// <summary>
        /// Montant restant disponible.
        /// </summary>
        public decimal RemainingAmount { get; set; }

        /// <summary>
        /// Pourcentage consommé.
        /// </summary>
        public decimal ConsumedPercentage { get; set; }

        /// <summary>
        /// Indique si le budget est dépassé.
        /// </summary>
        public bool IsExceeded { get; set; }
    }
}
