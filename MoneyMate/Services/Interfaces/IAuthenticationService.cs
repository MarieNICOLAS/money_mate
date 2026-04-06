using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Interface pour le service d'authentification.
    /// Gère l'inscription, la connexion et la gestion des sessions utilisateur.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe.
        /// </summary>
        /// <param name="email">Email de l'utilisateur.</param>
        /// <param name="password">Mot de passe en clair.</param>
        /// <returns>Résultat contenant l'utilisateur authentifié si succès.</returns>
        Task<ServiceResult<User>> LoginAsync(string email, string password);

        /// <summary>
        /// Enregistre un nouvel utilisateur.
        /// </summary>
        /// <param name="email">Email de l'utilisateur.</param>
        /// <param name="password">Mot de passe en clair.</param>
        /// <param name="devise">Devise préférée.</param>
        /// <returns>Résultat contenant l'utilisateur créé si succès.</returns>
        Task<ServiceResult<User>> RegisterAsync(string email, string password, string devise = "EUR");

        /// <summary>
        /// Déconnecte l'utilisateur actuel.
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Récupère l'utilisateur actuellement connecté.
        /// </summary>
        /// <returns>L'utilisateur connecté ou null.</returns>
        User? GetCurrentUser();

        /// <summary>
        /// Vérifie si un utilisateur est connecté.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Change le mot de passe de l'utilisateur.
        /// </summary>
        /// <param name="userId">ID de l'utilisateur.</param>
        /// <param name="oldPassword">Ancien mot de passe.</param>
        /// <param name="newPassword">Nouveau mot de passe.</param>
        /// <returns>Résultat de l'opération.</returns>
        Task<ServiceResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>
        /// Valide la force d'un mot de passe.
        /// </summary>
        /// <param name="password">Mot de passe à valider.</param>
        /// <returns>True si le mot de passe est valide.</returns>
        bool ValidatePasswordStrength(string password);
    }
}
