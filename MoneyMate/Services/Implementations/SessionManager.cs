using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Security;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Gère l'état de session utilisateur, sa persistance locale et les permissions d'accès.
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private const string RememberMeKey = "remember_me";
        private const string RememberedEmailKey = "remember_email";
        private const string SessionUserIdKey = "session_user_id";

        private static readonly HashSet<string> PublicRoutes =
        [
            "MainPage",
            "LoginPage",
            "RegisterPage",
            "ErrorPage",
            "NotFoundPage",
            "NoConnectionPage"
        ];

        private static readonly Dictionary<string, string[]> RoutePermissions = new(StringComparer.Ordinal)
        {
            ["DashboardPage"] = [UserRoles.User, UserRoles.Admin],
            ["ProfilePage"] = [UserRoles.User, UserRoles.Admin],
            ["ChangePasswordPage"] = [UserRoles.User, UserRoles.Admin],
            ["DeleteAccountPage"] = [UserRoles.User, UserRoles.Admin],
            ["ExpensesListPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["EditExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["ExpenseDetailsPage"] = [UserRoles.User, UserRoles.Admin],
            ["QuickAddExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["CategoriesListPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddCategoryPage"] = [UserRoles.User, UserRoles.Admin],
            ["EditCategoryPage"] = [UserRoles.User, UserRoles.Admin],
            ["BudgetsOverviewPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddBudgetPage"] = [UserRoles.User, UserRoles.Admin],
            ["EditBudgetPage"] = [UserRoles.User, UserRoles.Admin],
            ["AlertThresholdPage"] = [UserRoles.User, UserRoles.Admin],
            ["CalendarPage"] = [UserRoles.User, UserRoles.Admin]
        };

        private readonly MoneyMateDbContext _dbContext;
        private User? _currentUser;

        public SessionManager()
        {
            _dbContext = DatabaseService.Instance;
        }

        /// <summary>
        /// Événement déclenché lors d'un changement de session.
        /// </summary>
        public event EventHandler? SessionChanged;

        /// <summary>
        /// Utilisateur actuellement connecté.
        /// </summary>
        public User? CurrentUser => _currentUser;

        /// <summary>
        /// Indique si une session authentifiée est active.
        /// </summary>
        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Restaure une session persistée si disponible.
        /// </summary>
        public bool RestoreSession()
        {
            if (!Preferences.Get(RememberMeKey, false))
            {
                ClearTransientSessionData();
                return false;
            }

            int userId = Preferences.Get(SessionUserIdKey, 0);
            if (userId <= 0)
            {
                ClearSession();
                return false;
            }

            User? user = _dbContext.GetUserById(userId);
            if (user == null || !user.IsActive)
            {
                ClearSession();
                return false;
            }

            _currentUser = user;
            OnSessionChanged();
            return true;
        }

        /// <summary>
        /// Démarre une nouvelle session utilisateur.
        /// </summary>
        public void StartSession(User user, bool rememberSession)
        {
            _currentUser = user;

            if (rememberSession)
            {
                Preferences.Set(RememberMeKey, true);
                Preferences.Set(RememberedEmailKey, user.Email);
                Preferences.Set(SessionUserIdKey, user.Id);
            }
            else
            {
                ClearTransientSessionData();
            }

            OnSessionChanged();
        }

        /// <summary>
        /// Met à jour l'utilisateur courant en mémoire.
        /// </summary>
        public void UpdateCurrentUser(User user)
        {
            _currentUser = user;

            if (Preferences.Get(RememberMeKey, false))
            {
                Preferences.Set(RememberedEmailKey, user.Email);
                Preferences.Set(SessionUserIdKey, user.Id);
            }

            OnSessionChanged();
        }

        /// <summary>
        /// Termine la session courante.
        /// </summary>
        public void ClearSession(bool clearPersistentSession = true)
        {
            _currentUser = null;

            if (clearPersistentSession)
            {
                Preferences.Remove(RememberedEmailKey);
                Preferences.Remove(SessionUserIdKey);
                Preferences.Set(RememberMeKey, false);
            }
            else
            {
                Preferences.Remove(SessionUserIdKey);
            }

            OnSessionChanged();
        }

        /// <summary>
        /// Retourne l'email mémorisé pour pré-remplissage de la connexion.
        /// </summary>
        public string GetRememberedEmail()
            => Preferences.Get(RememberedEmailKey, string.Empty);

        /// <summary>
        /// Indique si la persistance de session est activée.
        /// </summary>
        public bool GetRememberMePreference()
            => Preferences.Get(RememberMeKey, false);

        /// <summary>
        /// Vérifie si l'utilisateur courant possède au moins un des rôles demandés.
        /// </summary>
        public bool HasRole(params string[] roles)
        {
            if (_currentUser == null || roles == null || roles.Length == 0)
                return false;

            return roles.Any(role => string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Vérifie si la session courante peut accéder à une route donnée.
        /// </summary>
        public bool CanAccessRoute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            if (PublicRoutes.Contains(route))
                return true;

            if (!RoutePermissions.TryGetValue(route, out string[]? allowedRoles))
                return true;

            if (!IsAuthenticated)
                return false;

            return HasRole(allowedRoles);
        }

        /// <summary>
        /// Nettoie les données de session temporaires sans toucher au reste des préférences.
        /// </summary>
        private static void ClearTransientSessionData()
        {
            Preferences.Remove(RememberedEmailKey);
            Preferences.Remove(SessionUserIdKey);
            Preferences.Set(RememberMeKey, false);
        }

        /// <summary>
        /// Déclenche l'événement de changement de session.
        /// </summary>
        private void OnSessionChanged()
            => SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
