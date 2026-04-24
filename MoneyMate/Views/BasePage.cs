using System.Windows.Input;
using MoneyMate.Components;
using MoneyMate.Configuration;
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

        private readonly Header _header = new();
        private readonly Footer _footer = new();
        private readonly AuthenticatedFooter _authenticatedFooter;

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

        /// <summary>
        /// Affiche ou masque le header public.
        /// </summary>
        public static readonly BindableProperty ShowHeaderProperty =
            BindableProperty.Create(
                nameof(ShowHeader),
                typeof(bool),
                typeof(BasePage),
                true,
                propertyChanged: (b, _, n) => ((BasePage)b)._header.IsVisible = (bool)n);

        /// <summary>
        /// Affiche ou masque le footer public.
        /// </summary>
        public static readonly BindableProperty ShowFooterProperty =
            BindableProperty.Create(
                nameof(ShowFooter),
                typeof(bool),
                typeof(BasePage),
                true,
                propertyChanged: (b, _, _) => ((BasePage)b).UpdateFooterVisibility());

        /// <summary>
        /// Affiche la navbar des pages authentifiées à la place du footer public.
        /// </summary>
        public static readonly BindableProperty UseAuthenticatedFooterProperty =
            BindableProperty.Create(
                nameof(UseAuthenticatedFooter),
                typeof(bool),
                typeof(BasePage),
                false,
                propertyChanged: (b, _, _) => ((BasePage)b).UpdateFooterVisibility());

        public View? PageContent
        {
            get => (View?)GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }

        public bool ShowHeader
        {
            get => (bool)GetValue(ShowHeaderProperty);
            set => SetValue(ShowHeaderProperty, value);
        }

        public bool ShowFooter
        {
            get => (bool)GetValue(ShowFooterProperty);
            set => SetValue(ShowFooterProperty, value);
        }

        public bool UseAuthenticatedFooter
        {
            get => (bool)GetValue(UseAuthenticatedFooterProperty);
            set => SetValue(UseAuthenticatedFooterProperty, value);
        }

        protected ICommand GoHomeCommand { get; }

        protected ICommand GoCalendarCommand { get; }

        protected ICommand GoQuickAddExpenseCommand { get; }

        protected ICommand GoBudgetCommand { get; }

        protected ICommand GoProfileCommand { get; }

        private static void OnPageContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BasePage page && newValue is View view)
                page._contentSlot.Content = view;
        }

        protected BasePage()
        {
            GoHomeCommand = new Command(async () => await NavigateToAsync(AppRoutes.Dashboard));
            GoCalendarCommand = new Command(async () => await NavigateToAsync(AppRoutes.Calendar));
            GoQuickAddExpenseCommand = new Command(async () => await NavigateToAsync(AppRoutes.QuickAddExpense));
            GoBudgetCommand = new Command(async () => await NavigateToAsync(AppRoutes.BudgetsOverview));
            GoProfileCommand = new Command(async () => await NavigateToAsync(AppRoutes.Profile));

            _authenticatedFooter = new AuthenticatedFooter
            {
                GoHomeCommand = GoHomeCommand,
                GoCalendarCommand = GoCalendarCommand,
                GoQuickAddExpenseCommand = GoQuickAddExpenseCommand,
                GoBudgetCommand = GoBudgetCommand,
                GoProfileCommand = GoProfileCommand,
                IsVisible = false
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            Grid.SetRow(_header, 0);
            Grid.SetRow(_contentSlot, 1);
            Grid.SetRow(_footer, 2);
            Grid.SetRow(_authenticatedFooter, 2);

            grid.Children.Add(_header);
            grid.Children.Add(_contentSlot);
            grid.Children.Add(_footer);
            grid.Children.Add(_authenticatedFooter);

            Content = grid;
            UpdateFooterVisibility();
        }

        /// <summary>
        /// Configure le ViewModel de la page.
        /// </summary>
        protected void SetViewModel(BaseViewModel viewModel)
        {
            BindingContext = viewModel;
        }

        private void UpdateFooterVisibility()
        {
            _authenticatedFooter.IsVisible = UseAuthenticatedFooter;
            _footer.IsVisible = !UseAuthenticatedFooter && ShowFooter;
        }

        private static async Task NavigateToAsync(string route)
        {
            if (Shell.Current == null)
                return;

            await Shell.Current.GoToAsync(route);
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
