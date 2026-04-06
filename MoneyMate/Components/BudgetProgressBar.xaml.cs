namespace MoneyMate.Components;

/// <summary>
/// Barre de progression indiquant la consommation d'un budget par catégorie.
/// Change de couleur selon le pourcentage (vert → orange → rouge).
/// </summary>
public partial class BudgetProgressBar : ContentView
{
    private const double PROGRESS_BAR_MAX_WIDTH = 1.0;

    /// <summary>
    /// Nom de la catégorie du budget.
    /// </summary>
    public static readonly BindableProperty CategoryNameProperty =
        BindableProperty.Create(nameof(CategoryName), typeof(string), typeof(BudgetProgressBar), string.Empty);

    /// <summary>
    /// Couleur de la catégorie.
    /// </summary>
    public static readonly BindableProperty CategoryColorProperty =
        BindableProperty.Create(nameof(CategoryColor), typeof(Color), typeof(BudgetProgressBar), Color.FromArgb("#6B7A8F"));

    /// <summary>
    /// Montant dépensé sur la période.
    /// </summary>
    public static readonly BindableProperty SpentAmountProperty =
        BindableProperty.Create(nameof(SpentAmount), typeof(decimal), typeof(BudgetProgressBar), 0m,
            propertyChanged: OnAmountChanged);

    /// <summary>
    /// Montant total du budget.
    /// </summary>
    public static readonly BindableProperty BudgetAmountProperty =
        BindableProperty.Create(nameof(BudgetAmount), typeof(decimal), typeof(BudgetProgressBar), 0m,
            propertyChanged: OnAmountChanged);

    /// <summary>
    /// Code devise.
    /// </summary>
    public static readonly BindableProperty DeviseProperty =
        BindableProperty.Create(nameof(Devise), typeof(string), typeof(BudgetProgressBar), "EUR");

    /// <summary>
    /// Texte du pourcentage (lecture seule, ex: "75 %").
    /// </summary>
    public static readonly BindableProperty PercentageTextProperty =
        BindableProperty.Create(nameof(PercentageText), typeof(string), typeof(BudgetProgressBar), "0 %");

    /// <summary>
    /// Couleur de la progression (lecture seule).
    /// </summary>
    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(BudgetProgressBar), Color.FromArgb("#6CC57C"));

    /// <summary>
    /// Nom de la catégorie du budget.
    /// </summary>
    public string CategoryName
    {
        get => (string)GetValue(CategoryNameProperty);
        set => SetValue(CategoryNameProperty, value);
    }

    /// <summary>
    /// Couleur de la catégorie.
    /// </summary>
    public Color CategoryColor
    {
        get => (Color)GetValue(CategoryColorProperty);
        set => SetValue(CategoryColorProperty, value);
    }

    /// <summary>
    /// Montant dépensé sur la période.
    /// </summary>
    public decimal SpentAmount
    {
        get => (decimal)GetValue(SpentAmountProperty);
        set => SetValue(SpentAmountProperty, value);
    }

    /// <summary>
    /// Montant total du budget.
    /// </summary>
    public decimal BudgetAmount
    {
        get => (decimal)GetValue(BudgetAmountProperty);
        set => SetValue(BudgetAmountProperty, value);
    }

    /// <summary>
    /// Code devise.
    /// </summary>
    public string Devise
    {
        get => (string)GetValue(DeviseProperty);
        set => SetValue(DeviseProperty, value);
    }

    /// <summary>
    /// Texte du pourcentage (lecture seule, ex: "75 %").
    /// </summary>
    public string PercentageText
    {
        get => (string)GetValue(PercentageTextProperty);
        private set => SetValue(PercentageTextProperty, value);
    }

    /// <summary>
    /// Couleur de la progression (lecture seule).
    /// </summary>
    public Color ProgressColor
    {
        get => (Color)GetValue(ProgressColorProperty);
        private set => SetValue(ProgressColorProperty, value);
    }

    public BudgetProgressBar()
    {
        InitializeComponent();
        UpdateProgress();
    }

    private static void OnAmountChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BudgetProgressBar bar)
            bar.UpdateProgress();
    }

    /// <summary>
    /// Recalcule le pourcentage, la couleur et la largeur de la barre.
    /// </summary>
    private void UpdateProgress()
    {
        decimal percentage = BudgetAmount > 0
            ? Math.Min(100, (SpentAmount / BudgetAmount) * 100)
            : 0;

        PercentageText = $"{percentage:F0} %";
        ProgressColor = GetColorForPercentage(percentage);

        // Largeur proportionnelle de la barre de progression
        double ratio = (double)Math.Min(percentage / 100, (decimal)PROGRESS_BAR_MAX_WIDTH);
        ProgressFill.WidthRequest = -1; // Reset
        ProgressFill.SetBinding(WidthRequestProperty, new Binding
        {
            Source = ProgressFill.Parent,
            Path = "Width",
            Converter = new FuncMultiplyConverter(ratio)
        });
    }

    /// <summary>
    /// Détermine la couleur selon le pourcentage consommé.
    /// Vert (0-60), Orange (60-85), Rouge (85+).
    /// </summary>
    private static Color GetColorForPercentage(decimal percentage)
    {
        if (percentage >= 85) return Color.FromArgb("#E57373"); // Error / Rouge
        if (percentage >= 60) return Color.FromArgb("#FFB74D"); // Orange
        return Color.FromArgb("#6CC57C"); // Success / Vert
    }

    /// <summary>
    /// Convertisseur interne pour calculer la largeur proportionnelle.
    /// </summary>
    private sealed class FuncMultiplyConverter : IValueConverter
    {
        private readonly double _factor;

        public FuncMultiplyConverter(double factor)
        {
            _factor = factor;
        }

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double width && width > 0)
                return width * _factor;
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
