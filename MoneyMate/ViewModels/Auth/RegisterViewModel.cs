using System.Windows.Input;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Auth
{
    /// <summary>
    /// ViewModel pour la page d'inscription.
    /// Gère la création de compte, la validation et la navigation.
    /// </summary>
    public class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isEmailTaken;
        private bool _isRegisterErrorVisible;
        private string _registerErrorMessage = string.Empty;

        /// <summary>
        /// Pseudo de l'utilisateur (optionnel).
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// Email saisi par l'utilisateur.
        /// </summary>
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    IsEmailTaken = false;
                    IsRegisterErrorVisible = false;
                    ((Command)RegisterCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Mot de passe saisi par l'utilisateur.
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    IsRegisterErrorVisible = false;
                    ((Command)RegisterCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Confirmation du mot de passe.
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    IsRegisterErrorVisible = false;
                    ((Command)RegisterCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Indique si l'email est déjà utilisé.
        /// </summary>
        public bool IsEmailTaken
        {
            get => _isEmailTaken;
            set => SetProperty(ref _isEmailTaken, value);
        }

        /// <summary>
        /// Indique si le message d'erreur d'inscription est visible.
        /// </summary>
        public bool IsRegisterErrorVisible
        {
            get => _isRegisterErrorVisible;
            set => SetProperty(ref _isRegisterErrorVisible, value);
        }

        /// <summary>
        /// Message d'erreur affiché en cas d'échec d'inscription.
        /// </summary>
        public string RegisterErrorMessage
        {
            get => _registerErrorMessage;
            set => SetProperty(ref _registerErrorMessage, value);
        }

        /// <summary>
        /// Commande d'inscription.
        /// </summary>
        public ICommand RegisterCommand { get; }

        /// <summary>
        /// Commande de navigation vers la page de connexion.
        /// </summary>
        public ICommand GoToLoginCommand { get; }

        /// <summary>
        /// Commande de retour à la page d'accueil.
        /// </summary>
        public ICommand GoBackCommand { get; }

        public RegisterViewModel(IAuthenticationService authService, IDialogService dialogService, INavigationService navigationService)
        {
            _authService = authService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            Title = "Inscription";

            RegisterCommand  = new Command(async () => await RegisterAsync(), CanRegister);
            GoToLoginCommand = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Login));
            GoBackCommand    = new Command(async () => await _navigationService.NavigateToAsync(AppRoutes.Main));
        }

        /// <summary>
        /// Vérifie si la commande Register peut s'exécuter.
        /// </summary>
        private bool CanRegister()
            => !IsBusy
               && !string.IsNullOrWhiteSpace(Email)
               && !string.IsNullOrWhiteSpace(Password)
               && !string.IsNullOrWhiteSpace(ConfirmPassword);

        /// <summary>
        /// Enregistre un nouvel utilisateur via le service d'authentification.
        /// </summary>
        private async Task RegisterAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsRegisterErrorVisible = false;
                IsEmailTaken = false;

                if (!ValidationHelper.IsValidEmail(Email))
                {
                    RegisterErrorMessage   = "Format d'email invalide";
                    IsRegisterErrorVisible = true;
                    return;
                }

                if (!_authService.ValidatePasswordStrength(Password))
                {
                    RegisterErrorMessage   = "Le mot de passe ne respecte pas les critères de sécurité";
                    IsRegisterErrorVisible = true;
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    RegisterErrorMessage   = "Les mots de passe ne correspondent pas";
                    IsRegisterErrorVisible = true;
                    return;
                }

                var result = await _authService.RegisterAsync(Email.Trim(), Password);

                if (result.IsSuccess && result.Data != null)
                {
                    await _dialogService.ShowAlertAsync(
                        "Inscription réussie",
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "Votre compte a été créé avec succès. Vous pouvez maintenant vous connecter."
                            : result.Message,
                        "OK");

                    await _navigationService.NavigateToAsync(AppRoutes.Login);
                }
                else
                {
                    IsEmailTaken = result.ErrorCode == "AUTH_EMAIL_ALREADY_EXISTS";

                    RegisterErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "Une erreur est survenue lors de l'inscription."
                        : result.Message;

                    IsRegisterErrorVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RegisterAsync : {ex.Message}");
                RegisterErrorMessage   = "Une erreur est survenue. Veuillez réessayer.";
                IsRegisterErrorVisible = true;
            }
            finally
            {
                IsBusy = false;
                ((Command)RegisterCommand).ChangeCanExecute();
            }
        }
    }
}
