using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Infrastructure.Repositories;
using PatternAnalyzer.Infrastructure.Services;
using PatternAnalyzer.Presentation.ViewModels;
using PatternAnalyzer.UI.Views;
using Serilog;

namespace PatternAnalyzer.UI;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAnalysisService, PatternAnalyzerService>();
        services.AddSingleton<IPresetRepository, JsonPresetRepository>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ConnectionViewModel>();
        services.AddSingleton<AnalysisViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
