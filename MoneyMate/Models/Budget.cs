using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente un budget défini pour une catégorie et une période
    /// Permet le suivi et le contrôle des dépenses
    /// </summary>
    [Table("Budgets")]
    public class Budget
    {
        /// <summary>
        /// Identifiant unique du budget
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur propriétaire
        /// </summary>
        [NotNull, Indexed]
        public int UserId { get; set; }

        /// <summary>
        /// Identifiant de la catégorie concernée
        /// </summary>
        [NotNull, Indexed]
        public int CategoryId { get; set; }

        /// <summary>
        /// Montant du budget alloué
        /// </summary>
        [NotNull]
        public decimal Amount { get; set; }

        /// <summary>
        /// Type de période du budget (Monthly, Weekly, Yearly)
        /// </summary>
        [NotNull, MaxLength(20)]
        public string PeriodType { get; set; } = "Monthly";

        /// <summary>
        /// Date de début de validité du budget
        /// </summary>
        [NotNull]
        public DateTime StartDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de fin de validité du budget (optionnelle)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Indique si le budget est actif
        /// </summary>
        [NotNull]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date de création du budget
        /// </summary>
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property vers l'utilisateur
        /// </summary>
        [Ignore]
        public User? User { get; set; }

        /// <summary>
        /// Navigation property vers la catégorie
        /// </summary>
        [Ignore]
        public Category? Category { get; set; }

        /// <summary>
        /// Calcule le pourcentage de budget consommé
        /// Formule : Budget consommé (%) = Dépenses / Budget
        /// </summary>
        /// <param name="totalExpenses">Total des dépenses de la période</param>
        /// <returns>Pourcentage entre 0 et 100</returns>
        public decimal CalculateBudgetPercentage(decimal totalExpenses)
        {
            if (Amount <= 0) return 0;
            return Math.Min(100, (totalExpenses / Amount) * 100);
        }
    }
}
