namespace MoneyMate.Configuration
{
    public static class AppRoutes
    {
        public const string Main = "//MainPage";
        public const string Login = "//LoginPage";
        public const string Register = "//RegisterPage";
        public const string Dashboard = "//DashboardPage";

        public const string Profile = "ProfilePage";
        public const string Calendar = "CalendarPage";
        public const string QuickAddExpense = "QuickAddExpensePage";
        public const string BudgetsOverview = "BudgetsOverviewPage";
        public const string ExpensesList = "ExpensesListPage";
        public const string AddExpense = "AddExpensePage";
        public const string EditExpense = "EditExpensePage";
        public const string ExpenseDetails = "ExpenseDetailsPage";
        public const string ChangePassword = "ChangePasswordPage";
        public const string DeleteAccount = "DeleteAccountPage";
        public const string CategoriesList = "CategoriesListPage";
        public const string AddCategory = "AddCategoryPage";
        public const string EditCategory = "EditCategoryPage";
        public const string FixedCharges = "FixedChargesPage";
        public const string AddFixedCharge = "AddFixedChargePage";
        public const string EditFixedCharge = "EditFixedChargePage";
        public const string AlertThreshold = "AlertThresholdPage";
        public const string AddBudget = "AddBudgetPage";
        public const string EditBudget = "EditBudgetPage";

        private static readonly HashSet<string> RootRouteNames = new(StringComparer.Ordinal)
        {
            "MainPage",
            "LoginPage",
            "RegisterPage",
            "DashboardPage"
        };

        public static string Normalize(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La route est requise.", nameof(route));

            string trimmedRoute = route.Trim();
            int queryIndex = trimmedRoute.IndexOf('?');
            string routePath = queryIndex >= 0 ? trimmedRoute[..queryIndex] : trimmedRoute;
            string queryString = queryIndex >= 0 ? trimmedRoute[queryIndex..] : string.Empty;

            string routeName = routePath.Trim('/');
            return RootRouteNames.Contains(routeName)
                ? $"//{routeName}{queryString}"
                : $"{routeName}{queryString}";
        }
    }
}
