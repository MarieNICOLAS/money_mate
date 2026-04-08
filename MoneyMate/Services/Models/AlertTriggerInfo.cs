using MoneyMate.Models;

namespace MoneyMate.Services.Models
{
    /// <summary>
    /// Représente le résultat d'évaluation d'un seuil d'alerte.
    /// </summary>
    public class AlertTriggerInfo
    {
        /// <summary>
        /// Seuil évalué.
        /// </summary>
        public AlertThreshold? AlertThreshold { get; set; }

        /// <summary>
        /// Budget associé à l'évaluation.
        /// </summary>
        public Budget? Budget { get; set; }

        /// <summary>
        /// Montant consommé sur le budget concerné.
        /// </summary>
        public decimal ConsumedAmount { get; set; }

        /// <summary>
        /// Montant total du budget concerné.
        /// </summary>
        public decimal BudgetAmount { get; set; }

        /// <summary>
        /// Pourcentage consommé.
        /// </summary>
        public decimal ConsumedPercentage { get; set; }

        /// <summary>
        /// Indique si le seuil est atteint.
        /// </summary>
        public bool IsTriggered { get; set; }
    }
}
