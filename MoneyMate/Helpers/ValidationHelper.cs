namespace MoneyMate.Helpers
{
    /// <summary>
    /// Utilitaires de validation purs, sans dépendances externes.
    /// </summary>
    public static class ValidationHelper
    {
        private const int MIN_PASSWORD_LENGTH = 12;
        private const int MIN_USERNAME_LENGTH = 3;

        /// <summary>
        /// Valide le format d'une adresse email.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email.Trim());
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valide la longueur minimale d'un pseudo.
        /// </summary>
        public static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username)
                && username.Trim().Length >= MIN_USERNAME_LENGTH;
        }

        /// <summary>
        /// Valide la force d'un mot de passe.
        /// Retourne un score de 0 (vide) à 4 (très fort).
        /// </summary>
        public static int GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            if (password.Length >= MIN_PASSWORD_LENGTH)               score++;
            if (password.Any(char.IsUpper))                           score++;
            if (password.Any(char.IsDigit))                           score++;
            if (password.Any(c => !char.IsLetterOrDigit(c)))          score++;

            return score;
        }

        /// <summary>
        /// Retourne le libellé de force selon le score.
        /// </summary>
        public static string GetPasswordStrengthLabel(int score) => score switch
        {
            0 => string.Empty,
            1 => "Très faible",
            2 => "Faible",
            3 => "Moyen",
            4 => "Fort",
            _ => string.Empty
        };

        /// <summary>
        /// Retourne la couleur associée au score de force.
        /// </summary>
        public static Color GetPasswordStrengthColor(int score) => score switch
        {
            1 => Color.FromArgb("#E57373"),
            2 => Color.FromArgb("#FFB74D"),
            3 => Color.FromArgb("#FFF176"),
            4 => Color.FromArgb("#6CC57C"),
            _ => Color.FromArgb("#EEEEEE")
        };

        /// <summary>
        /// Valide qu'un montant est positif et non nul.
        /// </summary>
        public static bool IsValidAmount(decimal amount)
            => amount > 0;

        /// <summary>
        /// Valide qu'une date n'est pas dans le futur.
        /// </summary>
        public static bool IsValidDate(DateTime date)
            => date <= DateTime.Now;
    }
}
