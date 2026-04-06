using System.Windows.Input;

namespace MoneyMate.Components;

/// <summary>
/// Vue affichée quand une liste est vide.
/// Propose une icône, un message et un bouton d'action optionnel.
/// </summary>
public partial class EmptyStateView : ContentView
{
    /// <summary>
    /// Icône affichée (emoji ou unicode).
    /// </summary>
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyStateView), "📭");

    /// <summary>
    /// Message descriptif affiché sous l'icône.
    /// </summary>
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyStateView), "Aucun élément à afficher.");

    /// <summary>
    /// Texte du bouton d'action (optionnel).
    /// </summary>
    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyStateView), string.Empty,
            propertyChanged: OnActionPropertyChanged);

    /// <summary>
    /// Commande exécutée au clic sur le bouton d'action.
    /// </summary>
    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateView), null,
            propertyChanged: OnActionPropertyChanged);

    /// <summary>
    /// Indique si le bouton d'action est visible (lecture seule).
    /// </summary>
    public static readonly BindableProperty HasActionProperty =
        BindableProperty.Create(nameof(HasAction), typeof(bool), typeof(EmptyStateView), false);

    /// <summary>
    /// Icône affichée (emoji ou unicode).
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Message descriptif affiché sous l'icône.
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Texte du bouton d'action (optionnel).
    /// </summary>
    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    /// <summary>
    /// Commande exécutée au clic sur le bouton d'action.
    /// </summary>
    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    /// <summary>
    /// Indique si le bouton d'action est visible (lecture seule).
    /// </summary>
    public bool HasAction
    {
        get => (bool)GetValue(HasActionProperty);
        private set => SetValue(HasActionProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }

    private static void OnActionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is EmptyStateView view)
            view.HasAction = !string.IsNullOrWhiteSpace(view.ActionText) && view.ActionCommand != null;
    }
}
