using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.Services;

/// <summary>Holds who is signed in. The token lives in memory only and ends with the app.</summary>
public sealed class SessionService
{
    private readonly ICyberSecApi _api;

    public SessionService(ICyberSecApi api)
    {
        _api = api;
    }

    public UserProfile? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public event EventHandler? SessionChanged;

    public void SignIn(TokenResponse token)
    {
        _api.AccessToken = token.AccessToken;
        CurrentUser = token.User;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateUser(UserProfile user)
    {
        CurrentUser = user;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        _api.AccessToken = null;
        CurrentUser = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
