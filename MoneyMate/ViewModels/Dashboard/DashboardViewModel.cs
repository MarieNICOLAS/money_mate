using System.Windows.Input;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel pour le tableau de bord.
    /// Affiche les infos de l'utilisateur connecté et gère la déconnexion.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private string _userName = string.Empty;

        /// <summary>
        /// Nom affiché dans le header (email de l'utilisateur).
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// Commande de déconnexion.
        /// </summary>
        public ICommand LogoutCommand { get; }

        /// <summary>
        /// Commandes de navigation du footer.
        /// </summary>
        public ICommand GoHomeCommand { get; }
        public ICommand GoExpensesCommand { get; }
        public ICommand GoBudgetCommand { get; }
        public ICommand GoProfileCommand { get; }

        public DashboardViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Tableau de Bord";

            LogoutCommand     = new Command(async () => await LogoutAsync());
            GoHomeCommand     = new Command(async () => await Shell.Current.GoToAsync("//DashboardPage"));
            GoExpensesCommand = new Command(async () => await Shell.Current.GoToAsync("//ExpensesPage"));
            GoBudgetCommand   = new Command(async () => await Shell.Current.GoToAsync("//BudgetPage"));
            GoProfileCommand  = new Command(async () => await Shell.Current.GoToAsync("//ProfilePage"));
        }

        /// <summary>
        /// Charge les données de l'utilisateur connecté.
        /// </summary>
        public void LoadUser()
        {
            var user = _authService.GetCurrentUser();
            UserName = user?.Email ?? "Utilisateur";
        }

        /// <summary>
        /// Déconnecte l'utilisateur et redirige vers l'accueil.
        /// </summary>
        private async Task LogoutAsync()
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Déconnexion",
                "Voulez-vous vraiment vous déconnecter ?",
                "Oui", "Non");

            if (!confirm)
                return;

            await _authService.LogoutAsync();

            Preferences.Remove("remember_email");
            Preferences.Set("remember_me", false);

            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
