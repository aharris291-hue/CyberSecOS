using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>
/// Sign-in page. On a fresh install (no account yet) it switches to first-run
/// setup and creates the single learner account instead.
/// </summary>
public sealed partial class LoginViewModel : PageViewModel, IPageViewModel
{
    public const int MinPasswordLength = 10;

    private readonly ICyberSecApi _api;
    private readonly SessionService _session;

    public LoginViewModel(ICyberSecApi api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(SubmitLabel))]
    private bool _isFirstRun;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    public string Title => IsFirstRun ? "Create your CyberSecOS account" : "Sign in to CyberSecOS";

    public string SubmitLabel => IsFirstRun ? "Create account" : "Sign in";

    private bool _statusLoaded;

    public async Task OnNavigatedToAsync()
    {
        _statusLoaded = await RunAsync(async () => IsFirstRun = !(await _api.GetAuthStatusAsync()).Registered);
    }

    private bool CanSubmit() => !string.IsNullOrWhiteSpace(Username) && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        // The server may not have been running when the page opened; check again before submitting.
        if (!_statusLoaded)
        {
            await OnNavigatedToAsync();
            if (!_statusLoaded)
            {
                return;
            }
            if (IsFirstRun)
            {
                // The form was showing sign-in fields; let the learner fill in the setup fields first.
                return;
            }
        }

        if (IsFirstRun && Validate() is { } problem)
        {
            ErrorMessage = problem;
            return;
        }

        TokenResponse? token = null;
        var ok = await RunAsync(async () =>
        {
            token = IsFirstRun
                ? await _api.RegisterAsync(new RegisterRequest(Username.Trim(), Password, DisplayName.Trim()))
                : await _api.LoginAsync(new LoginRequest(Username.Trim(), Password));
        });

        Password = string.Empty;
        ConfirmPassword = string.Empty;
        if (ok && token is not null)
        {
            _session.SignIn(token);
        }
    }

    /// <summary>Client-side checks mirroring the server's password policy, for instant feedback.</summary>
    internal string? Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            return "Enter the name you'd like to be called.";
        }
        if (Password.Length < MinPasswordLength)
        {
            return $"Password must be at least {MinPasswordLength} characters.";
        }
        if (!Password.Any(char.IsLetter) || !Password.Any(char.IsDigit))
        {
            return "Password must contain a letter and a digit.";
        }
        if (Password != ConfirmPassword)
        {
            return "Passwords do not match.";
        }
        return null;
    }
}
