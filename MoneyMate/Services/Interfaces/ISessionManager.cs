using MoneyMate.Models;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Centralise la gestion de la session utilisateur, de sa persistance et des permissions.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Événement déclenché lors d'un changement de session.
        /// </summary>
        event EventHandler? SessionChanged;

        /// <summary>
        /// Utilisateur actuellement connecté.
        /// </summary>
        User? CurrentUser { get; }

        /// <summary>
        /// Indique si une session authentifiée est active.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Restaure une session persistée si disponible.
        /// </summary>
        /// <returns>True si une session valide a été restaurée.</returns>
        bool RestoreSession();

        /// <summary>
        /// Démarre une nouvelle session utilisateur.
        /// </summary>
        /// <param name="user">Utilisateur authentifié.</param>
        /// <param name="rememberSession">Indique si la session doit être persistée.</param>
        void StartSession(User user, bool rememberSession);

        /// <summary>
        /// Met à jour l'utilisateur courant en mémoire.
        /// </summary>
        /// <param name="user">Nouvel état de l'utilisateur.</param>
        void UpdateCurrentUser(User user);

        /// <summary>
        /// Termine la session courante.
        /// </summary>
        /// <param name="clearPersistentSession">True pour supprimer aussi la session persistée.</param>
        void ClearSession(bool clearPersistentSession = true);

        /// <summary>
        /// Retourne l'email mémorisé pour pré-remplissage de la connexion.
        /// </summary>
        string GetRememberedEmail();

        /// <summary>
        /// Indique si l'option de persistance de session est activée.
        /// </summary>
        bool GetRememberMePreference();

        /// <summary>
        /// Vérifie si l'utilisateur courant possède au moins un des rôles demandés.
        /// </summary>
        bool HasRole(params string[] roles);

        /// <summary>
        /// Vérifie si la session courante peut accéder à une route Shell donnée.
        /// </summary>
        bool CanAccessRoute(string route);
    }
}
