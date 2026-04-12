using MoneyMate.Services.Interfaces;
using MoneyMate.Views.Budgets;

namespace MoneyMate
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authenticationService;
        private bool _isRedirectingToLogin;

        public AppShell(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;

            InitializeComponent();

            Routing.RegisterRoute(nameof(AddBudgetPage), typeof(AddBudgetPage));
            Routing.RegisterRoute(nameof(EditBudgetPage), typeof(EditBudgetPage));

            Navigating += OnShellNavigating;
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;

            UpdateFlyoutItemsVisibility();
        }

        /// <summary>
        /// Initialise la navigation Shell à partir de l'état courant de session.
        /// </summary>
        public void InitializeForCurrentSession()
        {
            UpdateFlyoutItemsVisibility();

            string targetRoute = _authenticationService.IsAuthenticated
                ? "//DashboardPage"
                : "//MainPage";

            Dispatcher.Dispatch(async () =>
            {
                _isRedirectingToLogin = true;
                try
                {
                    await GoToAsync(targetRoute);
                }
                finally
                {
                    _isRedirectingToLogin = false;
                }
            });
        }

        private void OnAuthenticationStateChanged(object? sender, EventArgs e)
        {
            Dispatcher.Dispatch(async () =>
            {
                UpdateFlyoutItemsVisibility();

                if (_authenticationService.IsAuthenticated)
                    return;

                string currentRoute = GetCurrentRoute();
                if (_authenticationService.CanAccessRoute(currentRoute))
                    return;

                _isRedirectingToLogin = true;
                try
                {
                    await GoToAsync("//LoginPage");
                }
                finally
                {
                    _isRedirectingToLogin = false;
                }
            });
        }

        private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            if (_isRedirectingToLogin)
                return;

            string targetRoute = GetRouteFromLocation(e.Target.Location.OriginalString);
            if (string.IsNullOrWhiteSpace(targetRoute) || _authenticationService.CanAccessRoute(targetRoute))
                return;

            e.Cancel();

            Dispatcher.Dispatch(async () =>
            {
                _isRedirectingToLogin = true;
                try
                {
                    if (_authenticationService.IsAuthenticated)
                    {
                        await Current.DisplayAlert(
                            "Accès refusé",
                            "Votre rôle actuel ne permet pas d'accéder à cette page.",
                            "OK");
                    }
                    else
                    {
                        await Current.DisplayAlert(
                            "Session requise",
                            "Vous devez être connecté pour accéder à cette page.",
                            "OK");

                        await GoToAsync("//LoginPage");
                    }
                }
                finally
                {
                    _isRedirectingToLogin = false;
                }
            });
        }

        private void UpdateFlyoutItemsVisibility()
        {
            bool isAuthenticated = _authenticationService.IsAuthenticated;

            SetFlyoutItemVisibility("Authentification", !isAuthenticated);
            SetFlyoutItemVisibility("Tableau de Bord", isAuthenticated);
            SetFlyoutItemVisibility("Mon Compte", isAuthenticated);
            SetFlyoutItemVisibility("Depenses", isAuthenticated);
            SetFlyoutItemVisibility("Categories", isAuthenticated);
            SetFlyoutItemVisibility("Budgets", isAuthenticated);
            SetFlyoutItemVisibility("Charges fixes", isAuthenticated);
            SetFlyoutItemVisibility("Alertes", isAuthenticated);
            SetFlyoutItemVisibility("Calendrier", isAuthenticated);
        }

        private void SetFlyoutItemVisibility(string title, bool isVisible)
        {
            FlyoutItem? flyoutItem = Items
                .OfType<FlyoutItem>()
                .FirstOrDefault(item => string.Equals(item.Title, title, StringComparison.Ordinal));

            if (flyoutItem != null)
                flyoutItem.FlyoutItemIsVisible = isVisible;
        }

        private string GetCurrentRoute()
            => CurrentItem?.CurrentItem?.Route ?? string.Empty;

        private static string GetRouteFromLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return string.Empty;

            string route = location.Split('?', '#')[0].Trim('/');
            if (string.IsNullOrWhiteSpace(route))
                return string.Empty;

            string[] segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length == 0 ? string.Empty : segments[^1];
        }
    }
}
