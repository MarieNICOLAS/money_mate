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
            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(normalizedRoute));
        }

        public Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            parameters ??= [];

            string normalizedRoute = NormalizeRoute(route);

            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(normalizedRoute, parameters));
        }

        public Task GoBackAsync()
            => ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(".."));

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
            => ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(".."));

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

        private string NormalizeRoute(string route)
        {
            string normalizedRoute = AppRoutes.Normalize(route.Trim());

            if (!_authenticationService.CanAccessRoute(normalizedRoute))
                return AppRoutes.Login;

            return normalizedRoute;
        }
    }
}
