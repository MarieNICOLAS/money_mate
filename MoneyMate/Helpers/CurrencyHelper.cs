namespace MoneyMate.Helpers
{
    /// <summary>
    /// Utilitaires de formatage monétaire.
    /// </summary>
    public static class CurrencyHelper
    {
        private static readonly Dictionary<string, string> _symbols = new()
        {
            { "EUR", "€" },
            { "USD", "$" },
            { "GBP", "£" },
            { "CHF", "CHF" },
            { "CAD", "CA$" }
        };

        /// <summary>
        /// Retourne le symbole d'une devise (ex: EUR → €).
        /// </summary>
        public static string GetSymbol(string devise)
            => _symbols.TryGetValue(devise?.ToUpper() ?? "EUR", out var symbol)
                ? symbol
                : devise ?? "€";

        /// <summary>
        /// Formate un montant avec le symbole de la devise.
        /// Ex: 1250.5, "EUR" → "1 250,50 €"
        /// </summary>
        public static string Format(decimal amount, string devise = "EUR")
        {
            string symbol = GetSymbol(devise);
            return $"{amount:N2} {symbol}";
        }

        /// <summary>
        /// Formate un montant avec signe + ou - selon le type.
        /// </summary>
        public static string FormatSigned(decimal amount, string devise = "EUR")
        {
            string symbol = GetSymbol(devise);
            string sign = amount >= 0 ? "+" : string.Empty;
            return $"{sign}{amount:N2} {symbol}";
        }

        /// <summary>
        /// Retourne la liste des devises disponibles.
        /// </summary>
        public static IReadOnlyList<string> AvailableDevises
            => _symbols.Keys.ToList().AsReadOnly();
    }
}
