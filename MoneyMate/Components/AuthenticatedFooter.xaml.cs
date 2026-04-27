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

    public static readonly BindableProperty GoCalendarCommandProperty =
        BindableProperty.Create(nameof(GoCalendarCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoAddExpenseCommandProperty =
        BindableProperty.Create(nameof(GoAddExpenseCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoBudgetCommandProperty =
        BindableProperty.Create(nameof(GoBudgetCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

    public static readonly BindableProperty GoStatsCommandProperty =
        BindableProperty.Create(nameof(GoStatsCommand), typeof(ICommand), typeof(AuthenticatedFooter), null);

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

    public ICommand? GoCalendarCommand
    {
        get => (ICommand?)GetValue(GoCalendarCommandProperty);
        set => SetValue(GoCalendarCommandProperty, value);
    }

    public ICommand? GoAddExpenseCommand
    {
        get => (ICommand?)GetValue(GoAddExpenseCommandProperty);
        set => SetValue(GoAddExpenseCommandProperty, value);
    }

    public ICommand? GoQuickAddExpenseCommand
    {
        get => GoAddExpenseCommand;
        set => GoAddExpenseCommand = value;
    }

    public ICommand? GoBudgetCommand
    {
        get => (ICommand?)GetValue(GoBudgetCommandProperty);
        set => SetValue(GoBudgetCommandProperty, value);
    }

    public ICommand? GoStatsCommand
    {
        get => (ICommand?)GetValue(GoStatsCommandProperty);
        set => SetValue(GoStatsCommandProperty, value);
    }

    public AuthenticatedFooter()
    {
        InitializeComponent();
    }
}
