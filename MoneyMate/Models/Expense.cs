using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente une dépense effectuée par un utilisateur
    /// Suit le modèle défini dans CONTRIBUTING.md
    /// </summary>
    [Table("Expenses")]
    public class Expense
    {
        /// <summary>
        /// Identifiant unique de la dépense
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur propriétaire
        /// </summary>
        [NotNull, Indexed]
        public int UserId { get; set; }

        /// <summary>
        /// Date de l'opération
        /// </summary>
        [NotNull]
        public DateTime DateOperation { get; set; } = DateTime.Now;

        /// <summary>
        /// Montant de la dépense - decimal(10,2) selon spécifications
        /// </summary>
        [NotNull]
        public decimal Amount { get; set; }

        /// <summary>
        /// Identifiant de la catégorie
        /// </summary>
        [NotNull, Indexed]
        public int CategoryId { get; set; }

        /// <summary>
        /// Note ou description optionnelle de la dépense
        /// </summary>
        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Indique si c'est une charge fixe récurrente
        /// </summary>
        [NotNull]
        public bool IsFixedCharge { get; set; } = false;

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
    }
}
