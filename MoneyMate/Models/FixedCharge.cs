using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente une charge fixe récurrente
    /// Permet de gérer les abonnements et charges mensuelles automatiques
    /// </summary>
    [Table("FixedCharges")]
    public class FixedCharge
    {
        /// <summary>
        /// Identifiant unique de la charge fixe
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur propriétaire
        /// </summary>
        [NotNull, Indexed]
        public int UserId { get; set; }

        /// <summary>
        /// Nom de la charge fixe (ex: Netflix, EDF)
        /// </summary>
        [NotNull, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description de la charge fixe
        /// </summary>
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Montant de la charge fixe
        /// </summary>
        [NotNull]
        public decimal Amount { get; set; }

        /// <summary>
        /// Identifiant de la catégorie
        /// </summary>
        [NotNull, Indexed]
        public int CategoryId { get; set; }

        /// <summary>
        /// Fréquence de récurrence (Monthly, Quarterly, Yearly)
        /// </summary>
        [NotNull, MaxLength(20)]
        public string Frequency { get; set; } = "Monthly";

        /// <summary>
        /// Jour du mois de prélèvement (1-31)
        /// </summary>
        [NotNull]
        public int DayOfMonth { get; set; } = 1;

        /// <summary>
        /// Date de première occurrence
        /// </summary>
        [NotNull]
        public DateTime StartDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de fin de l'abonnement (optionnelle)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Indique si la charge fixe est active
        /// </summary>
        [NotNull]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indique si une dépense automatique doit être créée
        /// </summary>
        [NotNull]
        public bool AutoCreateExpense { get; set; } = true;

        /// <summary>
        /// Date de création de la charge fixe
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
        /// Calcule la prochaine date d'échéance
        /// </summary>
        /// <returns>Date de la prochaine occurrence</returns>
        public DateTime GetNextOccurrenceDate()
        {
            var now = DateTime.Now;
            var nextDate = StartDate;

            while (nextDate <= now)
            {
                nextDate = Frequency switch
                {
                    "Monthly" => nextDate.AddMonths(1),
                    "Quarterly" => nextDate.AddMonths(3),
                    "Yearly" => nextDate.AddYears(1),
                    _ => nextDate.AddMonths(1)
                };
            }

            return nextDate;
        }
    }
}
