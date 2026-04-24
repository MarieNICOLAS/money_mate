using Microsoft.Maui.Controls;
using MoneyMate.Configuration;
using MoneyMate.Infrastructure;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Auth;

namespace MoneyMate.Views.Auth
{
    public partial class LoginPage : BasePage
    {
        private readonly INavigationService _navigationService;
        private LoginViewModel ViewModel => (LoginViewModel)BindingContext;

        public LoginPage()
            : this(
                ServiceResolver.GetRequiredService<LoginViewModel>(),
                ServiceResolver.GetRequiredService<INavigationService>())
        {
        }

        public LoginPage(LoginViewModel viewModel, INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.LoadRememberMe();

            if (!ViewModel.RememberMe)
            {
                ViewModel.Email    = string.Empty;
                ViewModel.Password = string.Empty;
                PasswordEntry.IsPassword = true;
                EyeIcon.Text = Application.Current!.Resources["IconEyeOpen"] as string;
            }
        }

        private void OnTogglePasswordVisibility(object sender, TappedEventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            EyeIcon.Text = PasswordEntry.IsPassword
                ? Application.Current!.Resources["IconEyeOpen"]  as string
                : Application.Current!.Resources["IconEyeClosed"] as string;
        }

        private async void OnGoToRegisterTapped(object sender, TappedEventArgs e)
        {
            await _navigationService.NavigateToAsync(AppRoutes.Register);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await _navigationService.NavigateToAsync(AppRoutes.Main);
        }
    }
}
