using MoneyMate.ViewModels;

namespace MoneyMate.Views
{
    /// <summary>
    /// Page de base pour toutes les vues de l'application
    /// Garantit l'homogeneite et le binding automatique au ViewModel
    /// </summary>
    public abstract class BasePage : ContentPage
    {
        protected BasePage()
        {
            BackgroundColor = Color.FromArgb("#FFF7F0");
        }

        /// <summary>
        /// Configure le ViewModel de la page
        /// </summary>
        protected void SetViewModel(BaseViewModel viewModel)
        {
            BindingContext = viewModel;
        }
    }

    /// <summary>
    /// Page de base generique avec ViewModel type
    /// </summary>
    /// <typeparam name="TViewModel">Type du ViewModel associe</typeparam>
    public abstract class BasePage<TViewModel> : BasePage where TViewModel : BaseViewModel
    {
        protected TViewModel ViewModel => (TViewModel)BindingContext;

        protected BasePage(TViewModel viewModel) : base()
        {
            SetViewModel(viewModel);
        }
    }
}