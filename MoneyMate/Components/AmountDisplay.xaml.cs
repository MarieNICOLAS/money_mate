using MoneyMate.Helpers;

namespace MoneyMate.Components;

/// <summary>
/// Affiche un montant formaté avec le symbole de la devise.
/// Supporte l'affichage signé (+/-) et la personnalisation visuelle.
/// </summary>
public partial class AmountDisplay : ContentView
{
    /// <summary>
    /// Montant à afficher.
    /// </summary>
    public static readonly BindableProperty AmountProperty =
        BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(AmountDisplay), 0m,
            propertyChanged: OnAmountPropertyChanged);

    /// <summary>
    /// Code devise (EUR, USD, etc.).
    /// </summary>
    public static readonly BindableProperty DeviseProperty =
        BindableProperty.Create(nameof(Devise), typeof(string), typeof(AmountDisplay), "EUR",
            propertyChanged: OnAmountPropertyChanged);

    /// <summary>
    /// Affiche le signe +/- devant le montant.
    /// </summary>
    public static readonly BindableProperty ShowSignProperty =
        BindableProperty.Create(nameof(ShowSign), typeof(bool), typeof(AmountDisplay), false,
            propertyChanged: OnAmountPropertyChanged);

    /// <summary>
    /// Taille de police du montant.
    /// </summary>
    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(AmountDisplay), 18.0);

    /// <summary>
    /// Couleur du texte.
    /// </summary>
    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(AmountDisplay), Color.FromArgb("#222222"));

    /// <summary>
    /// Texte formaté calculé automatiquement (lecture seule).
    /// </summary>
    public static readonly BindableProperty FormattedAmountProperty =
        BindableProperty.Create(nameof(FormattedAmount), typeof(string), typeof(AmountDisplay), "0,00 €");

    /// <summary>
    /// Montant à afficher.
    /// </summary>
    public decimal Amount
    {
        get => (decimal)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }

    /// <summary>
    /// Code devise (EUR, USD, etc.).
    /// </summary>
    public string Devise
    {
        get => (string)GetValue(DeviseProperty);
        set => SetValue(DeviseProperty, value);
    }

    /// <summary>
    /// Affiche le signe +/- devant le montant.
    /// </summary>
    public bool ShowSign
    {
        get => (bool)GetValue(ShowSignProperty);
        set => SetValue(ShowSignProperty, value);
    }

    /// <summary>
    /// Taille de police du montant.
    /// </summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Couleur du texte.
    /// </summary>
    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    /// <summary>
    /// Texte formaté calculé automatiquement (lecture seule).
    /// </summary>
    public string FormattedAmount
    {
        get => (string)GetValue(FormattedAmountProperty);
        private set => SetValue(FormattedAmountProperty, value);
    }

    public AmountDisplay()
    {
        InitializeComponent();
        UpdateFormattedAmount();
    }

    private static void OnAmountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AmountDisplay display)
            display.UpdateFormattedAmount();
    }

    /// <summary>
    /// Recalcule le texte formaté via CurrencyHelper.
    /// </summary>
    private void UpdateFormattedAmount()
    {
        FormattedAmount = ShowSign
            ? CurrencyHelper.FormatSigned(Amount, Devise)
            : CurrencyHelper.Format(Amount, Devise);
    }
}
