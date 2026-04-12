using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Alerts;
using MoneyMate.ViewModels.Auth;
using MoneyMate.ViewModels.Budgets;
using MoneyMate.ViewModels.Categories;
using MoneyMate.ViewModels.Dashboard;
using MoneyMate.ViewModels.Expenses;
using MoneyMate.ViewModels.FixedCharges;
using MoneyMate.ViewModels.Profile;
using MoneyMate.Views.Alerts;
using MoneyMate.Views.Auth;
using MoneyMate.Views.Budgets;
using MoneyMate.Views.Categories;
using MoneyMate.Views.Dashboard;
using MoneyMate.Views.Expenses;
using MoneyMate.Views.FixedCharges;
using MoneyMate.Views.Profile;

namespace MoneyMate
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSans");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                    fonts.AddFont("Lora-VariableFont_wght.ttf", "Lora");
                    fonts.AddFont("Lora-VariableFont_wght.ttf", "LoraBold");
                    fonts.AddFont("FunnelDisplay-VariableFont_wght.ttf", "FunnelDisplay");
                });

            // ── Services ──────────────────────────────────────────────────
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<ISessionManager, SessionManager>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSingleton<ICategoryService, CategoryService>();
            builder.Services.AddSingleton<IExpenseService, ExpenseService>();
            builder.Services.AddSingleton<IBudgetService, BudgetService>();
            builder.Services.AddSingleton<IFixedChargeService, FixedChargeService>();
            builder.Services.AddSingleton<IAlertThresholdService, AlertThresholdService>();
            builder.Services.AddSingleton<IDashboardService, DashboardService>();
            builder.Services.AddSingleton<AppShell>();

            // ── ViewModels ────────────────────────────────────────────────
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<CategoriesViewModel>();
            builder.Services.AddTransient<ExpensesListViewModel>();
            builder.Services.AddTransient<ExpenseFormViewModel>();
            builder.Services.AddTransient<QuickAddExpenseViewModel>();
            builder.Services.AddTransient<ExpenseDetailsViewModel>();
            builder.Services.AddTransient<BudgetsOverviewViewModel>();
            builder.Services.AddTransient<BudgetFormViewModel>();
            builder.Services.AddTransient<FixedChargesViewModel>();
            builder.Services.AddTransient<AlertThresholdsViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<ChangePasswordViewModel>();
            builder.Services.AddTransient<DeleteAccountViewModel>();

            // ── Pages ─────────────────────────────────────────────────────
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<CategoriesListPage>();
            builder.Services.AddTransient<ExpensesListPage>();
            builder.Services.AddTransient<AddExpensePage>();
            builder.Services.AddTransient<EditExpensePage>();
            builder.Services.AddTransient<QuickAddExpensePage>();
            builder.Services.AddTransient<ExpenseDetailsPage>();
            builder.Services.AddTransient<BudgetsOverviewPage>();
            builder.Services.AddTransient<AddBudgetPage>();
            builder.Services.AddTransient<EditBudgetPage>();
            builder.Services.AddTransient<FixedChargesPage>();
            builder.Services.AddTransient<AlertThresholdPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ChangePasswordPage>();
            builder.Services.AddTransient<DeleteAccountPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
