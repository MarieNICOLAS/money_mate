using System.Windows.Input;

namespace MoneyMate.Components;

/// <summary>
/// Barre de navigation du bas pour les pages authentifi�es.
/// Expose des commandes bindables pour chaque onglet.
/// </summary>
public partial class AuthenticatedFooter : ContentView
{
    public static readonly BindableProperty GoHomeCommandProperty =
        BindableProperty.Create(nameof(GoHomeCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoExpensesCommandProperty =
        BindableProperty.Create(nameof(GoExpensesCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoBudgetCommandProperty =
        BindableProperty.Create(nameof(GoBudgetCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoProfileCommandProperty =
        BindableProperty.Create(nameof(GoProfileCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public ICommand? GoHomeCommand
    {
        get => (ICommand?)GetValue(GoHomeCommandProperty);
        set => SetValue(GoHomeCommandProperty, value);
    }

    public ICommand? GoExpensesCommand
    {
        get => (ICommand?)GetValue(GoExpensesCommandProperty);
        set => SetValue(GoExpensesCommandProperty, value);
    }

    public ICommand? GoBudgetCommand
    {
        get => (ICommand?)GetValue(GoBudgetCommandProperty);
        set => SetValue(GoBudgetCommandProperty, value);
    }

    public ICommand? GoProfileCommand
    {
        get => (ICommand?)GetValue(GoProfileCommandProperty);
        set => SetValue(GoProfileCommandProperty, value);
    }

    public AuthenticatedFooter()
    {
        InitializeComponent();
    }
}