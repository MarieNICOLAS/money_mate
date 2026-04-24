using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Input;
using MoneyMate.Configuration;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Auth;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _isLoginErrorVisible;
    private string _loginErrorMessage = string.Empty;

    public LoginViewModel(
        IAuthenticationService authService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _authService = authService;
        _dialogService = dialogService;
        _navigationService = navigationService;
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public bool IsLoginErrorVisible
    {
        get => _isLoginErrorVisible;
        set => SetProperty(ref _isLoginErrorVisible, value);
    }

    public string LoginErrorMessage
    {
        get => _loginErrorMessage;
        set => SetProperty(ref _loginErrorMessage, value);
    }

    public void LoadRememberMe()
    {
        RememberMe = _authService.GetRememberMePreference();
        Email = _authService.GetRememberedEmail();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            IsLoginErrorVisible = false;
            LoginErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Email, Password, RememberMe);

            if (result.IsSuccess)
            {
                await _navigationService.NavigateToAsync(AppRoutes.Dashboard);
                return;
            }

            LoginErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Connexion impossible."
                : result.Message;
            IsLoginErrorVisible = true;
        }
        catch
        {
            LoginErrorMessage = "Une erreur est survenue lors de la connexion.";
            IsLoginErrorVisible = true;
            await _dialogService.ShowAlertAsync("Connexion", LoginErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
