using System.Net;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.Tests.Fakes;

/// <summary>In-memory ICyberSecApi. Tests override the handlers they care about.</summary>
public sealed class FakeApi : ICyberSecApi
{
    public static readonly UserProfile Adam = new(1, "adam", "Adam", "SOC Analyst", 10, new DateTime(2026, 1, 1), null);

    public string? AccessToken { get; set; }

    public List<object> Calls { get; } = [];

    public Func<AuthStatus> AuthStatus { get; set; } = () => new AuthStatus(true);
    public Func<RegisterRequest, TokenResponse> Register { get; set; } = r => Token(Adam with { Username = r.Username, DisplayName = r.DisplayName });
    public Func<LoginRequest, TokenResponse> Login { get; set; } = _ => Token(Adam);
    public Func<UserProfile> Profile { get; set; } = () => Adam;
    public Func<ProfileUpdate, UserProfile> UpdateProfile { get; set; } =
        u => Adam with { DisplayName = u.DisplayName ?? Adam.DisplayName, TargetRole = u.TargetRole ?? Adam.TargetRole, WeeklyGoalHours = u.WeeklyGoalHours ?? Adam.WeeklyGoalHours };
    public Action<PasswordChange> ChangePassword { get; set; } = _ => { };

    public static TokenResponse Token(UserProfile user) => new("token-123", "bearer", 3600, user);

    public static ApiException Error(HttpStatusCode code, string message) => new(code, message);

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken ct = default) => Run(AuthStatus);
    public Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) => Run(() => Register(request), request);
    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) => Run(() => Login(request), request);
    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) => Run(Profile);
    public Task<UserProfile> UpdateProfileAsync(ProfileUpdate update, CancellationToken ct = default) => Run(() => UpdateProfile(update), update);
    public Task ChangePasswordAsync(PasswordChange change, CancellationToken ct = default) => Run<object?>(() => { ChangePassword(change); return null; }, change);

    private Task<T> Run<T>(Func<T> handler, object? request = null)
    {
        if (request is not null)
        {
            Calls.Add(request);
        }
        try
        {
            return Task.FromResult(handler());
        }
        catch (Exception ex)
        {
            return Task.FromException<T>(ex);
        }
    }
}
