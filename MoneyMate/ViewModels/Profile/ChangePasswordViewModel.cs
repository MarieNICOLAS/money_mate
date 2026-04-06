using System.Windows.Input;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Profile
{
    /// <summary>
    /// ViewModel pour le changement de mot de passe.
    /// </summary>
    public class ChangePasswordViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;

        private string _userName = string.Empty;
        private string _oldPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private bool _isErrorVisible;
        private string _errorMessage = string.Empty;

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string OldPassword
        {
            get => _oldPassword;
            set
            {
                if (SetProperty(ref _oldPassword, value))
                {
                    IsErrorVisible = false;
                    ((Command)ChangePasswordCommand).ChangeCanExecute();
                }
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    IsErrorVisible = false;
                    ((Command)ChangePasswordCommand).ChangeCanExecute();
                }
            }
        }

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set
            {
                if (SetProperty(ref _confirmNewPassword, value))
                {
                    IsErrorVisible = false;
                    ((Command)ChangePasswordCommand).ChangeCanExecute();
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
        public ICommand ChangePasswordCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand GoExpensesCommand { get; }
        public ICommand GoBudgetCommand { get; }
        public ICommand GoProfileCommand { get; }

        public ChangePasswordViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Changer le mot de passe";

            ChangePasswordCommand = new Command(async () => await ChangePasswordAsync(), CanChangePassword);
            LogoutCommand         = new Command(async () => await LogoutAsync());
            GoHomeCommand         = new Command(async () => await Shell.Current.GoToAsync("//DashboardPage"));
            GoExpensesCommand     = new Command(async () => await Shell.Current.GoToAsync("//ExpensesListPage"));
            GoBudgetCommand       = new Command(async () => await Shell.Current.GoToAsync("//BudgetsOverviewPage"));
            GoProfileCommand      = new Command(async () => await Shell.Current.GoToAsync("//ProfilePage"));
        }

        public void LoadUser()
        {
            var user = _authService.GetCurrentUser();
            UserName = user?.Email ?? "Utilisateur";
        }

        private bool CanChangePassword()
            => !IsBusy
               && !string.IsNullOrWhiteSpace(OldPassword)
               && !string.IsNullOrWhiteSpace(NewPassword)
               && !string.IsNullOrWhiteSpace(ConfirmNewPassword);

        private async Task ChangePasswordAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsErrorVisible = false;

                if (!_authService.ValidatePasswordStrength(NewPassword))
                {
                    ErrorMessage   = "Le nouveau mot de passe ne respecte pas les critères de sécurité";
                    IsErrorVisible = true;
                    return;
                }

                if (NewPassword != ConfirmNewPassword)
                {
                    ErrorMessage   = "Les mots de passe ne correspondent pas";
                    IsErrorVisible = true;
                    return;
                }

                var user = _authService.GetCurrentUser();
                if (user == null)
                    return;

                bool success = await _authService.ChangePasswordAsync(user.Id, OldPassword, NewPassword);

                if (success)
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Succès",
                        "Votre mot de passe a été modifié avec succès.",
                        "OK");

                    await Shell.Current.GoToAsync("//ProfilePage");
                }
                else
                {
                    ErrorMessage   = "L'ancien mot de passe est incorrect";
                    IsErrorVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ChangePassword : {ex.Message}");
                ErrorMessage   = "Une erreur est survenue. Veuillez réessayer.";
                IsErrorVisible = true;
            }
            finally
            {
                IsBusy = false;
                ((Command)ChangePasswordCommand).ChangeCanExecute();
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
