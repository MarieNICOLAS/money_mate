using Microsoft.Maui.Controls;
using MoneyMate.Configuration;
using MoneyMate.Infrastructure;
using MoneyMate.ViewModels.Auth;

namespace MoneyMate.Views.Auth
{
    public partial class LoginPage : BasePage
    {
        private LoginViewModel ViewModel => (LoginViewModel)BindingContext;

        public LoginPage()
            : this(ServiceResolver.GetRequiredService<LoginViewModel>())
        {
        }

        public LoginPage(LoginViewModel viewModel)
        {
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
            await Shell.Current.GoToAsync(AppRoutes.Register);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(AppRoutes.Main);
        }
    }
}
