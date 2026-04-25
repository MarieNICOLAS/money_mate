using System.Windows.Input;
using MoneyMate.Configuration;
using MoneyMate.Infrastructure;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.Components;

/// <summary>
/// Header affich� sur les pages authentifi�es.
/// Affiche le nom de l'utilisateur connect� et un bouton de d�connexion.
/// </summary>
public partial class AuthenticatedHeader : ContentView
{
    private readonly IAuthenticationService? _authenticationService;
    private readonly INavigationService? _navigationService;

    /// <summary>Nom de l'utilisateur connect�.</summary>
    public static readonly BindableProperty UserNameProperty =
        BindableProperty.Create(nameof(UserName), typeof(string), typeof(AuthenticatedHeader), string.Empty);

    /// <summary>Commande ex�cut�e lors du clic sur D�connexion.</summary>
    public static readonly BindableProperty LogoutCommandProperty =
        BindableProperty.Create(nameof(LogoutCommand), typeof(ICommand), typeof(AuthenticatedHeader), null);

    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(AuthenticatedHeader), false);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(AuthenticatedHeader), null);

    public static readonly BindableProperty PageTitleProperty =
        BindableProperty.Create(nameof(PageTitle), typeof(string), typeof(AuthenticatedHeader), string.Empty);

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    public ICommand? LogoutCommand
    {
        get => (ICommand?)GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    public AuthenticatedHeader()
    {
        try
        {
            _authenticationService = ServiceResolver.GetRequiredService<IAuthenticationService>();
            _navigationService = ServiceResolver.GetRequiredService<INavigationService>();
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            RefreshUserName();
        }
        catch
        {
        }

        LogoutCommand ??= new Command(async () => await LogoutAsync());
        BackCommand ??= new Command(async () => await GoBackAsync());
        InitializeComponent();
    }

    private void OnAuthenticationStateChanged(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(RefreshUserName);

    private void RefreshUserName()
        => UserName = _authenticationService?.GetCurrentUser()?.Email ?? string.Empty;

    private async Task LogoutAsync()
    {
        if (_authenticationService == null || _navigationService == null)
            return;

        await _authenticationService.LogoutAsync(true);
        await _navigationService.NavigateToAsync(AppRoutes.Main);
    }

    private async Task GoBackAsync()
    {
        if (_navigationService == null)
            return;

        try
        {
            await _navigationService.GoBackAsync();
        }
        catch
        {
            await _navigationService.NavigateToAsync(AppRoutes.Dashboard);
        }
    }
}
