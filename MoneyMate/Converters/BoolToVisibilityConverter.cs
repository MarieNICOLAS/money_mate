using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Convertit un booléen en visibilité.
    /// Passer parameter="Invert" pour inverser le comportement.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            bool boolValue = value is bool b && b;

            if (parameter is string p && p == "Invert")
                boolValue = !boolValue;

            return boolValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => value is bool b && b;
    }
}
