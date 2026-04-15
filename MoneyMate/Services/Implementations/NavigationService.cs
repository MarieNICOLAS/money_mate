using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly SemaphoreSlim _navigationLock = new(1, 1);

        public Task NavigateToAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(route));
        }

        public Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            parameters ??= [];

            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(route, parameters));
        }

        public Task GoBackAsync()
            => ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(".."));

        public Task NavigateToMainAsync()
            => ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync("//DashboardPage"));

        public Task PresentModalAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route de navigation est requise.", nameof(route));

            return ExecuteShellNavigationAsync(() => Shell.Current!.GoToAsync(route));
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
    }
}
