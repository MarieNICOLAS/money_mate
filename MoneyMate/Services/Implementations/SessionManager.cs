using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Security;

namespace MoneyMate.Services.Implementations
{
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
            ["expenses-list"] = [UserRoles.User, UserRoles.Admin],
            ["AddExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["EditExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["ExpenseDetailsPage"] = [UserRoles.User, UserRoles.Admin],
            ["ExpenseFilterPage"] = [UserRoles.User, UserRoles.Admin],
            ["expense-filter"] = [UserRoles.User, UserRoles.Admin],
            ["QuickAddExpensePage"] = [UserRoles.User, UserRoles.Admin],
            ["CategoriesListPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddCategoryPage"] = [UserRoles.User, UserRoles.Admin],
            ["EditCategoryPage"] = [UserRoles.User, UserRoles.Admin],
            ["BudgetsOverviewPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddBudgetPage"] = [UserRoles.User, UserRoles.Admin],
            ["EditBudgetPage"] = [UserRoles.User, UserRoles.Admin],
            ["AlertThresholdPage"] = [UserRoles.User, UserRoles.Admin],
            ["FixedChargesPage"] = [UserRoles.User, UserRoles.Admin],
            ["AddFixedChargePage"] = [UserRoles.User, UserRoles.Admin],
            ["EditFixedChargePage"] = [UserRoles.User, UserRoles.Admin],
            ["CalendarPage"] = [UserRoles.User, UserRoles.Admin],
            ["StatsOverviewPage"] = [UserRoles.User, UserRoles.Admin]
        };

        private readonly IMoneyMateDbContext _dbContext;
        private readonly SemaphoreSlim _sessionLock = new(1, 1);

        private User? _currentUser;

        public SessionManager()
            : this(DbContextFactory.CreateDefault())
        {
        }

        public SessionManager(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public event EventHandler? SessionChanged;

        public User? CurrentUser => _currentUser;

        public bool IsAuthenticated => _currentUser is not null;

        public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default)
        {
            await _sessionLock.WaitAsync(cancellationToken);

            try
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

                User? user = await Task.Run(() => _dbContext.GetUserById(userId), cancellationToken);

                if (user is null || !user.IsActive)
                {
                    ClearSession();
                    return false;
                }

                _currentUser = user;
                OnSessionChanged();
                return true;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public void StartSession(User user, bool rememberSession)
        {
            ArgumentNullException.ThrowIfNull(user);

            _currentUser = user;

            if (rememberSession)
            {
                Preferences.Set(RememberMeKey, true);
                Preferences.Set(RememberedEmailKey, user.Email);
                Preferences.Set(SessionUserIdKey, user.Id);
            }
            else
            {
                Preferences.Remove(SessionUserIdKey);
                Preferences.Set(RememberMeKey, false);
                Preferences.Set(RememberedEmailKey, user.Email);
            }

            OnSessionChanged();
        }

        public void UpdateCurrentUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            _currentUser = user;

            if (Preferences.Get(RememberMeKey, false))
            {
                Preferences.Set(RememberedEmailKey, user.Email);
                Preferences.Set(SessionUserIdKey, user.Id);
            }

            OnSessionChanged();
        }

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

        public string GetRememberedEmail()
            => Preferences.Get(RememberedEmailKey, string.Empty);

        public bool GetRememberMePreference()
            => Preferences.Get(RememberMeKey, false);

        public bool HasRole(params string[] roles)
        {
            if (_currentUser is null || roles is null || roles.Length == 0)
                return false;

            return roles.Any(role =>
                string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase));
        }

        public bool CanAccessRoute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            string normalizedRoute = NormalizeRoute(route);

            if (PublicRoutes.Contains(normalizedRoute))
                return true;

            if (!RoutePermissions.TryGetValue(normalizedRoute, out string[]? allowedRoles))
                return true;

            if (!IsAuthenticated)
                return false;

            return HasRole(allowedRoles);
        }

        private static string NormalizeRoute(string route)
            => route.Trim().Trim('/').Split('/').LastOrDefault() ?? string.Empty;

        private static void ClearTransientSessionData()
        {
            Preferences.Remove(RememberedEmailKey);
            Preferences.Remove(SessionUserIdKey);
            Preferences.Set(RememberMeKey, false);
        }

        private void OnSessionChanged()
            => SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
