using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Interfaces;

using MoneyMate.Configuration;

namespace MoneyMate.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly SemaphoreSlim _navigationLock = new(1, 1);
        private readonly IAuthenticationService _authenticationService;

        public NavigationService(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        public Task NavigateToAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            string normalizedRoute = NormalizeRoute(route);
            return ExecuteShellNavigationAsync(() => NavigateWithinShellAsync(normalizedRoute));
        }

        public Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            parameters ??= [];

            string normalizedRoute = NormalizeRoute(route);
            return ExecuteShellNavigationAsync(() => NavigateWithinShellAsync(normalizedRoute, parameters));
        }

        public Task GoBackAsync()
            => ExecuteShellNavigationAsync(PopBackAsync);

        public Task NavigateToMainAsync()
            => NavigateToAsync(_authenticationService.IsAuthenticated ? AppRoutes.Dashboard : AppRoutes.Main);

        public Task PresentModalAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            string normalizedRoute = NormalizeRoute(route);

            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(normalizedRoute));
        }

        public Task DismissModalAsync()
            => ExecuteShellNavigationAsync(PopBackAsync);

        private async Task ExecuteShellNavigationAsync(Func<Task> navigationAction)
        {
            await _navigationLock.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current is null)
                        throw new InvalidOperationException("Shell.Current est indisponible.");

                    await navigationAction();
                });
            }
            finally
            {
                _navigationLock.Release();
            }
        }

        private static async Task PopBackAsync()
        {
            INavigation navigation = Shell.Current!.Navigation;

            if (navigation.ModalStack.Count > 0)
            {
                await navigation.PopModalAsync();
                return;
            }

            if (navigation.NavigationStack.Count > 1)
            {
                await navigation.PopAsync();
                return;
            }

            await Shell.Current.GoToAsync("..");
        }

        private static async Task NavigateWithinShellAsync(string route, Dictionary<string, object>? parameters = null)
        {
            if (await TryReuseFooterPageAsync(route))
                return;

            if (parameters is null || parameters.Count == 0)
                await Shell.Current!.GoToAsync(route);
            else
                await Shell.Current!.GoToAsync(route, parameters);
        }

        private static async Task<bool> TryReuseFooterPageAsync(string route)
        {
            string routeName = ExtractRouteName(route);
            if (!IsFooterRoute(routeName))
                return false;

            INavigation navigation = Shell.Current!.Navigation;
            Page? currentPage = navigation.NavigationStack.LastOrDefault() ?? Shell.Current.CurrentPage;
            if (MatchesRoute(currentPage, routeName))
                return true;

            Page? existingPage = navigation.NavigationStack.LastOrDefault(page => MatchesRoute(page, routeName));
            if (existingPage is null)
                return false;

            while (navigation.NavigationStack.LastOrDefault() != existingPage)
                await navigation.PopAsync(false);

            return true;
        }

        private static bool IsFooterRoute(string route)
            => string.Equals(route, "DashboardPage", StringComparison.Ordinal)
                || string.Equals(route, AppRoutes.ExpensesList, StringComparison.Ordinal)
                || string.Equals(route, AppRoutes.Calendar, StringComparison.Ordinal)
                || string.Equals(route, AppRoutes.BudgetsOverview, StringComparison.Ordinal)
                || string.Equals(route, AppRoutes.StatsOverview, StringComparison.Ordinal);

        private static bool MatchesRoute(Page? page, string routeName)
            => page is not null
                && string.Equals(page.GetType().Name, routeName, StringComparison.Ordinal);

        private static string ExtractRouteName(string route)
        {
            string trimmedRoute = route.Trim();
            int queryIndex = trimmedRoute.IndexOf('?');
            string routePath = queryIndex >= 0 ? trimmedRoute[..queryIndex] : trimmedRoute;
            return routePath.Trim('/');
        }

        private string NormalizeRoute(string route)
        {
            string normalizedRoute = AppRoutes.Normalize(route.Trim());

            if (!_authenticationService.CanAccessRoute(normalizedRoute))
                return AppRoutes.Login;

            return normalizedRoute;
        }
    }
}
