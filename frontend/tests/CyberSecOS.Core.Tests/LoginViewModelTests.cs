using System.Net;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class LoginViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly LoginViewModel _vm;

    public LoginViewModelTests()
    {
        _session = new SessionService(_api);
        _vm = new LoginViewModel(_api, _session);
    }

    [Fact]
    public async Task FreshInstallShowsFirstRunSetup()
    {
        _api.AuthStatus = () => new AuthStatus(false);

        await _vm.OnNavigatedToAsync();

        Assert.True(_vm.IsFirstRun);
        Assert.Equal("Create account", _vm.SubmitLabel);
    }

    [Fact]
    public async Task ExistingAccountShowsSignIn()
    {
        await _vm.OnNavigatedToAsync();

        Assert.False(_vm.IsFirstRun);
        Assert.Equal("Sign in", _vm.SubmitLabel);
    }

    [Fact]
    public void SubmitDisabledUntilCredentialsEntered()
    {
        Assert.False(_vm.SubmitCommand.CanExecute(null));
        _vm.Username = "adam";
        _vm.Password = "x";
        Assert.True(_vm.SubmitCommand.CanExecute(null));
    }

    [Fact]
    public async Task SubmitRetriesWhenServerWasOfflineAtStartup()
    {
        _api.AuthStatus = () => throw new CyberSecOS.Core.Api.ApiException(null, "Cannot reach the CyberSecOS server.");
        await _vm.OnNavigatedToAsync();
        Assert.Contains("Cannot reach", _vm.ErrorMessage);

        _api.AuthStatus = () => new AuthStatus(true);
        _vm.Username = "adam";
        _vm.Password = "Correct-Horse-42";
        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.True(_session.IsSignedIn);
    }

    [Fact]
    public async Task OfflineStartupOnFreshInstallSwitchesToSetupBeforeSubmitting()
    {
        _api.AuthStatus = () => throw new CyberSecOS.Core.Api.ApiException(null, "Cannot reach the CyberSecOS server.");
        await _vm.OnNavigatedToAsync();

        _api.AuthStatus = () => new AuthStatus(false);
        _vm.Username = "adam";
        _vm.Password = "Correct-Horse-42";
        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.True(_vm.IsFirstRun);
        Assert.False(_session.IsSignedIn);
        Assert.DoesNotContain(_api.Calls, c => c is RegisterRequest or LoginRequest);
    }

    [Fact]
    public async Task SuccessfulLoginSignsIn()
    {
        _vm.Username = " adam ";
        _vm.Password = "Correct-Horse-42";

        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.True(_session.IsSignedIn);
        Assert.Equal("token-123", _api.AccessToken);
        Assert.Equal("adam", Assert.IsType<LoginRequest>(_api.Calls.Single()).Username);
        Assert.Empty(_vm.Password);
    }

    [Fact]
    public async Task FailedLoginShowsServerMessageAndClearsPassword()
    {
        _api.Login = _ => throw FakeApi.Error(HttpStatusCode.Unauthorized, "Invalid username or password");
        _vm.Username = "adam";
        _vm.Password = "wrong";

        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.False(_session.IsSignedIn);
        Assert.Equal("Invalid username or password", _vm.ErrorMessage);
        Assert.Empty(_vm.Password);
        Assert.False(_vm.IsBusy);
    }

    [Theory]
    [InlineData("", "Correct-Horse-42", "Correct-Horse-42", "Enter the name")]
    [InlineData("Adam", "short1", "short1", "at least 10")]
    [InlineData("Adam", "onlyletters-here", "onlyletters-here", "letter and a digit")]
    [InlineData("Adam", "Correct-Horse-42", "Different-Horse-42", "do not match")]
    public async Task FirstRunValidatesBeforeCallingServer(string name, string password, string confirm, string expected)
    {
        _api.AuthStatus = () => new AuthStatus(false);
        await _vm.OnNavigatedToAsync();
        _vm.Username = "adam";
        _vm.DisplayName = name;
        _vm.Password = password;
        _vm.ConfirmPassword = confirm;

        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.Contains(expected, _vm.ErrorMessage);
        Assert.Empty(_api.Calls);
    }

    [Fact]
    public async Task FirstRunRegistersAccount()
    {
        _api.AuthStatus = () => new AuthStatus(false);
        await _vm.OnNavigatedToAsync();
        _vm.Username = "adam";
        _vm.DisplayName = "Adam";
        _vm.Password = "Correct-Horse-42";
        _vm.ConfirmPassword = "Correct-Horse-42";

        await _vm.SubmitCommand.ExecuteAsync(null);

        Assert.IsType<RegisterRequest>(_api.Calls.Single());
        Assert.True(_session.IsSignedIn);
    }
}
