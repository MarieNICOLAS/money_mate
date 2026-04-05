using System.Windows.Input;
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
        public ICommand GoExpensesCommand { get; }
        public ICommand GoBudgetCommand { get; }
        public ICommand GoProfileCommand { get; }

        public DeleteAccountViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Supprimer le compte";

            DeleteAccountCommand = new Command(async () => await DeleteAccountAsync(), CanDelete);
            CancelCommand        = new Command(async () => await Shell.Current.GoToAsync("//ProfilePage"));
            LogoutCommand        = new Command(async () => await LogoutAsync());
            GoHomeCommand        = new Command(async () => await Shell.Current.GoToAsync("//DashboardPage"));
            GoExpensesCommand    = new Command(async () => await Shell.Current.GoToAsync("//ExpensesListPage"));
            GoBudgetCommand      = new Command(async () => await Shell.Current.GoToAsync("//BudgetsOverviewPage"));
            GoProfileCommand     = new Command(async () => await Shell.Current.GoToAsync("//ProfilePage"));
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
                bool confirm1 = await Application.Current!.MainPage!.DisplayAlert(
                    "Supprimer le compte",
                    "Cette action est irréversible. Toutes vos données seront définitivement supprimées.",
                    "Continuer", "Annuler");

                if (!confirm1) return;

                // ── Deuxième confirmation ─────────────────────────────
                bool confirm2 = await Application.Current.MainPage.DisplayAlert(
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
                if (verified == null)
                {
                    ErrorMessage   = "Mot de passe incorrect";
                    IsErrorVisible = true;
                    return;
                }

                // ── Suppression cascade (RGPD) ───────────────────────
                await _authService.LogoutAsync();

                var dbContext = DatabaseService.Instance;
                dbContext.DeleteAllUserData(user.Id);

                Preferences.Remove("remember_email");
                Preferences.Set("remember_me", false);

                await Application.Current.MainPage.DisplayAlert(
                    "Compte supprimé",
                    "Votre compte et toutes vos données ont été supprimés.",
                    "OK");

                await Shell.Current.GoToAsync("//MainPage");
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
            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Déconnexion", "Voulez-vous vraiment vous déconnecter ?", "Oui", "Non");

            if (!confirm) return;

            await _authService.LogoutAsync();
            Preferences.Remove("remember_email");
            Preferences.Set("remember_me", false);
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
