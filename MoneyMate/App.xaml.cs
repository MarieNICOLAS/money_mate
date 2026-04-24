namespace MoneyMate;

using MoneyMate.Services.Interfaces;

public partial class App : Application
{
    private readonly IStartupCoordinator _startupCoordinator;
    private bool _startupCompleted;

    public App(AppShell appShell, IStartupCoordinator startupCoordinator)
    {
        InitializeComponent();

        _startupCoordinator = startupCoordinator ?? throw new ArgumentNullException(nameof(startupCoordinator));
        MainPage = appShell ?? throw new ArgumentNullException(nameof(appShell));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);
        window.Created += OnWindowCreated;
        return window;
    }

    private async void OnWindowCreated(object? sender, EventArgs e)
    {
        if (_startupCompleted)
            return;

        _startupCompleted = true;

        try
        {
            await _startupCoordinator.InitializeAsync();
        }
        catch
        {
        }
    }
}
