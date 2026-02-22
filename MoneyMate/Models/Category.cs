using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente une catégorie de dépenses
    /// Permet d'organiser et analyser les dépenses par type
    /// </summary>
    [Table("Categories")]
    public class Category
    {
        /// <summary>
        /// Identifiant unique de la catégorie
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Nom de la catégorie (ex: Alimentation, Transport)
        /// </summary>
        [NotNull, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description optionnelle de la catégorie
        /// </summary>
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Couleur de la catégorie au format hexadécimal (#RRGGBB)
        /// </summary>
        [NotNull, MaxLength(7)]
        public string Color { get; set; } = "#6B7A8F";

        /// <summary>
        /// Icône de la catégorie (nom de l'icône ou code Unicode)
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "💰";

        /// <summary>
        /// Ordre d'affichage de la catégorie
        /// </summary>
        [NotNull]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si la catégorie est active
        /// </summary>
        [NotNull]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date de création de la catégorie
        /// </summary>
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property vers les dépenses de cette catégorie
        /// </summary>
        [Ignore]
        public List<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
