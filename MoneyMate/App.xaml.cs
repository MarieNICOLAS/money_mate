using Microsoft.Extensions.DependencyInjection;
using MoneyMate.Services.Interfaces;

namespace MoneyMate
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authenticationService;

        private int _startupInitialized;

        public App(IServiceProvider serviceProvider, IAuthenticationService authenticationService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            AppShell appShell = _serviceProvider.GetRequiredService<AppShell>();
            Window window = new(appShell);

            window.Created += async (_, _) => await InitializeStartupAsync(appShell);

            return window;
        }

        private async Task InitializeStartupAsync(AppShell appShell)
        {
            if (Interlocked.Exchange(ref _startupInitialized, 1) == 1)
                return;

            try
            {
                await Task.Yield();

                await _authenticationService.RestoreSessionAsync();
                await appShell.InitializeForCurrentSessionAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[APP STARTUP] {ex}");
#endif
                await appShell.NavigateToPublicEntryAsync();
            }
        }
    }
}
