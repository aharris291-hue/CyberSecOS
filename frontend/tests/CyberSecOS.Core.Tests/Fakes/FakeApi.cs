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

    public Func<IReadOnlyList<Certification>> Certifications { get; set; } = () => [];
    public Func<IReadOnlyList<CourseSummary>> Courses { get; set; } = () => [Samples.Course("networking", 1), Samples.Course("windows", 2)];
    public Func<string, CourseDetail> Course { get; set; } = slug => Samples.CourseDetail(slug);
    public Func<int, LessonDetail> Lesson { get; set; } = id => Samples.Lesson(id);
    public Func<int, QuizSubmission, QuizResult> Quiz { get; set; } = (_, s) => Samples.Grade(s, correctIndex: 1);

    public Func<int, LessonProgress> TheoryRead { get; set; } = _ => Samples.Progress(LessonStatuses.InProgress, theory: true);
    public Func<int, LabRequest, LessonProgress> Lab { get; set; } = (_, _) => Samples.Progress(LessonStatuses.InProgress, lab: true);
    public Func<int, LessonProgress> Reopen { get; set; } = _ => Samples.Progress(LessonStatuses.InProgress);
    public Func<ProgressSummary> Progress { get; set; } = Samples.Summary;
    public Func<int, IReadOnlyList<DailyMinutes>> Daily { get; set; } = Samples.Daily;

    public Func<DashboardData> Dashboard { get; set; } = Samples.Dashboard;

    public Func<string, ExamInfo> ExamInfo { get; set; } = Samples.ExamInfo;
    public Func<string, int, ExamPaper> ExamPaper { get; set; } = Samples.ExamPaper;
    public Func<string, ExamSubmission, ExamResult> ExamSubmit { get; set; } = (cert, s) => Samples.ExamResult(cert, s);
    public Func<string, IReadOnlyList<ExamAttempt>> ExamHistory { get; set; } = _ => [];
    public Dictionary<string, ExamTarget> ExamTargets { get; } = [];

    public static TokenResponse Token(UserProfile user) => new("token-123", "bearer", 3600, user);

    public static ApiException Error(HttpStatusCode code, string message) => new(code, message);

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken ct = default) => Run(AuthStatus);
    public Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) => Run(() => Register(request), request);
    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) => Run(() => Login(request), request);
    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) => Run(Profile);
    public Task<UserProfile> UpdateProfileAsync(ProfileUpdate update, CancellationToken ct = default) => Run(() => UpdateProfile(update), update);
    public Task ChangePasswordAsync(PasswordChange change, CancellationToken ct = default) => Run<object?>(() => { ChangePassword(change); return null; }, change);

    public Task<IReadOnlyList<Certification>> GetCertificationsAsync(CancellationToken ct = default) => Run(Certifications);
    public Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken ct = default) => Run(Courses);
    public Task<CourseDetail> GetCourseAsync(string slug, CancellationToken ct = default) => Run(() => Course(slug), slug);
    public Task<LessonDetail> GetLessonAsync(int lessonId, CancellationToken ct = default) => Run(() => Lesson(lessonId));
    public Task<QuizResult> SubmitQuizAsync(int lessonId, QuizSubmission submission, CancellationToken ct = default) =>
        Run(() => Quiz(lessonId, submission), submission);

    public Task<LessonProgress> MarkTheoryReadAsync(int lessonId, CancellationToken ct = default) => Run(() => TheoryRead(lessonId), $"theory:{lessonId}");
    public Task<LessonProgress> CompleteLabAsync(int lessonId, LabRequest request, CancellationToken ct = default) => Run(() => Lab(lessonId, request), request);
    public Task<LessonProgress> ReopenLabAsync(int lessonId, CancellationToken ct = default) => Run(() => Reopen(lessonId), $"reopen:{lessonId}");
    public Task<ProgressSummary> GetProgressAsync(CancellationToken ct = default) => Run(Progress);
    public Task<IReadOnlyList<DailyMinutes>> GetDailyActivityAsync(int days, CancellationToken ct = default) => Run(() => Daily(days));

    public Task<DashboardData> GetDashboardAsync(CancellationToken ct = default) => Run(Dashboard);

    public Task<ExamInfo> GetExamInfoAsync(string certId, CancellationToken ct = default) => Run(() => ExamInfo(certId), $"exam-info:{certId}");
    public Task<ExamPaper> GetExamPaperAsync(string certId, int size, CancellationToken ct = default) => Run(() => ExamPaper(certId, size), $"exam-paper:{certId}:{size}");
    public Task<ExamResult> SubmitExamAsync(string certId, ExamSubmission submission, CancellationToken ct = default) => Run(() => ExamSubmit(certId, submission), submission);
    public Task<IReadOnlyList<ExamAttempt>> GetExamHistoryAsync(string certId, CancellationToken ct = default) => Run(() => ExamHistory(certId), $"exam-history:{certId}");

    public Task<ExamTarget?> GetExamTargetAsync(string certId, CancellationToken ct = default) =>
        Run(() => ExamTargets.TryGetValue(certId, out var t) ? t : null, $"exam-target-get:{certId}");

    public Task<ExamTarget> SetExamTargetAsync(string certId, ExamTargetUpdate update, CancellationToken ct = default) =>
        Run(() =>
        {
            var target = new ExamTarget(certId, update.ExamDate, update.ExamDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber);
            ExamTargets[certId] = target;
            return target;
        }, update);

    public Task ClearExamTargetAsync(string certId, CancellationToken ct = default) =>
        Run<object?>(() => { ExamTargets.Remove(certId); return null; }, $"exam-target-clear:{certId}");

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
