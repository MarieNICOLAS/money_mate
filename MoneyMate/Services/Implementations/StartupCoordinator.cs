using MoneyMate.Configuration;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations;

public sealed class StartupCoordinator : IStartupCoordinator
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;

    public StartupCoordinator(
        IAuthenticationService authenticationService,
        INavigationService navigationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        bool hasSession;

        try
        {
            hasSession = await _authenticationService.RestoreSessionAsync(cancellationToken);
        }
        catch
        {
            hasSession = false;
        }

        await _navigationService.NavigateToAsync(hasSession ? AppRoutes.Dashboard : AppRoutes.Main);
    }
}
