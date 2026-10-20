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

    Task<IReadOnlyList<Certification>> GetCertificationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CourseSummary>> GetCoursesAsync(CancellationToken ct = default);
    Task<CourseDetail> GetCourseAsync(string slug, CancellationToken ct = default);
    Task<LessonDetail> GetLessonAsync(int lessonId, CancellationToken ct = default);
    Task<QuizResult> SubmitQuizAsync(int lessonId, QuizSubmission submission, CancellationToken ct = default);

    Task<LessonProgress> MarkTheoryReadAsync(int lessonId, CancellationToken ct = default);
    Task<LessonProgress> CompleteLabAsync(int lessonId, LabRequest request, CancellationToken ct = default);
    Task<LessonProgress> ReopenLabAsync(int lessonId, CancellationToken ct = default);
    Task<ProgressSummary> GetProgressAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyMinutes>> GetDailyActivityAsync(int days, CancellationToken ct = default);

    Task<DashboardData> GetDashboardAsync(CancellationToken ct = default);

    Task<ExamInfo> GetExamInfoAsync(string certId, CancellationToken ct = default);
    Task<ExamPaper> GetExamPaperAsync(string certId, int size, CancellationToken ct = default);
    Task<ExamResult> SubmitExamAsync(string certId, ExamSubmission submission, CancellationToken ct = default);
    Task<IReadOnlyList<ExamAttempt>> GetExamHistoryAsync(string certId, CancellationToken ct = default);
    Task<ExamTarget?> GetExamTargetAsync(string certId, CancellationToken ct = default);
    Task<ExamTarget> SetExamTargetAsync(string certId, ExamTargetUpdate update, CancellationToken ct = default);
    Task ClearExamTargetAsync(string certId, CancellationToken ct = default);

    Task<StudyPlan> GetStudyPlanAsync(CancellationToken ct = default);
}
