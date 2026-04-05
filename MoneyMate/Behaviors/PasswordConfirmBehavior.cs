namespace MoneyMate.Behaviors
{
    /// <summary>
    /// Vérifie que la confirmation de mot de passe correspond au mot de passe original.
    /// </summary>
    public class PasswordConfirmBehavior : Behavior<Entry>
    {
        private static readonly Color ColorOk   = Color.FromArgb("#6CC57C");
        private static readonly Color ColorFail = Color.FromArgb("#E57373");

        /// <summary>
        /// L'Entry du mot de passe original à comparer.
        /// </summary>
        public static readonly BindableProperty PasswordEntryProperty =
            BindableProperty.Create(nameof(PasswordEntry), typeof(Entry), typeof(PasswordConfirmBehavior));
        public Entry? PasswordEntry
        {
            get => (Entry?)GetValue(PasswordEntryProperty);
            set => SetValue(PasswordEntryProperty, value);
        }

        public static readonly BindableProperty BorderProperty =
            BindableProperty.Create(nameof(Border), typeof(Border), typeof(PasswordConfirmBehavior));
        public Border? Border
        {
            get => (Border?)GetValue(BorderProperty);
            set => SetValue(BorderProperty, value);
        }

        public static readonly BindableProperty ErrorLabelProperty =
            BindableProperty.Create(nameof(ErrorLabel), typeof(Label), typeof(PasswordConfirmBehavior));
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
            var confirm = e.NewTextValue ?? string.Empty;

            if (confirm.Length == 0)
            {
                if (Border != null) Border.Stroke = Colors.Transparent;
                if (ErrorLabel != null) ErrorLabel.IsVisible = false;
                return;
            }

            bool isMatch = PasswordEntry != null && confirm == PasswordEntry.Text;

            if (Border != null)
                Border.Stroke = isMatch ? ColorOk : ColorFail;

            if (ErrorLabel != null)
                ErrorLabel.IsVisible = !isMatch;
        }
    }
}
