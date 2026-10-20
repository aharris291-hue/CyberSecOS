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

    public Task<TokenResponse> ChangePasswordAsync(PasswordChange change, CancellationToken ct = default) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "api/users/me/password", change, ct);

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

    public Task<LessonProgress> MarkTheoryReadAsync(int lessonId, CancellationToken ct = default) =>
        SendAsync<LessonProgress>(HttpMethod.Post, $"api/lessons/{lessonId}/theory", null, ct);

    public Task<LessonProgress> CompleteLabAsync(int lessonId, LabRequest request, CancellationToken ct = default) =>
        SendAsync<LessonProgress>(HttpMethod.Post, $"api/lessons/{lessonId}/lab", request, ct);

    public Task<LessonProgress> ReopenLabAsync(int lessonId, CancellationToken ct = default) =>
        SendAsync<LessonProgress>(HttpMethod.Delete, $"api/lessons/{lessonId}/lab", null, ct);

    public Task<ProgressSummary> GetProgressAsync(CancellationToken ct = default) =>
        SendAsync<ProgressSummary>(HttpMethod.Get, $"api/progress?tz_offset_minutes={LocalOffsetMinutes()}", null, ct);

    public async Task<IReadOnlyList<DailyMinutes>> GetDailyActivityAsync(int days, CancellationToken ct = default) =>
        await SendAsync<List<DailyMinutes>>(
            HttpMethod.Get, $"api/progress/activity?days={days}&tz_offset_minutes={LocalOffsetMinutes()}", null, ct).ConfigureAwait(false);

    public Task<ExamInfo> GetExamInfoAsync(string certId, CancellationToken ct = default) =>
        SendAsync<ExamInfo>(HttpMethod.Get, $"api/certifications/{Uri.EscapeDataString(certId)}/exam", null, ct);

    public Task<ExamPaper> GetExamPaperAsync(string certId, int size, CancellationToken ct = default) =>
        SendAsync<ExamPaper>(HttpMethod.Get, $"api/certifications/{Uri.EscapeDataString(certId)}/exam/paper?size={size}", null, ct);

    public Task<ExamResult> SubmitExamAsync(string certId, ExamSubmission submission, CancellationToken ct = default) =>
        SendAsync<ExamResult>(HttpMethod.Post, $"api/certifications/{Uri.EscapeDataString(certId)}/exam", submission, ct);

    public async Task<IReadOnlyList<ExamAttempt>> GetExamHistoryAsync(string certId, CancellationToken ct = default) =>
        await SendAsync<List<ExamAttempt>>(HttpMethod.Get, $"api/certifications/{Uri.EscapeDataString(certId)}/exam/history", null, ct).ConfigureAwait(false);

    public Task<ExamTarget?> GetExamTargetAsync(string certId, CancellationToken ct = default) =>
        SendAsync<ExamTarget?>(HttpMethod.Get, $"api/certifications/{Uri.EscapeDataString(certId)}/exam/target?tz_offset_minutes={LocalOffsetMinutes()}", null, ct);

    public Task<ExamTarget> SetExamTargetAsync(string certId, ExamTargetUpdate update, CancellationToken ct = default) =>
        SendAsync<ExamTarget>(HttpMethod.Put, $"api/certifications/{Uri.EscapeDataString(certId)}/exam/target?tz_offset_minutes={LocalOffsetMinutes()}", update, ct);

    public Task ClearExamTargetAsync(string certId, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/certifications/{Uri.EscapeDataString(certId)}/exam/target", null, ct);

    public Task<DashboardData> GetDashboardAsync(CancellationToken ct = default) =>
        SendAsync<DashboardData>(HttpMethod.Get, $"api/dashboard?tz_offset_minutes={LocalOffsetMinutes()}", null, ct);

    public Task<StudyPlan> GetStudyPlanAsync(CancellationToken ct = default) =>
        SendAsync<StudyPlan>(HttpMethod.Get, $"api/plan?tz_offset_minutes={LocalOffsetMinutes()}", null, ct);

    /// <summary>Lets the server bucket study days and streaks in the learner's own time zone.</summary>
    internal static int LocalOffsetMinutes() => (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;

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
            T? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                // Usually a client/server version mismatch; surface it like any other API error.
                throw new ApiException(response.StatusCode, "The server sent a response this app does not understand.", ex);
            }
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
