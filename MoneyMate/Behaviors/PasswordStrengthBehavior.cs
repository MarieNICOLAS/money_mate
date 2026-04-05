using MoneyMate.Helpers;

namespace MoneyMate.Behaviors
{
    /// <summary>
    /// Évalue la force du mot de passe en temps réel.
    /// Gère les barres de force, le label et les critères.
    /// Remplace OnPasswordTextChanged dans RegisterPage.
    /// </summary>
    public class PasswordStrengthBehavior : Behavior<Entry>
    {
        private static readonly Color ColorGrey = Color.FromArgb("#EEEEEE");

        public static readonly BindableProperty BorderProperty =
            BindableProperty.Create(nameof(Border), typeof(Border), typeof(PasswordStrengthBehavior));
        public Border? Border
        {
            get => (Border?)GetValue(BorderProperty);
            set => SetValue(BorderProperty, value);
        }

        public static readonly BindableProperty StrengthBarGridProperty =
            BindableProperty.Create(nameof(StrengthBarGrid), typeof(Grid), typeof(PasswordStrengthBehavior));
        public Grid? StrengthBarGrid
        {
            get => (Grid?)GetValue(StrengthBarGridProperty);
            set => SetValue(StrengthBarGridProperty, value);
        }

        public static readonly BindableProperty StrengthLabelProperty =
            BindableProperty.Create(nameof(StrengthLabel), typeof(Label), typeof(PasswordStrengthBehavior));
        public Label? StrengthLabel
        {
            get => (Label?)GetValue(StrengthLabelProperty);
            set => SetValue(StrengthLabelProperty, value);
        }

        public static readonly BindableProperty CriteriaPanelProperty =
            BindableProperty.Create(nameof(CriteriaPanel), typeof(VerticalStackLayout), typeof(PasswordStrengthBehavior));
        public VerticalStackLayout? CriteriaPanel
        {
            get => (VerticalStackLayout?)GetValue(CriteriaPanelProperty);
            set => SetValue(CriteriaPanelProperty, value);
        }

        public static readonly BindableProperty Bar1Property =
            BindableProperty.Create(nameof(Bar1), typeof(BoxView), typeof(PasswordStrengthBehavior));
        public BoxView? Bar1 { get => (BoxView?)GetValue(Bar1Property); set => SetValue(Bar1Property, value); }

        public static readonly BindableProperty Bar2Property =
            BindableProperty.Create(nameof(Bar2), typeof(BoxView), typeof(PasswordStrengthBehavior));
        public BoxView? Bar2 { get => (BoxView?)GetValue(Bar2Property); set => SetValue(Bar2Property, value); }

        public static readonly BindableProperty Bar3Property =
            BindableProperty.Create(nameof(Bar3), typeof(BoxView), typeof(PasswordStrengthBehavior));
        public BoxView? Bar3 { get => (BoxView?)GetValue(Bar3Property); set => SetValue(Bar3Property, value); }

        public static readonly BindableProperty Bar4Property =
            BindableProperty.Create(nameof(Bar4), typeof(BoxView), typeof(PasswordStrengthBehavior));
        public BoxView? Bar4 { get => (BoxView?)GetValue(Bar4Property); set => SetValue(Bar4Property, value); }

        public static readonly BindableProperty CritLengthProperty =
            BindableProperty.Create(nameof(CritLength), typeof(Label), typeof(PasswordStrengthBehavior));
        public Label? CritLength { get => (Label?)GetValue(CritLengthProperty); set => SetValue(CritLengthProperty, value); }

        public static readonly BindableProperty CritUpperProperty =
            BindableProperty.Create(nameof(CritUpper), typeof(Label), typeof(PasswordStrengthBehavior));
        public Label? CritUpper { get => (Label?)GetValue(CritUpperProperty); set => SetValue(CritUpperProperty, value); }

        public static readonly BindableProperty CritDigitProperty =
            BindableProperty.Create(nameof(CritDigit), typeof(Label), typeof(PasswordStrengthBehavior));
        public Label? CritDigit { get => (Label?)GetValue(CritDigitProperty); set => SetValue(CritDigitProperty, value); }

        public static readonly BindableProperty CritSpecialProperty =
            BindableProperty.Create(nameof(CritSpecial), typeof(Label), typeof(PasswordStrengthBehavior));
        public Label? CritSpecial { get => (Label?)GetValue(CritSpecialProperty); set => SetValue(CritSpecialProperty, value); }

        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnTextChanged;
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnTextChanged;
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var pwd = e.NewTextValue ?? string.Empty;

            if (pwd.Length == 0)
            {
                ResetUI();
                return;
            }

            int score = ValidationHelper.GetPasswordStrength(pwd);
            Color barColor = ValidationHelper.GetPasswordStrengthColor(score);

            if (Border != null)
                Border.Stroke = score >= 4
                    ? Color.FromArgb("#6CC57C")
                    : Color.FromArgb("#E57373");

            if (StrengthBarGrid != null) StrengthBarGrid.IsVisible = true;
            if (StrengthLabel != null)
            {
                StrengthLabel.IsVisible = true;
                StrengthLabel.Text      = ValidationHelper.GetPasswordStrengthLabel(score);
                StrengthLabel.TextColor = barColor;
            }
            if (CriteriaPanel != null) CriteriaPanel.IsVisible = true;

            UpdateBar(Bar1, score >= 1, barColor);
            UpdateBar(Bar2, score >= 2, barColor);
            UpdateBar(Bar3, score >= 3, barColor);
            UpdateBar(Bar4, score >= 4, barColor);

            bool hasLength  = pwd.Length >= 8;
            bool hasUpper   = pwd.Any(char.IsUpper);
            bool hasDigit   = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(c => !char.IsLetterOrDigit(c));

            UpdateCriteria(CritLength,  hasLength,  "8 caractères minimum");
            UpdateCriteria(CritUpper,   hasUpper,   "1 majuscule");
            UpdateCriteria(CritDigit,   hasDigit,   "1 chiffre");
            UpdateCriteria(CritSpecial, hasSpecial, "1 caractère spécial (!@#$...)");
        }

        private void ResetUI()
        {
            if (Border != null) Border.Stroke = Colors.Transparent;
            if (StrengthBarGrid != null) StrengthBarGrid.IsVisible = false;
            if (StrengthLabel != null) StrengthLabel.IsVisible = false;
            if (CriteriaPanel != null) CriteriaPanel.IsVisible = false;

            UpdateBar(Bar1, false, ColorGrey);
            UpdateBar(Bar2, false, ColorGrey);
            UpdateBar(Bar3, false, ColorGrey);
            UpdateBar(Bar4, false, ColorGrey);
        }

        private static void UpdateBar(BoxView? bar, bool active, Color activeColor)
        {
            if (bar != null)
                bar.BackgroundColor = active ? activeColor : ColorGrey;
        }

        private static void UpdateCriteria(Label? label, bool isValid, string text)
        {
            if (label == null) return;
            label.Text      = $"{(isValid ? "✓" : "✗")}  {text}";
            label.TextColor = isValid
                ? Color.FromArgb("#6CC57C")
                : Color.FromArgb("#E57373");
        }
    }
}
