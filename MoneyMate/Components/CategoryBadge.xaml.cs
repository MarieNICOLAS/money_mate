namespace MoneyMate.Components;

/// <summary>
/// Pastille colorée affichant l'icône et le nom d'une catégorie.
/// </summary>
public partial class CategoryBadge : ContentView
{
    /// <summary>
    /// Nom de la catégorie affiché dans le badge.
    /// </summary>
    public static readonly BindableProperty CategoryNameProperty =
        BindableProperty.Create(nameof(CategoryName), typeof(string), typeof(CategoryBadge), string.Empty);

    /// <summary>
    /// Icône (emoji ou unicode) de la catégorie.
    /// </summary>
    public static readonly BindableProperty CategoryIconProperty =
        BindableProperty.Create(nameof(CategoryIcon), typeof(string), typeof(CategoryBadge), "💰");

    /// <summary>
    /// Couleur de fond du badge.
    /// </summary>
    public static readonly BindableProperty CategoryColorProperty =
        BindableProperty.Create(nameof(CategoryColor), typeof(Color), typeof(CategoryBadge), Color.FromArgb("#6B7A8F"));

    /// <summary>
    /// Nom de la catégorie affiché dans le badge.
    /// </summary>
    public string CategoryName
    {
        get => (string)GetValue(CategoryNameProperty);
        set => SetValue(CategoryNameProperty, value);
    }

    /// <summary>
    /// Icône (emoji ou unicode) de la catégorie.
    /// </summary>
    public string CategoryIcon
    {
        get => (string)GetValue(CategoryIconProperty);
        set => SetValue(CategoryIconProperty, value);
    }

    /// <summary>
    /// Couleur de fond du badge.
    /// </summary>
    public Color CategoryColor
    {
        get => (Color)GetValue(CategoryColorProperty);
        set => SetValue(CategoryColorProperty, value);
    }

    public CategoryBadge()
    {
        InitializeComponent();
    }
}
