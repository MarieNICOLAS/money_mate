using System.Windows.Input;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Auth
{
    /// <summary>
    /// ViewModel pour la page de connexion.
    /// Gère l'authentification, le "Se souvenir de moi" et la navigation.
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _isLoginErrorVisible;
        private string _loginErrorMessage = string.Empty;

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
                    IsLoginErrorVisible = false;
                    ((Command)LoginCommand).ChangeCanExecute();
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
                    IsLoginErrorVisible = false;
                    ((Command)LoginCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Indique si l'utilisateur souhaite rester connecté.
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>
        /// Indique si le message d'erreur de connexion est visible.
        /// </summary>
        public bool IsLoginErrorVisible
        {
            get => _isLoginErrorVisible;
            set => SetProperty(ref _isLoginErrorVisible, value);
        }

        /// <summary>
        /// Message d'erreur affiché en cas d'échec de connexion.
        /// </summary>
        public string LoginErrorMessage
        {
            get => _loginErrorMessage;
            set => SetProperty(ref _loginErrorMessage, value);
        }

        /// <summary>
        /// Commande de connexion.
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// Commande de navigation vers la page d'inscription.
        /// </summary>
        public ICommand GoToRegisterCommand { get; }

        /// <summary>
        /// Commande de retour à la page d'accueil.
        /// </summary>
        public ICommand GoBackCommand { get; }

        public LoginViewModel(IAuthenticationService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Connexion";

            LoginCommand        = new Command(async () => await LoginAsync(), CanLogin);
            GoToRegisterCommand = new Command(async () => await _navigationService.NavigateToAsync("//RegisterPage"));
            GoBackCommand       = new Command(async () => await _navigationService.NavigateToAsync("//MainPage"));
        }

        /// <summary>
        /// Vérifie si la commande Login peut s'exécuter.
        /// </summary>
        private bool CanLogin()
            => !IsBusy
               && !string.IsNullOrWhiteSpace(Email)
               && !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        /// Authentifie l'utilisateur via le service d'authentification.
        /// En cas de succès, sauvegarde les préférences si "Se souvenir de moi" est coché
        /// et navigue vers le tableau de bord.
        /// </summary>
        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsLoginErrorVisible = false;

                var result = await _authService.LoginAsync(Email.Trim(), Password, RememberMe);

                if (result.IsSuccess && result.Data != null)
                {
                    await _navigationService.NavigateToMainAsync();
                }
                else
                {
                    LoginErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "Email ou mot de passe incorrect."
                        : result.Message;

                    IsLoginErrorVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur LoginAsync : {ex.Message}");
                LoginErrorMessage   = "Une erreur est survenue. Veuillez réessayer.";
                IsLoginErrorVisible = true;
            }
            finally
            {
                IsBusy = false;
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }

        /// <summary>
        /// Charge les préférences "Se souvenir de moi" au démarrage de la page.
        /// </summary>
        public void LoadRememberMe()
        {
            bool remembered = _authService.GetRememberMePreference();

            if (remembered)
            {
                Email      = _authService.GetRememberedEmail();
                RememberMe = true;
            }
        }
    }
}
