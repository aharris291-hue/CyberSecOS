using System.IO;
using System.Text.Json;
using System.Windows;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyberSecOS.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services = ConfigureServices();

        var shell = _services.GetRequiredService<MainViewModel>();
        var window = new MainWindow { DataContext = shell };
        MainWindow = window;
        window.Show();
        shell.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(ApiClientOptions.FromEnvironment(ReadConfiguredApiUrl()));
        services.AddSingleton<ICyberSecApi>(sp => new CyberSecApiClient(sp.GetRequiredService<ApiClientOptions>()));
        services.AddSingleton<SessionService>();
        services.AddSingleton<INavigationService>(sp => new NavigationService(sp.GetRequiredService));
        services.AddSingleton<MainViewModel>();

        // Pages are transient so each visit loads fresh data.
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ProfileViewModel>();
        return services.BuildServiceProvider();
    }

    private static string? ReadConfiguredApiUrl()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return null;
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.TryGetProperty("ApiBaseUrl", out var url) ? url.GetString() : null;
    }
}
