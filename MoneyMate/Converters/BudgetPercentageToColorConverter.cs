using System.Globalization;

namespace MoneyMate.Converters
{
    /// <summary>
    /// Convertit un pourcentage de budget consommé en couleur.
    /// Moins de 75% → vert, 75-99% → orange, 100%+ → rouge.
    /// </summary>
    public class BudgetPercentageToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            decimal percent = value switch
            {
                decimal d => d,
                double d  => (decimal)d,
                int i     => (decimal)i,
                _         => 0m
            };

            return percent switch
            {
                < 75m  => Color.FromArgb("#6CC57C"),
                < 100m => Color.FromArgb("#FFB74D"),
                _      => Color.FromArgb("#E57373")
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
