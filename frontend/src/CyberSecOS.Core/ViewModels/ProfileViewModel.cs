using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

public sealed partial class ProfileViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly SessionService _session;

    public ProfileViewModel(ICyberSecApi api, SessionService session)
        : base(session)
    {
        _api = api;
        _session = session;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _targetRole = string.Empty;

    [ObservableProperty]
    private int _weeklyGoalHours = 10;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () => Apply(await _api.GetProfileAsync()));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (WeeklyGoalHours is < 1 or > 80)
        {
            ErrorMessage = "Weekly goal must be between 1 and 80 hours.";
            return;
        }
        var ok = await RunAsync(async () =>
        {
            var user = await _api.UpdateProfileAsync(new ProfileUpdate(DisplayName, TargetRole, WeeklyGoalHours));
            Apply(user);
            _session.UpdateUser(user);
        });
        if (ok)
        {
            StatusMessage = "Profile saved.";
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        var ok = await RunAsync(async () =>
        {
            // Changing the password invalidates every existing token, including
            // this session's; the response carries a fresh one to sign back in with.
            var token = await _api.ChangePasswordAsync(new PasswordChange(CurrentPassword, NewPassword));
            _session.SignIn(token);
        });
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        if (ok)
        {
            StatusMessage = "Password changed. Other sessions have been signed out.";
        }
    }

    private void Apply(UserProfile user)
    {
        Username = user.Username;
        DisplayName = user.DisplayName;
        TargetRole = user.TargetRole;
        WeeklyGoalHours = user.WeeklyGoalHours;
    }
}
