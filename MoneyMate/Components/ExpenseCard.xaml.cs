using System.Windows.Input;

namespace MoneyMate.Components;

/// <summary>
/// Carte résumant une dépense : catégorie, montant, note et date.
/// Peut être tapée pour naviguer vers le détail.
/// </summary>
public partial class ExpenseCard : ContentView
{
    /// <summary>
    /// Date de l'opération.
    /// </summary>
    public static readonly BindableProperty ExpenseDateProperty =
        BindableProperty.Create(nameof(ExpenseDate), typeof(DateTime), typeof(ExpenseCard), DateTime.Now);

    /// <summary>
    /// Montant de la dépense.
    /// </summary>
    public static readonly BindableProperty AmountProperty =
        BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(ExpenseCard), 0m);

    /// <summary>
    /// Code devise (EUR, USD, etc.).
    /// </summary>
    public static readonly BindableProperty DeviseProperty =
        BindableProperty.Create(nameof(Devise), typeof(string), typeof(ExpenseCard), "EUR");

    /// <summary>
    /// Nom de la catégorie.
    /// </summary>
    public static readonly BindableProperty CategoryNameProperty =
        BindableProperty.Create(nameof(CategoryName), typeof(string), typeof(ExpenseCard), string.Empty);

    /// <summary>
    /// Libellé principal de la dépense.
    /// </summary>
    public static readonly BindableProperty DisplayNameProperty =
        BindableProperty.Create(nameof(DisplayName), typeof(string), typeof(ExpenseCard), string.Empty);

    /// <summary>
    /// Icône de la catégorie.
    /// </summary>
    public static readonly BindableProperty CategoryIconProperty =
        BindableProperty.Create(nameof(CategoryIcon), typeof(string), typeof(ExpenseCard), "💰");

    /// <summary>
    /// Couleur de la catégorie.
    /// </summary>
    public static readonly BindableProperty CategoryColorProperty =
        BindableProperty.Create(nameof(CategoryColor), typeof(Color), typeof(ExpenseCard), Color.FromArgb("#6B7A8F"));

    /// <summary>
    /// Note ou description de la dépense.
    /// </summary>
    public static readonly BindableProperty NoteProperty =
        BindableProperty.Create(nameof(Note), typeof(string), typeof(ExpenseCard), string.Empty);

    /// <summary>
    /// Commande exécutée au tap sur la carte.
    /// </summary>
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(ExpenseCard), null);

    /// <summary>
    /// Paramètre transmis à la commande de tap.
    /// </summary>
    public static readonly BindableProperty TapCommandParameterProperty =
        BindableProperty.Create(nameof(TapCommandParameter), typeof(object), typeof(ExpenseCard), null);

    /// <summary>
    /// Date de l'opération.
    /// </summary>
    public DateTime ExpenseDate
    {
        get => (DateTime)GetValue(ExpenseDateProperty);
        set => SetValue(ExpenseDateProperty, value);
    }

    /// <summary>
    /// Montant de la dépense.
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
    /// Nom de la catégorie.
    /// </summary>
    public string CategoryName
    {
        get => (string)GetValue(CategoryNameProperty);
        set => SetValue(CategoryNameProperty, value);
    }

    /// <summary>
    /// Libellé principal de la dépense.
    /// </summary>
    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    /// <summary>
    /// Icône de la catégorie.
    /// </summary>
    public string CategoryIcon
    {
        get => (string)GetValue(CategoryIconProperty);
        set => SetValue(CategoryIconProperty, value);
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
    /// Note ou description de la dépense.
    /// </summary>
    public string Note
    {
        get => (string)GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }

    /// <summary>
    /// Commande exécutée au tap sur la carte.
    /// </summary>
    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    /// <summary>
    /// Paramètre transmis à la commande de tap.
    /// </summary>
    public object? TapCommandParameter
    {
        get => GetValue(TapCommandParameterProperty);
        set => SetValue(TapCommandParameterProperty, value);
    }

    public ExpenseCard()
    {
        InitializeComponent();
    }
}
