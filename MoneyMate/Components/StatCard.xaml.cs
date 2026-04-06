namespace MoneyMate.Components;

/// <summary>
/// Carte de statistique pour le dashboard.
/// Affiche une icône, un titre et une valeur sur un fond coloré.
/// </summary>
public partial class StatCard : ContentView
{
    /// <summary>
    /// Titre de la statistique (ex: "Dépenses du mois").
    /// </summary>
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(StatCard), string.Empty);

    /// <summary>
    /// Valeur affichée (ex: "1 250,00 €").
    /// </summary>
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(StatCard), string.Empty);

    /// <summary>
    /// Icône affichée (emoji ou unicode).
    /// </summary>
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(StatCard), "📊");

    /// <summary>
    /// Couleur de fond de la carte.
    /// </summary>
    public static readonly BindableProperty CardColorProperty =
        BindableProperty.Create(nameof(CardColor), typeof(Color), typeof(StatCard), Color.FromArgb("#6793AE"));

    /// <summary>
    /// Titre de la statistique (ex: "Dépenses du mois").
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Valeur affichée (ex: "1 250,00 €").
    /// </summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Icône affichée (emoji ou unicode).
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Couleur de fond de la carte.
    /// </summary>
    public Color CardColor
    {
        get => (Color)GetValue(CardColorProperty);
        set => SetValue(CardColorProperty, value);
    }

    public StatCard()
    {
        InitializeComponent();
    }
}
