using System.Diagnostics;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels;

/// <summary>
/// Base commune pour les ViewModels des modules métier nécessitant une session utilisateur.
/// </summary>
public abstract class AuthenticatedViewModelBase : BaseViewModel
{
    private string _errorMessage = string.Empty;

    protected AuthenticatedViewModelBase(
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        AuthenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    protected IAuthenticationService AuthenticationService { get; }

    protected IDialogService DialogService { get; }

    protected INavigationService NavigationService { get; }

    protected User? CurrentUser => AuthenticationService.GetCurrentUser();

    protected int CurrentUserId => CurrentUser?.Id ?? 0;

    protected string CurrentDevise => CurrentUser?.Devise ?? "EUR";

    public string ErrorMessage
    {
        get => _errorMessage;
        protected set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected bool EnsureCurrentUser()
    {
        if (CurrentUserId > 0)
            return true;

        ErrorMessage = "Aucune session utilisateur active.";
        return false;
    }

    protected async Task ExecuteBusyActionAsync(Func<Task> action, string unexpectedErrorMessage)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = unexpectedErrorMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
