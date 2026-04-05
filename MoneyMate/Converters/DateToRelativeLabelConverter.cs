using MoneyMate.Helpers;
using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Convertit une DateTime en libellé relatif lisible.
    /// Utilise DateHelper.ToRelativeLabel().
    /// </summary>
    public class DateToRelativeLabelConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is DateTime date)
                return DateHelper.ToRelativeLabel(date);

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
