using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.Api;

public sealed class CyberSecApiClient : ICyberSecApi
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    public CyberSecApiClient(HttpClient http)
    {
        _http = http;
    }

    public CyberSecApiClient(ApiClientOptions options)
        : this(new HttpClient { BaseAddress = options.BaseAddress, Timeout = options.Timeout })
    {
    }

    public string? AccessToken { get; set; }

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken ct = default) =>
        SendAsync<AuthStatus>(HttpMethod.Get, "api/auth/status", null, ct);

    public Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "api/auth/register", request, ct);

    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "api/auth/login", request, ct);

    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Get, "api/users/me", null, ct);

    public Task<UserProfile> UpdateProfileAsync(ProfileUpdate update, CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Patch, "api/users/me", update, ct);

    public Task ChangePasswordAsync(PasswordChange change, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/users/me/password", change, ct);

    public async Task<IReadOnlyList<Certification>> GetCertificationsAsync(CancellationToken ct = default) =>
        await SendAsync<List<Certification>>(HttpMethod.Get, "api/certifications", null, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken ct = default) =>
        await SendAsync<List<CourseSummary>>(HttpMethod.Get, "api/courses", null, ct).ConfigureAwait(false);

    public Task<CourseDetail> GetCourseAsync(string slug, CancellationToken ct = default) =>
        SendAsync<CourseDetail>(HttpMethod.Get, $"api/courses/{Uri.EscapeDataString(slug)}", null, ct);

    public Task<LessonDetail> GetLessonAsync(int lessonId, CancellationToken ct = default) =>
        SendAsync<LessonDetail>(HttpMethod.Get, $"api/lessons/{lessonId}", null, ct);

    public Task<QuizResult> SubmitQuizAsync(int lessonId, QuizSubmission submission, CancellationToken ct = default) =>
        SendAsync<QuizResult>(HttpMethod.Post, $"api/lessons/{lessonId}/quiz", submission, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (AccessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(null, "Cannot reach the CyberSecOS server. Is the backend running?", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(null, "The CyberSecOS server did not respond in time.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(response.StatusCode, await ReadErrorAsync(response, ct).ConfigureAwait(false));
            }
            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return default!;
            }
            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
            return result ?? throw new ApiException(response.StatusCode, "The server returned an empty response.");
        }
    }

    /// <summary>FastAPI errors look like {"detail": "..."} or {"detail": [{"msg": "..."}]}.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var fallback = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return fallback;
            }
            return detail.ValueKind switch
            {
                JsonValueKind.String => detail.GetString() ?? fallback,
                JsonValueKind.Array when detail.GetArrayLength() > 0 &&
                                         detail[0].TryGetProperty("msg", out var msg) => msg.GetString() ?? fallback,
                _ => fallback,
            };
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
