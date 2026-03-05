namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Interface pour le service d authentification
    /// Gere l inscription, la connexion et la gestion des sessions utilisateur
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe
        /// </summary>
        /// <param name="email">Email de l utilisateur</param>
        /// <param name="password">Mot de passe en clair</param>
        /// <returns>L utilisateur authentifie ou null si echec</returns>
        Task<Models.User?> LoginAsync(string email, string password);

        /// <summary>
        /// Enregistre un nouvel utilisateur
        /// </summary>
        /// <param name="email">Email de l utilisateur</param>
        /// <param name="password">Mot de passe en clair</param>
        /// <param name="devise">Devise preferee (EUR par defaut)</param>
        /// <returns>L utilisateur cree ou null si erreur</returns>
        Task<Models.User?> RegisterAsync(string email, string password, string devise = "EUR");

        /// <summary>
        /// Deconnecte l utilisateur actuel
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Recupere l utilisateur actuellement connecte
        /// </summary>
        /// <returns>L utilisateur connecte ou null</returns>
        Models.User? GetCurrentUser();

        /// <summary>
        /// Verifie si un utilisateur est connecte
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Change le mot de passe de l utilisateur
        /// </summary>
        /// <param name="userId">ID de l utilisateur</param>
        /// <param name="oldPassword">Ancien mot de passe</param>
        /// <param name="newPassword">Nouveau mot de passe</param>
        /// <returns>True si succes, False sinon</returns>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>
        /// Valide la force d un mot de passe
        /// </summary>
        /// <param name="password">Mot de passe à valider</param>
        /// <returns>True si le mot de passe est valide</returns>
        bool ValidatePasswordStrength(string password);
    }
}