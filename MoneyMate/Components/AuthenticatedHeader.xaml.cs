using System.Windows.Input;

namespace MoneyMate.Components;

/// <summary>
/// Header affich� sur les pages authentifi�es.
/// Affiche le nom de l'utilisateur connect� et un bouton de d�connexion.
/// </summary>
public partial class AuthenticatedHeader : ContentView
{
    /// <summary>Nom de l'utilisateur connect�.</summary>
    public static readonly BindableProperty UserNameProperty =
        BindableProperty.Create(nameof(UserName), typeof(string), typeof(AuthenticatedHeader), string.Empty);

    /// <summary>Commande ex�cut�e lors du clic sur D�connexion.</summary>
    public static readonly BindableProperty LogoutCommandProperty =
        BindableProperty.Create(nameof(LogoutCommand), typeof(ICommand), typeof(AuthenticatedHeader), null);

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    public ICommand? LogoutCommand
    {
        get => (ICommand?)GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }

    public AuthenticatedHeader()
    {
        InitializeComponent();
    }
}