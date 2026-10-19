using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.Api;

/// <summary>Typed access to the CyberSecOS backend. View models depend on this, not on HttpClient.</summary>
public interface ICyberSecApi
{
    /// <summary>Bearer token sent with every request; set after login.</summary>
    string? AccessToken { get; set; }

    Task<AuthStatus> GetAuthStatusAsync(CancellationToken ct = default);
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<UserProfile> GetProfileAsync(CancellationToken ct = default);
    Task<UserProfile> UpdateProfileAsync(ProfileUpdate update, CancellationToken ct = default);
    Task ChangePasswordAsync(PasswordChange change, CancellationToken ct = default);
}
