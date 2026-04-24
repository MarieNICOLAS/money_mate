using Microsoft.Maui.Controls;
using MoneyMate.Configuration;
using MoneyMate.Infrastructure;
using MoneyMate.ViewModels.Auth;
   
namespace MoneyMate.Views.Auth
{
    public partial class RegisterPage : BasePage
    {
        private RegisterViewModel ViewModel => (RegisterViewModel)BindingContext;

        private static readonly Color ColorOk   = Color.FromArgb("#4CAF50");
        private static readonly Color ColorFail = Color.FromArgb("#F44336");
        private static readonly Color ColorGrey = Color.FromArgb("#9E9E9E");

        public RegisterPage()
            : this(ServiceResolver.GetRequiredService<RegisterViewModel>())
        {
        }

        public RegisterPage(RegisterViewModel viewModel)
        {
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.Username        = string.Empty;
            ViewModel.Email           = string.Empty;
            ViewModel.Password        = string.Empty;
            ViewModel.ConfirmPassword = string.Empty;
            PasswordEntry.IsPassword  = true;
            EyeIcon.Text = Application.Current!.Resources["IconEyeOpen"] as string;
        }

        // ── Validation email ───────────────────────────────────────────────────
        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {
            var email = e.NewTextValue ?? string.Empty;

            if (email.Length == 0)
            {
                EmailBorder.Stroke        = Colors.Transparent;
                EmailErrorLabel.IsVisible = false;
                EmailTakenLabel.IsVisible = false;
                return;
            }

            bool isValid = IsValidEmail(email);
            EmailBorder.Stroke        = isValid ? ColorOk : ColorFail;
            EmailErrorLabel.IsVisible = !isValid;

            // EmailTakenLabel sera gere par le ViewModel lors de l'etape logique
            if (isValid)
                EmailTakenLabel.IsVisible = false;
        }

        // ── Validation mot de passe ────────────────────────────────────────────
        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            var pwd = e.NewTextValue ?? string.Empty;

            if (pwd.Length == 0)
            {
                PasswordBorder.Stroke = Colors.Transparent;
                ResetStrengthUI();
                return;
            }

            bool hasLength  = pwd.Length >= 12;
            bool hasUpper   = pwd.Any(char.IsUpper);
            bool hasDigit   = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(c => !char.IsLetterOrDigit(c));

            int score = (hasLength ? 1 : 0) + (hasUpper ? 1 : 0)
                      + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

            PasswordBorder.Stroke = score >= 4 ? ColorOk : ColorFail;

            StrengthBarGrid.IsVisible = true;
            StrengthLabel.IsVisible   = true;
            CriteriaPanel.IsVisible   = true;

            UpdateCriteria(CritLength,  hasLength,  "12 caracteres minimum");
            UpdateCriteria(CritUpper,   hasUpper,   "1 majuscule");
            UpdateCriteria(CritDigit,   hasDigit,   "1 chiffre");
            UpdateCriteria(CritSpecial, hasSpecial, "1 caractere special (!@#$...)");

            UpdateStrengthBars(score);
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

        private static void UpdateCriteria(Label label, bool isValid, string text)
        {
            label.Text      = $"{(isValid ? "v" : "x")}  {text}";
            label.TextColor = isValid ? ColorOk : ColorFail;
        }

        private void UpdateStrengthBars(int score)
        {
            Color barColor = score switch
            {
                1 => ColorFail,
                2 => Color.FromArgb("#FFB74D"),
                3 => Color.FromArgb("#FFF176"),
                4 => ColorOk,
                _ => ColorGrey
            };

            string label = score switch
            {
                1 => "Tres faible",
                2 => "Faible",
                3 => "Moyen",
                4 => "Fort",
                _ => string.Empty
            };

            Bar1.BackgroundColor = score >= 1 ? barColor : ColorGrey;
            Bar2.BackgroundColor = score >= 2 ? barColor : ColorGrey;
            Bar3.BackgroundColor = score >= 3 ? barColor : ColorGrey;
            Bar4.BackgroundColor = score >= 4 ? barColor : ColorGrey;

            StrengthLabel.Text      = label;
            StrengthLabel.TextColor = barColor;
        }

        private void ResetStrengthUI()
        {
            StrengthBarGrid.IsVisible = false;
            StrengthLabel.IsVisible   = false;
            CriteriaPanel.IsVisible   = false;

            Bar1.BackgroundColor = ColorGrey;
            Bar2.BackgroundColor = ColorGrey;
            Bar3.BackgroundColor = ColorGrey;
            Bar4.BackgroundColor = ColorGrey;
        }

        private void ResetValidationUI()
        {
            UsernameBorder.Stroke        = Colors.Transparent;
            EmailBorder.Stroke           = Colors.Transparent;
            PasswordBorder.Stroke        = Colors.Transparent;
            EmailErrorLabel.IsVisible    = false;
            EmailTakenLabel.IsVisible    = false;
            ResetStrengthUI();
        }

        // ── Navigation ────────────────────────────────────────────────────────
        private async void OnGoToLoginTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(AppRoutes.Login);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(AppRoutes.Main);
        }
    }
}
