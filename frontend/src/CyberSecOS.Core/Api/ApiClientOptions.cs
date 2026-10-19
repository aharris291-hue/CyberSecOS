namespace CyberSecOS.Core.Api;

/// <summary>Connection settings for the CyberSecOS backend.</summary>
public sealed class ApiClientOptions
{
    public const string DefaultBaseUrl = "http://127.0.0.1:8000/";

    public Uri BaseAddress { get; init; } = new(DefaultBaseUrl);

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Builds options from the CYBERSECOS_API_URL environment variable when set.</summary>
    public static ApiClientOptions FromEnvironment(string? configuredUrl = null)
    {
        var url = Environment.GetEnvironmentVariable("CYBERSECOS_API_URL") ?? configuredUrl ?? DefaultBaseUrl;
        if (!url.EndsWith('/'))
        {
            url += "/";
        }
        return new ApiClientOptions { BaseAddress = new Uri(url, UriKind.Absolute) };
    }
}
