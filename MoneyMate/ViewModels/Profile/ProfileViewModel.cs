using System.Windows.Input;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Profile
{
    /// <summary>
    /// ViewModel pour la page de profil utilisateur.
    /// Affiche les informations du compte et permet la navigation vers les sous-pages.
    /// </summary>
    public class ProfileViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        private string _userName = string.Empty;
        private string _email = string.Empty;
        private string _devise = string.Empty;
        private int _budgetStartDay;
        private string _memberSince = string.Empty;

        /// <summary>
        /// Nom affiché dans le header.
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// Email de l'utilisateur.
        /// </summary>
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        /// <summary>
        /// Devise préférée.
        /// </summary>
        public string Devise
        {
            get => _devise;
            set => SetProperty(ref _devise, value);
        }

        /// <summary>
        /// Jour de début du cycle budgétaire.
        /// </summary>
        public int BudgetStartDay
        {
            get => _budgetStartDay;
            set => SetProperty(ref _budgetStartDay, value);
        }

        /// <summary>
        /// Date d'inscription formatée.
        /// </summary>
        public string MemberSince
        {
            get => _memberSince;
            set => SetProperty(ref _memberSince, value);
        }

        // ── Commandes ─────────────────────────────────────────────────
        public ICommand LogoutCommand { get; }
        public ICommand GoToChangePasswordCommand { get; }
        public ICommand GoToDeleteAccountCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand GoExpensesCommand { get; }
        public ICommand GoBudgetCommand { get; }
        public ICommand GoProfileCommand { get; }

        public ProfileViewModel(IAuthenticationService authService, IDialogService dialogService, INavigationService navigationService)
        {
            _authService = authService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            Title = "Mon Profil";

            LogoutCommand              = new Command(async () => await LogoutAsync());
            GoToChangePasswordCommand  = new Command(async () => await _navigationService.NavigateToAsync("//ChangePasswordPage"));
            GoToDeleteAccountCommand   = new Command(async () => await _navigationService.NavigateToAsync("//DeleteAccountPage"));
            GoHomeCommand              = new Command(async () => await _navigationService.NavigateToAsync("//DashboardPage"));
            GoExpensesCommand          = new Command(async () => await _navigationService.NavigateToAsync("//ExpensesListPage"));
            GoBudgetCommand            = new Command(async () => await _navigationService.NavigateToAsync("//BudgetsOverviewPage"));
            GoProfileCommand           = new Command(async () => await _navigationService.NavigateToAsync("//ProfilePage"));
        }

        /// <summary>
        /// Charge les données de l'utilisateur connecté.
        /// </summary>
        public void LoadUser()
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
                return;

            UserName       = user.Email;
            Email          = user.Email;
            Devise         = user.Devise;
            BudgetStartDay = user.BudgetStartDay;
            MemberSince    = user.CreatedAt.ToString("dd MMMM yyyy");
        }

        /// <summary>
        /// Déconnecte l'utilisateur et redirige vers l'accueil.
        /// </summary>
        private async Task LogoutAsync()
        {
            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Déconnexion",
                "Voulez-vous vraiment vous déconnecter ?",
                "Oui", "Non");

            if (!confirm)
                return;

            await _authService.LogoutAsync();

            await _navigationService.NavigateToAsync("//MainPage");
        }
    }
}
