using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MoneyMate.Configuration;
using MoneyMate.Data.Context;
using MoneyMate.Data.Repositories;
using MoneyMate.Infrastructure;
using MoneyMate.Services.Common;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Alerts;
using MoneyMate.ViewModels.Auth;
using MoneyMate.ViewModels.Budgets;
using MoneyMate.ViewModels.Calendar;
using MoneyMate.ViewModels.Categories;
using MoneyMate.ViewModels.Dashboard;
using MoneyMate.ViewModels.Expenses;
using MoneyMate.ViewModels.FixedCharges;
using MoneyMate.ViewModels.Profile;
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

namespace MoneyMate;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSans");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("Lora-VariableFont_wght.ttf", "Lora");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ===============================
        // DB PATH (CRITICAL FIX)
        // ===============================
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "money_mate.db");

        // ===============================
        // DB CONTEXT (LAZY SAFE)
        // ===============================
        builder.Services.AddSingleton<IMoneyMateDbContext>(_ =>
            new MoneyMateDbContext(dbPath));
        builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();

        builder.Services.AddSingleton(typeof(IDataRepository<>), typeof(BaseRepository<>));
        builder.Services.AddSingleton<DatabaseService>();

        // ===============================
        // SERVICES
        // ===============================
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<IExpenseService, ExpenseService>();
        builder.Services.AddSingleton<IBudgetService, BudgetService>();
        builder.Services.AddSingleton<IDashboardService, DashboardService>();
        builder.Services.AddSingleton<IFixedChargeService, FixedChargeService>();
        builder.Services.AddSingleton<IAlertThresholdService, AlertThresholdService>();
        builder.Services.AddSingleton<IAppEventBus, AppEventBus>();

        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<ISessionManager, SessionManager>();
        builder.Services.AddSingleton<IStartupCoordinator, StartupCoordinator>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<ChangePasswordPage>();
        builder.Services.AddTransient<DeleteAccountPage>();
        builder.Services.AddTransient<ExpensesListPage>();
        builder.Services.AddTransient<AddExpensePage>();
        builder.Services.AddTransient<EditExpensePage>();
        builder.Services.AddTransient<ExpenseDetailsPage>();
        builder.Services.AddTransient<QuickAddExpensePage>();
        builder.Services.AddTransient<CategoriesListPage>();
        builder.Services.AddTransient<AddCategoryPage>();
        builder.Services.AddTransient<EditCategoryPage>();
        builder.Services.AddTransient<BudgetsOverviewPage>();
        builder.Services.AddTransient<AddBudgetPage>();
        builder.Services.AddTransient<EditBudgetPage>();
        builder.Services.AddTransient<FixedChargesPage>();
        builder.Services.AddTransient<AddFixedChargePage>();
        builder.Services.AddTransient<EditFixedChargePage>();
        builder.Services.AddTransient<AlertThresholdPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<ErrorPage>();
        builder.Services.AddTransient<NotFoundPage>();
        builder.Services.AddTransient<NoConnectionPage>();

        // ===============================
        // VIEWMODELS (TRANSIENT = PERFORMANCE)
        // ===============================
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<ChangePasswordViewModel>();
        builder.Services.AddTransient<DeleteAccountViewModel>();
        builder.Services.AddTransient<ExpensesListViewModel>();
        builder.Services.AddTransient<ExpenseFormViewModel>();
        builder.Services.AddTransient<ExpenseDetailsViewModel>();
        builder.Services.AddTransient<QuickAddExpenseViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<CategoryFormViewModel>();
        builder.Services.AddTransient<BudgetsOverviewViewModel>();
        builder.Services.AddTransient<BudgetFormViewModel>();
        builder.Services.AddTransient<AlertThresholdsViewModel>();
        builder.Services.AddTransient<AlertThresholdFormViewModel>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<FixedChargesViewModel>();
        builder.Services.AddTransient<FixedChargeFormViewModel>();

        MauiApp app = builder.Build();
        ServiceResolver.Configure(app.Services);
        ShellRouteRegistry.RegisterRoutes();

        return app;
    }
}
