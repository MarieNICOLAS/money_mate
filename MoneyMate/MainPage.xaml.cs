using MoneyMate.Helpers;
using MoneyMate.Views;

namespace MoneyMate
{
    /// <summary>
    /// Page d'accueil de l'application.
    /// Point d'entrée pour la connexion et l'inscription.
    /// </summary>
    public partial class MainPage : BasePage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Redirige vers la page de connexion.
        /// </summary>
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        /// <summary>
        /// Redirige vers la page d'inscription.
        /// </summary>
        private async void OnSignupClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}
