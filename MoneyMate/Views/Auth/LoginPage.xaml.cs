using Microsoft.Maui.Controls;

namespace MoneyMate.Views.Auth
{
    public partial class LoginPage : BasePage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!RememberMeCheckBox.IsChecked)
            {
                EmailEntry.Text          = string.Empty;
                PasswordEntry.Text       = string.Empty;
                PasswordEntry.IsPassword = true;
                EyeIcon.Text = Application.Current!.Resources["IconEyeOpen"] as string;
            }

            ResetValidationUI();
        }

        private void OnTogglePasswordVisibility(object sender, TappedEventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            EyeIcon.Text = PasswordEntry.IsPassword
                ? Application.Current!.Resources["IconEyeOpen"]  as string
                : Application.Current!.Resources["IconEyeClosed"] as string;
        }

        private void ResetValidationUI()
        {
            EmailBorder.Stroke           = Colors.Transparent;
            PasswordBorder.Stroke        = Colors.Transparent;
            EmailErrorLabel.IsVisible    = false;
            PasswordErrorLabel.IsVisible = false;
            LoginErrorLabel.IsVisible    = false;
        }

        private async void OnGoToRegisterTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
