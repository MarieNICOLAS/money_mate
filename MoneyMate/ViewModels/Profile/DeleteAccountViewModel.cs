using System.Windows.Input;
using MoneyMate.Configuration;
using MoneyMate.Data.Context;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Profile
{
    /// <summary>
    /// ViewModel pour la suppression de compte.
    /// Exige la saisie du mot de passe et une double confirmation.
    /// </summary>
    public class DeleteAccountViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isErrorVisible;
        private string _errorMessage = string.Empty;

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    IsErrorVisible = false;
                    ((Command)DeleteAccountCommand).ChangeCanExecute();
                }
            }
        }

        public bool IsErrorVisible
        {
            get => _isErrorVisible;
            set => SetProperty(ref _isErrorVisible, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // ── Commandes ─────────────────────────────────────────────────
        public ICommand DeleteAccountCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand GoCalendarCommand { get; }
        public ICommand GoQuickAddExpenseCommand { get; }
        public ICommand GoBudgetCommand { get; }
        public ICommand GoProfileCommand { get; }

        public DeleteAccountViewModel(IAuthenticationService authService, IDialogService dialogService, INavigationService navigationService)
        {
            _authService = authService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            Title = "Supprimer le compte";

            DeleteAccountCommand = new Command(async () => await DeleteAccountAsync(), CanDelete);
            CancelCommand        = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Profile));
            LogoutCommand        = new Command(async () => await LogoutAsync());
            GoHomeCommand        = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Dashboard));
            GoCalendarCommand    = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Calendar));
            GoQuickAddExpenseCommand = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.QuickAddExpense));
            GoBudgetCommand      = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.BudgetsOverview));
            GoProfileCommand     = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Profile));
        }

        public void LoadUser()
        {
            var user = _authService.GetCurrentUser();
            UserName = user?.Email ?? "Utilisateur";
        }

        private bool CanDelete()
            => !IsBusy && !string.IsNullOrWhiteSpace(Password);

        private async Task DeleteAccountAsync()
        {
            if (IsBusy)
                return;

            try
            {
                // ── Première confirmation ──────────────────────────────
                bool confirm1 = await _dialogService.ShowConfirmationAsync(
                    "Supprimer le compte",
                    "Cette action est irréversible. Toutes vos données seront définitivement supprimées.",
                    "Continuer", "Annuler");

                if (!confirm1) return;

                // ── Deuxième confirmation ─────────────────────────────
                bool confirm2 = await _dialogService.ShowConfirmationAsync(
                    "Dernière confirmation",
                    "Êtes-vous vraiment sûr ? Il sera impossible de récupérer vos données.",
                    "Supprimer définitivement", "Annuler");

                if (!confirm2) return;

                IsBusy = true;
                IsErrorVisible = false;

                // ── Vérification du mot de passe ──────────────────────
                var user = _authService.GetCurrentUser();
                if (user == null)
                    return;

                var verified = await _authService.LoginAsync(user.Email, Password);
                if (!verified.IsSuccess)
                {
                    ErrorMessage   = "Mot de passe incorrect";
                    IsErrorVisible = true;
                    return;
                }

                // ── Suppression cascade (RGPD) ───────────────────────
                await _authService.LogoutAsync();

                using var dbContext = DbContextFactory.CreateDefault();
                dbContext.DeleteAllUserData(user.Id);

                await _dialogService.ShowAlertAsync(
                    "Compte supprimé",
                    "Votre compte et toutes vos données ont été supprimés.",
                    "OK");

                await _navigationService.NavigateToAsync(AppRoutes.Main);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteAccount : {ex.Message}");
                ErrorMessage   = "Une erreur est survenue. Veuillez réessayer.";
                IsErrorVisible = true;
            }
            finally
            {
                IsBusy = false;
                ((Command)DeleteAccountCommand).ChangeCanExecute();
            }
        }

        private async Task LogoutAsync()
        {
            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Déconnexion", "Voulez-vous vraiment vous déconnecter ?", "Oui", "Non");

            if (!confirm) return;

            await _authService.LogoutAsync();
            await _navigationService.NavigateToAsync(AppRoutes.Main);
        }
    }
}
