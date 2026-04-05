using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Convertit un montant décimal en couleur :
    /// positif → vert, négatif → rouge.
    /// </summary>
    public class AmountToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is decimal amount)
                return amount >= 0
                    ? Color.FromArgb("#6CC57C")
                    : Color.FromArgb("#E57373");

            return Color.FromArgb("#6CC57C");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
