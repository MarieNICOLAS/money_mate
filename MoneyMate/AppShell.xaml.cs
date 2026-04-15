using MoneyMate.Services.Interfaces;
using MoneyMate.Views.Budgets;
using System.Diagnostics;

namespace MoneyMate
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly SemaphoreSlim _navigationLock = new(1, 1);

        private bool _isInitialized;
        private bool _isRedirecting;

        private const string DashboardRoute = "//DashboardPage";
        private const string LoginRoute = "//LoginPage";
        private const string PublicEntryRoute = "//MainPage";

        public AppShell(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

            InitializeComponent();
            RegisterRoutes();

            Navigating += OnShellNavigating;
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;

            UpdateFlyoutItemsVisibility();
        }

        private static void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(AddBudgetPage), typeof(AddBudgetPage));
            Routing.RegisterRoute(nameof(EditBudgetPage), typeof(EditBudgetPage));
        }

        public async Task InitializeForCurrentSessionAsync()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            UpdateFlyoutItemsVisibility();

            string targetRoute = _authenticationService.IsAuthenticated
                ? DashboardRoute
                : PublicEntryRoute;

            await NavigateSafelyAsync(targetRoute);
        }

        public Task NavigateToPublicEntryAsync()
            => NavigateSafelyAsync(PublicEntryRoute);

        private async void OnAuthenticationStateChanged(object? sender, EventArgs e)
        {
            try
            {
                UpdateFlyoutItemsVisibility();

                if (_authenticationService.IsAuthenticated)
                    return;

                string currentRoute = GetCurrentRoute();
                if (_authenticationService.CanAccessRoute(currentRoute))
                    return;

                await NavigateSafelyAsync(LoginRoute);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[AUTH STATE CHANGED] {ex}");
#endif
            }
        }

        private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            try
            {
                if (_isRedirecting)
                    return;

                string targetRoute = GetRouteFromLocation(e.Target.Location.OriginalString);

                if (string.IsNullOrWhiteSpace(targetRoute))
                    return;

                if (_authenticationService.CanAccessRoute(targetRoute))
                    return;

                e.Cancel();

                if (_authenticationService.IsAuthenticated)
                {
                    await ShowAlertAsync(
                        "Accès refusé",
                        "Votre rôle actuel ne permet pas d'accéder à cette page.");
                    return;
                }

                await ShowAlertAsync(
                    "Session requise",
                    "Vous devez être connecté pour accéder à cette page.");

                await NavigateSafelyAsync(LoginRoute);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[SHELL NAVIGATING] {ex}");
#endif
            }
        }

        private async Task NavigateSafelyAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return;

            await _navigationLock.WaitAsync();

            try
            {
                if (_isRedirecting)
                    return;

                string currentRoute = GetCurrentRoute();
                string normalizedTarget = NormalizeRoute(route);

                if (string.Equals(currentRoute, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    return;

                _isRedirecting = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await GoToAsync(route);
                });
            }
            finally
            {
                _isRedirecting = false;
                _navigationLock.Release();
            }
        }

        private async Task ShowAlertAsync(string title, string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = Current?.CurrentPage ?? Shell.Current?.CurrentPage;
                if (page is not null)
                    await page.DisplayAlert(title, message, "OK");
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

            if (flyoutItem is not null)
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

        private static string NormalizeRoute(string route)
            => route.Trim().Trim('/').Split('/').LastOrDefault() ?? string.Empty;
    }
}
