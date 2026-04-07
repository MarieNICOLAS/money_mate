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
        /// Événement déclenché lors d'un changement d'état de session.
        /// </summary>
        event EventHandler? AuthenticationStateChanged;

        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe.
        /// </summary>
        /// <param name="email">Email de l'utilisateur.</param>
        /// <param name="password">Mot de passe en clair.</param>
        /// <param name="rememberSession">Indique si la session doit être restaurée au prochain lancement.</param>
        /// <returns>Résultat contenant l'utilisateur authentifié si succès.</returns>
        Task<ServiceResult<User>> LoginAsync(string email, string password, bool rememberSession = false);

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
        /// <param name="clearPersistentSession">True pour supprimer aussi la session persistée.</param>
        Task LogoutAsync(bool clearPersistentSession = true);

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
        /// Restaure une session persistée si disponible.
        /// </summary>
        bool RestoreSession();

        /// <summary>
        /// Retourne l'email mémorisé pour pré-remplir la connexion.
        /// </summary>
        string GetRememberedEmail();

        /// <summary>
        /// Retourne l'état de l'option "Se souvenir de moi".
        /// </summary>
        bool GetRememberMePreference();

        /// <summary>
        /// Vérifie si l'utilisateur courant possède au moins un des rôles demandés.
        /// </summary>
        bool HasRole(params string[] roles);

        /// <summary>
        /// Vérifie si l'utilisateur courant peut accéder à une route de navigation.
        /// </summary>
        bool CanAccessRoute(string route);

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
