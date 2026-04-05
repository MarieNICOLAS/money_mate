using MoneyMate.Helpers;

namespace MoneyMate.Behaviors
{
    /// <summary>
    /// Valide la longueur minimale du mot de passe en temps réel.
    /// Colore le Border et affiche/masque le label d'erreur.
    /// Remplace OnPasswordTextChanged dans LoginPage.
    /// </summary>
    public class PasswordMinLengthBehavior : Behavior<Entry>
    {
        private const int MIN_LENGTH = 8;

        private static readonly Color ColorOk   = Color.FromArgb("#6CC57C");
        private static readonly Color ColorFail = Color.FromArgb("#E57373");

        /// <summary>
        /// Border à colorier (lié depuis le XAML).
        /// </summary>
        public static readonly BindableProperty BorderProperty =
            BindableProperty.Create(nameof(Border), typeof(Border), typeof(PasswordMinLengthBehavior));

        public Border? Border
        {
            get => (Border?)GetValue(BorderProperty);
            set => SetValue(BorderProperty, value);
        }

        /// <summary>
        /// Label d'erreur à afficher/masquer.
        /// </summary>
        public static readonly BindableProperty ErrorLabelProperty =
            BindableProperty.Create(nameof(ErrorLabel), typeof(Label), typeof(PasswordMinLengthBehavior));

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
            var pwd = e.NewTextValue ?? string.Empty;

            if (pwd.Length == 0)
            {
                if (Border != null) Border.Stroke = Colors.Transparent;
                if (ErrorLabel != null) ErrorLabel.IsVisible = false;
                return;
            }

            bool isValid = pwd.Length >= MIN_LENGTH;

            if (Border != null)
                Border.Stroke = isValid ? ColorOk : ColorFail;

            if (ErrorLabel != null)
                ErrorLabel.IsVisible = !isValid;
        }
    }
}
