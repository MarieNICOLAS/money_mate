using MoneyMate.Helpers;
using MoneyMate.Services.Interfaces;
using MoneyMate.Views;

namespace MoneyMate
{
    /// <summary>
    /// Page d'accueil de l'application.
    /// Point d'entrée pour la connexion et l'inscription.
    /// </summary>
    public partial class MainPage : BasePage
    {
        private readonly INavigationService _navigationService;

        public MainPage(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await _navigationService.NavigateToAsync("//LoginPage");
        }

        private async void OnSignupClicked(object sender, EventArgs e)
        {
            await _navigationService.NavigateToAsync("//RegisterPage");
        }
    }
}
