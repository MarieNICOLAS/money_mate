using MoneyMate.Components;
using MoneyMate.ViewModels;

namespace MoneyMate.Views
{
    /// <summary>
    /// Page de base pour toutes les vues de l'application.
    /// Impose le squelette Header / Contenu / Footer et le binding au ViewModel.
    /// Compatible avec les pages filles en XAML.
    /// </summary>
    public abstract class BasePage : ContentPage
    {
        private readonly ContentView _contentSlot = new()
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        /// <summary>
        /// Contenu central de la page (entre le header et le footer).
        /// Utilisé par les pages filles via XAML ou code-behind.
        /// </summary>
        public static readonly BindableProperty PageContentProperty =
            BindableProperty.Create(
                nameof(PageContent),
                typeof(View),
                typeof(BasePage),
                null,
                propertyChanged: OnPageContentChanged);

        public View? PageContent
        {
            get => (View?)GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        private static void OnPageContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BasePage page && newValue is View view)
                page._contentSlot.Content = view;
        }

        protected BasePage()
        {
            var header = new Header();
            var footer = new Footer();

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            Grid.SetRow(header, 0);
            Grid.SetRow(_contentSlot, 1);
            Grid.SetRow(footer, 2);

            grid.Children.Add(header);
            grid.Children.Add(_contentSlot);
            grid.Children.Add(footer);

            Content = grid;
        }

        /// <summary>
        /// Configure le ViewModel de la page.
        /// </summary>
        protected void SetViewModel(BaseViewModel viewModel)
        {
            BindingContext = viewModel;
        }
    }

    /// <summary>
    /// Page de base générique avec ViewModel typé.
    /// </summary>
    /// <typeparam name="TViewModel">Type du ViewModel associé.</typeparam>
    public abstract class BasePage<TViewModel> : BasePage where TViewModel : BaseViewModel
    {
        protected TViewModel ViewModel => (TViewModel)BindingContext;

        protected BasePage(TViewModel viewModel) : base()
        {
            SetViewModel(viewModel);
        }
    }
}
