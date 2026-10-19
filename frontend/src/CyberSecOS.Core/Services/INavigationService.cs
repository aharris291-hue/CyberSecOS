namespace CyberSecOS.Core.Services;

public interface INavigationService
{
    object? Current { get; }

    event EventHandler? Navigated;

    /// <summary>Shows the page backed by the given view model type.</summary>
    object NavigateTo(Type pageType, Action<object>? configure = null);
}

public static class NavigationServiceExtensions
{
    public static TViewModel NavigateTo<TViewModel>(this INavigationService navigation, Action<TViewModel>? configure = null)
        where TViewModel : class =>
        (TViewModel)navigation.NavigateTo(typeof(TViewModel), configure is null ? null : vm => configure((TViewModel)vm));
}

/// <summary>Resolves page view models through a factory (the DI container in the desktop app).</summary>
public sealed class NavigationService : INavigationService
{
    private readonly Func<Type, object> _factory;

    public NavigationService(Func<Type, object> factory)
    {
        _factory = factory;
    }

    public object? Current { get; private set; }

    public event EventHandler? Navigated;

    public object NavigateTo(Type pageType, Action<object>? configure = null)
    {
        var viewModel = _factory(pageType);
        configure?.Invoke(viewModel);
        Current = viewModel;
        Navigated?.Invoke(this, EventArgs.Empty);
        if (viewModel is IPageViewModel page)
        {
            _ = page.OnNavigatedToAsync();
        }
        return viewModel;
    }
}

/// <summary>Pages that load data when shown.</summary>
public interface IPageViewModel
{
    Task OnNavigatedToAsync();
}
