using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Convertit une chaîne hexadécimale (#RRGGBB) en Color MAUI.
    /// Utilisé pour afficher la couleur des catégories.
    /// </summary>
    public class HexToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try { return Color.FromArgb(hex); }
                catch { /* couleur invalide, on retourne le fallback */ }
            }

            return Color.FromArgb("#CCCCCC");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
