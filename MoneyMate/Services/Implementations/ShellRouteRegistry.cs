using MoneyMate.Configuration;
using MoneyMate.Views.Alerts;
using MoneyMate.Views.Auth;
using MoneyMate.Views.Budgets;
using MoneyMate.Views.Calendar;
using MoneyMate.Views.Categories;
using MoneyMate.Views.Dashboard;
using MoneyMate.Views.Errors;
using MoneyMate.Views.Expenses;
using MoneyMate.Views.FixedCharges;
using MoneyMate.Views.Profile;

namespace MoneyMate.Services.Implementations;

public static class ShellRouteRegistry
{
    private static bool _isRegistered;

    public static void RegisterRoutes()
    {
        if (_isRegistered)
            return;

        _isRegistered = true;

        Register<LoginPage>();
        Register<RegisterPage>();
        Register<DashboardPage>();
        Register<ProfilePage>();
        Register<ChangePasswordPage>();
        Register<DeleteAccountPage>();
        Register<ExpensesListPage>();
        Register<AddExpensePage>();
        Register<EditExpensePage>();
        Register<ExpenseDetailsPage>();
        Register<QuickAddExpensePage>();
        Register<CategoriesListPage>();
        Register<AddCategoryPage>();
        Register<EditCategoryPage>();
        Register<BudgetsOverviewPage>();
        Register<AddBudgetPage>();
        Register<EditBudgetPage>();
        Register<FixedChargesPage>();
        Register<AlertThresholdPage>();
        Register<CalendarPage>();
        Register<ErrorPage>();
        Register<NotFoundPage>();
        Register<NoConnectionPage>();
    }

    public static string Normalize(string route)
        => AppRoutes.Normalize(route);

    private static void Register<TPage>() where TPage : Page
        => Routing.RegisterRoute(typeof(TPage).Name, typeof(TPage));
}
