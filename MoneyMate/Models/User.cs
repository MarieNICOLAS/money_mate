using SQLite;

namespace MoneyMate.Models
{
    /// <summary>
    /// Représente un utilisateur de l'application Money Mate
    /// Gère l'authentification et les préférences utilisateur
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Identifiant unique de l'utilisateur
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Adresse email de l'utilisateur (unique)
        /// </summary>
        [Unique, NotNull, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hash BCrypt du mot de passe - JAMAIS en clair
        /// </summary>
        [NotNull, MaxLength(60)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Devise préférée de l'utilisateur (EUR, USD, etc.)
        /// </summary>
        [NotNull, MaxLength(3)]
        public string Devise { get; set; } = "EUR";

        /// <summary>
        /// Jour de début du cycle budgétaire (1-31)
        /// </summary>
        [NotNull]
        public int BudgetStartDay { get; set; } = 1;

        /// <summary>
        /// Rôle de l'utilisateur (User, Admin)
        /// </summary>
        [NotNull, MaxLength(20)]
        public string Role { get; set; } = "User";

        /// <summary>
        /// Indique si le compte est actif
        /// </summary>
        [NotNull]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date de création du compte
        /// </summary>
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
