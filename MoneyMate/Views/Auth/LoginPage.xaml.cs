using Microsoft.Maui.Controls;

namespace MoneyMate.Views.Auth
{
    public partial class LoginPage : BasePage
    {
        private static readonly Color ColorOk   = Color.FromArgb("#6CC57C");
        private static readonly Color ColorFail = Color.FromArgb("#E57373");

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

        // ── Validation email ───────────────────────────────────────────────────
        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {
            var email = e.NewTextValue ?? string.Empty;

            if (email.Length == 0)
            {
                EmailBorder.Stroke        = Colors.Transparent;
                EmailErrorLabel.IsVisible = false;
                return;
            }

            bool isValid = IsValidEmail(email);
            EmailBorder.Stroke        = isValid ? ColorOk : ColorFail;
            EmailErrorLabel.IsVisible = !isValid;
        }

        // ── Validation mot de passe ────────────────────────────────────────────
        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            var pwd = e.NewTextValue ?? string.Empty;

            if (pwd.Length == 0)
            {
                PasswordBorder.Stroke        = Colors.Transparent;
                PasswordErrorLabel.IsVisible = false;
                return;
            }

            bool isValid = pwd.Length >= 12;
            PasswordBorder.Stroke        = isValid ? ColorOk : ColorFail;
            PasswordErrorLabel.IsVisible = !isValid;
        }

        // ── Visibilite mot de passe ────────────────────────────────────────────
        private void OnTogglePasswordVisibility(object sender, TappedEventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            EyeIcon.Text = PasswordEntry.IsPassword
                ? Application.Current!.Resources["IconEyeOpen"]  as string
                : Application.Current!.Resources["IconEyeClosed"] as string;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ── Reinitialisation UI ────────────────────────────────────────────────
        private void ResetValidationUI()
        {
            EmailBorder.Stroke           = Colors.Transparent;
            PasswordBorder.Stroke        = Colors.Transparent;
            EmailErrorLabel.IsVisible    = false;
            PasswordErrorLabel.IsVisible = false;
            LoginErrorLabel.IsVisible    = false;
        }

        // ── Navigation ────────────────────────────────────────────────────────
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
