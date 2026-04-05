using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MoneyMate.Services.Implementations;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Auth;
using MoneyMate.Views.Auth;

namespace MoneyMate
{
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                    fonts.AddFont("Lora-Regular.ttf", "Lora");
                    fonts.AddFont("Lora-Bold.ttf", "LoraBold");
                    fonts.AddFont("FunnelDisplay-Regular.ttf", "FunnelDisplay");
                });

            // ── Services ──────────────────────────────────────────────────
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

            // ── ViewModels ────────────────────────────────────────────────
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();

            // ── Pages ─────────────────────────────────────────────────────
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
