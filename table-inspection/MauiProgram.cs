using Microsoft.Extensions.Logging;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Infrastructure.Repositories;
using PatternAnalyzer.Infrastructure.Services;
using PatternAnalyzer.Presentation.ViewModels;

namespace table_inspection
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            // Register Services
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAnalysisService, PatternAnalyzerService>();
            builder.Services.AddSingleton<IPresetRepository, JsonPresetRepository>();

            // Register ViewModels (Transient usually safer for VM if they hold state per view, but Singleton if global)
            // ConnectionViewModel holds state, AnalysisViewModel depends on it.
            // In Blazor Server/Hybrid, a Scoped service is often per-user/circuit. Singleton is app-wide.
            // Since this is a local app (Hybrid), Singleton acts like "one session".
            builder.Services.AddSingleton<ConnectionViewModel>();
            builder.Services.AddSingleton<AnalysisViewModel>();
            builder.Services.AddSingleton<MainViewModel>();

            return builder.Build();
        }
    }
}
