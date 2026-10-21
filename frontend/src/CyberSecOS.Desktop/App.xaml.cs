using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyberSecOS.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;
    private LocalBackend? _backend;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = ApiClientOptions.FromEnvironment(ReadConfiguredApiUrl());
        var splash = ShowStartingWindow();
        options = await StartBundledServerAsync(options);

        _services = ConfigureServices(options);
        var shell = _services.GetRequiredService<MainViewModel>();
        var window = new MainWindow { DataContext = shell };
        MainWindow = window;
        window.Show();
        splash.Close();
        shell.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _backend?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// An installed build carries the API in a "server" folder next to the app;
    /// start it unless a server is already answering (for example a developer's).
    /// </summary>
    private async Task<ApiClientOptions> StartBundledServerAsync(ApiClientOptions options)
    {
        var serverPath = Path.Combine(AppContext.BaseDirectory, "server", "cybersecos-server.exe");
        _backend = new LocalBackend(serverPath, LocalBackend.DefaultDataDirectory(), new SystemBackendHost());
        try
        {
            var address = await _backend.EnsureRunningAsync(options.BaseAddress);
            return new ApiClientOptions { BaseAddress = address, Timeout = options.Timeout };
        }
        catch (BackendStartException ex)
        {
            MessageBox.Show(ex.Message, "CyberSecOS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return options;
        }
    }

    private static Window ShowStartingWindow()
    {
        var splash = new Window
        {
            Title = "CyberSecOS",
            Width = 340,
            Height = 110,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = FindBrush("WindowBackgroundBrush", Brushes.Black),
            Content = new TextBlock
            {
                Text = "Starting CyberSecOS...",
                Foreground = FindBrush("TextPrimaryBrush", Brushes.White),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        splash.Show();
        return splash;
    }

    private static Brush FindBrush(string key, Brush fallback) => Current.TryFindResource(key) as Brush ?? fallback;

    private static ServiceProvider ConfigureServices(ApiClientOptions options)
    {
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<ICyberSecApi>(sp => new CyberSecApiClient(sp.GetRequiredService<ApiClientOptions>()));
        services.AddSingleton<SessionService>();
        services.AddSingleton<INavigationService>(sp => new NavigationService(sp.GetRequiredService));
        services.AddSingleton<MainViewModel>();

        // Pages are transient so each visit loads fresh data.
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<CoursesViewModel>();
        services.AddTransient<LessonViewModel>();
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<PlannerViewModel>();
        services.AddTransient<LabsViewModel>();
        services.AddTransient<ExamsViewModel>();
        services.AddTransient<InsightsViewModel>();
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
