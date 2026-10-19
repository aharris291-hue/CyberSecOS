using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

public sealed record NavigationItem(string Label, string Icon, Type PageType);

/// <summary>The application shell: login gate, side navigation and the current page.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly SessionService _session;

    public MainViewModel(INavigationService navigation, SessionService session)
    {
        _navigation = navigation;
        _session = session;
        _navigation.Navigated += (_, _) => CurrentPage = _navigation.Current;
        _session.SessionChanged += (_, _) => OnSessionChanged();
    }

    /// <summary>Pages shown in the side bar once signed in. Order is display order.</summary>
    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("Courses", "\uE82D", typeof(CoursesViewModel)),
        new("Profile", "\uE77B", typeof(ProfileViewModel)),
    ];

    /// <summary>Where the learner lands after signing in.</summary>
    public Type HomePage { get; init; } = typeof(CoursesViewModel);

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private bool _isSignedIn;

    [ObservableProperty]
    private string _userDisplayName = string.Empty;

    public void Start() => _navigation.NavigateTo<LoginViewModel>();

    partial void OnSelectedItemChanged(NavigationItem? value)
    {
        if (value is not null && IsSignedIn && CurrentPage?.GetType() != value.PageType)
        {
            Navigate(value.PageType);
        }
    }

    [RelayCommand]
    private void SignOut() => _session.SignOut();

    private void OnSessionChanged()
    {
        var wasSignedIn = IsSignedIn;
        IsSignedIn = _session.IsSignedIn;
        UserDisplayName = _session.CurrentUser?.DisplayName ?? string.Empty;

        if (IsSignedIn && !wasSignedIn)
        {
            SelectedItem = NavigationItems.FirstOrDefault(i => i.PageType == HomePage);
            if (CurrentPage?.GetType() != HomePage)
            {
                Navigate(HomePage);
            }
        }
        else if (!IsSignedIn && wasSignedIn)
        {
            SelectedItem = null;
            _navigation.NavigateTo<LoginViewModel>();
        }
    }

    private void Navigate(Type pageType) => _navigation.NavigateTo(pageType);
}
