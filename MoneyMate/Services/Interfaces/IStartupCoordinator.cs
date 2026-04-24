namespace MoneyMate.Services.Interfaces;

public interface IStartupCoordinator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
