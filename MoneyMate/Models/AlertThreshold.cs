using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente un seuil d'alerte pour les budgets
    /// Permet d'alerter l'utilisateur quand les dépenses dépassent un pourcentage du budget
    /// </summary>
    [Table("AlertThresholds")]
    public class AlertThreshold
    {
        /// <summary>
        /// Identifiant unique du seuil d'alerte
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur propriétaire
        /// </summary>
        [NotNull, Indexed]
        public int UserId { get; set; }

        /// <summary>
        /// Identifiant du budget concerné (optionnel pour seuil global)
        /// </summary>
        [Indexed]
        public int? BudgetId { get; set; }

        /// <summary>
        /// Identifiant de la catégorie concernée (optionnel pour seuil global)
        /// </summary>
        [Indexed]
        public int? CategoryId { get; set; }

        /// <summary>
        /// Pourcentage du budget à partir duquel déclencher l'alerte (0-100)
        /// </summary>
        [NotNull]
        public decimal ThresholdPercentage { get; set; } = 80;

        /// <summary>
        /// Type d'alerte (Warning, Critical)
        /// </summary>
        [NotNull, MaxLength(20)]
        public string AlertType { get; set; } = "Warning";

        /// <summary>
        /// Message personnalisé de l'alerte
        /// </summary>
        [MaxLength(255)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Indique si l'alerte est active
        /// </summary>
        [NotNull]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indique si des notifications push doivent être envoyées
        /// </summary>
        [NotNull]
        public bool SendNotification { get; set; } = true;

        /// <summary>
        /// Date de création du seuil
        /// </summary>
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property vers l'utilisateur
        /// </summary>
        [Ignore]
        public User? User { get; set; }

        /// <summary>
        /// Navigation property vers le budget (si applicable)
        /// </summary>
        [Ignore]
        public Budget? Budget { get; set; }

        /// <summary>
        /// Navigation property vers la catégorie (si applicable)
        /// </summary>
        [Ignore]
        public Category? Category { get; set; }
    }
}
