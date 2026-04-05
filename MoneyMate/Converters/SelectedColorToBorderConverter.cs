using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Retourne une bordure noire si la couleur est sélectionnée, transparente sinon.
    /// Utilisé dans la vue de sélection de couleur de catégorie.
    /// </summary>
    public class SelectedColorToBorderConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            var colorHex = value as string;
            var selectedColor = parameter as string;

            if (colorHex == null || selectedColor == null)
                return Colors.Transparent;

            return colorHex == selectedColor
                ? Colors.Black
                : Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
