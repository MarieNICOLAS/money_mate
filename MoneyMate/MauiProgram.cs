using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MoneyMate.Configuration;
using MoneyMate.Data.Context;
using MoneyMate.Data.Repositories;
using MoneyMate.Infrastructure;
using MoneyMate.Services.Common;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Auth;
using MoneyMate.ViewModels.Dashboard;
using MoneyMate.Views.Auth;
using MoneyMate.Views.Dashboard;

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

        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<ISessionManager, SessionManager>();
        builder.Services.AddSingleton<IStartupCoordinator, StartupCoordinator>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<DashboardPage>();

        // ===============================
        // VIEWMODELS (TRANSIENT = PERFORMANCE)
        // ===============================
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        MauiApp app = builder.Build();
        ServiceResolver.Configure(app.Services);
        ShellRouteRegistry.RegisterRoutes();

        return app;
    }
}
