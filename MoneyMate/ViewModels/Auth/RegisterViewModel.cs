using System.Windows.Input;
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

        public RegisterViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Inscription";

            RegisterCommand  = new Command(async () => await RegisterAsync(), CanRegister);
            GoToLoginCommand = new Command(async () => await Shell.Current.GoToAsync("//LoginPage"));
            GoBackCommand    = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
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

                var user = await _authService.RegisterAsync(Email.Trim(), Password);

                if (user != null)
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Inscription réussie",
                        "Votre compte a été créé avec succès. Vous pouvez maintenant vous connecter.",
                        "OK");

                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    IsEmailTaken           = true;
                    RegisterErrorMessage   = "Cet email est déjà utilisé";
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
