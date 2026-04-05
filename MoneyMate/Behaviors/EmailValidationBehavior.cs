using MoneyMate.Helpers;

namespace MoneyMate.Behaviors
{
    /// <summary>
    /// Valide le format email en temps réel et colore le Border parent.
    /// Remplace OnEmailTextChanged dans LoginPage et RegisterPage.
    /// </summary>
    public class EmailValidationBehavior : Behavior<Entry>
    {
        private static readonly Color ColorOk   = Color.FromArgb("#6CC57C");
        private static readonly Color ColorFail = Color.FromArgb("#E57373");

        /// <summary>
        /// Border à colorier (lié depuis le XAML).
        /// </summary>
        public static readonly BindableProperty BorderProperty =
            BindableProperty.Create(nameof(Border), typeof(Border), typeof(EmailValidationBehavior));

        public Border? Border
        {
            get => (Border?)GetValue(BorderProperty);
            set => SetValue(BorderProperty, value);
        }

        /// <summary>
        /// Label d'erreur à afficher/masquer.
        /// </summary>
        public static readonly BindableProperty ErrorLabelProperty =
            BindableProperty.Create(nameof(ErrorLabel), typeof(Label), typeof(EmailValidationBehavior));

        public Label? ErrorLabel
        {
            get => (Label?)GetValue(ErrorLabelProperty);
            set => SetValue(ErrorLabelProperty, value);
        }

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
            var text = e.NewTextValue ?? string.Empty;

            if (text.Length == 0)
            {
                if (Border != null) Border.Stroke = Colors.Transparent;
                if (ErrorLabel != null) ErrorLabel.IsVisible = false;
                return;
            }

            bool isValid = ValidationHelper.IsValidEmail(text);

            if (Border != null)
                Border.Stroke = isValid ? ColorOk : ColorFail;

            if (ErrorLabel != null)
                ErrorLabel.IsVisible = !isValid;
        }
    }
}
