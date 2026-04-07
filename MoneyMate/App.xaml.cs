using Microsoft.Extensions.DependencyInjection;
using MoneyMate.Services.Interfaces;

namespace MoneyMate
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authenticationService;

        public App(IServiceProvider serviceProvider, IAuthenticationService authenticationService)
        {
            _serviceProvider = serviceProvider;
            _authenticationService = authenticationService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _authenticationService.RestoreSession();

            AppShell appShell = _serviceProvider.GetRequiredService<AppShell>();
            Window window = new(appShell);

            appShell.InitializeForCurrentSession();

            return window;
        }
    }
}
