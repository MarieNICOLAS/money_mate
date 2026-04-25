using MoneyMate.Configuration;
using MoneyMate.Data.Context;
using MoneyMate.Services.Interfaces;
using System.Diagnostics;

namespace MoneyMate.Services.Implementations;

public sealed class StartupCoordinator : IStartupCoordinator
{
    private readonly IMoneyMateDbContext _dbContext;
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;

    public StartupCoordinator(
        IMoneyMateDbContext dbContext,
        IAuthenticationService authenticationService,
        INavigationService navigationService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        WarmUpLocalStorage();

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

    private void WarmUpLocalStorage()
    {
        _ = Task.Run(() =>
        {
            try
            {
                _dbContext.EnsureCreated();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur initialisation SQLite : {ex}");
            }
        });
    }
}
