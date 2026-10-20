using System.Net;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class ProfileViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly ProfileViewModel _vm;

    public ProfileViewModelTests()
    {
        _session = new SessionService(_api);
        _session.SignIn(FakeApi.Token(FakeApi.Adam));
        _vm = new ProfileViewModel(_api, _session);
    }

    [Fact]
    public async Task LoadsProfile()
    {
        await _vm.OnNavigatedToAsync();

        Assert.Equal("adam", _vm.Username);
        Assert.Equal("SOC Analyst", _vm.TargetRole);
        Assert.Equal(10, _vm.WeeklyGoalHours);
    }

    [Fact]
    public async Task SaveUpdatesServerAndSession()
    {
        await _vm.OnNavigatedToAsync();
        _vm.DisplayName = "Adam H";
        _vm.WeeklyGoalHours = 15;

        await _vm.SaveCommand.ExecuteAsync(null);

        var update = Assert.IsType<ProfileUpdate>(_api.Calls.Single());
        Assert.Equal(15, update.WeeklyGoalHours);
        Assert.Equal("Adam H", _session.CurrentUser!.DisplayName);
        Assert.Equal("Profile saved.", _vm.StatusMessage);
    }

    [Fact]
    public async Task SaveRejectsOutOfRangeGoal()
    {
        _vm.WeeklyGoalHours = 0;

        await _vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_api.Calls);
        Assert.NotNull(_vm.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordSignsBackInWithTheFreshToken()
    {
        _vm.CurrentPassword = "Correct-Horse-42";
        _vm.NewPassword = "Another-Secret-77";

        await _vm.ChangePasswordCommand.ExecuteAsync(null);

        // The returned token replaces the now-invalidated one on the client.
        Assert.Equal("token-after-change", _api.AccessToken);
        Assert.Contains("Password changed", _vm.StatusMessage);
        Assert.Empty(_vm.CurrentPassword);
        Assert.Empty(_vm.NewPassword);
    }

    [Fact]
    public async Task ChangePasswordReportsServerError()
    {
        _api.ChangePassword = _ => throw FakeApi.Error(HttpStatusCode.BadRequest, "Current password is incorrect");
        _vm.CurrentPassword = "nope";
        _vm.NewPassword = "Another-Secret-77";

        await _vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("Current password is incorrect", _vm.ErrorMessage);
        Assert.Empty(_vm.CurrentPassword);
        Assert.Empty(_vm.NewPassword);
    }
}
