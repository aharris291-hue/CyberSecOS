namespace CyberSecOS.Core.Models;

public sealed record LabRequest(string? Notes = null, int? Minutes = null);

public sealed record ProgressTotals(
    int TotalLessons,
    int CompletedLessons,
    int InProgressLessons,
    int CompletionPercent,
    int LabsCompleted,
    int QuizzesPassed,
    int QuizAttempts,
    int? AverageQuizScore,
    int TheoryMinutes,
    int LabMinutes,
    int QuizMinutes,
    int TotalMinutes,
    int? HandsOnPercent,
    int CurrentStreakDays,
    int LongestStreakDays);

public sealed record WeekProgress(string WeekStart, int Minutes, int GoalMinutes, int GoalPercent);

public sealed record CourseProgress(string Slug, string Title, string Domain, int TotalLessons, int CompletedLessons, int Percent);

public sealed record CertificationProgress(
    string Id,
    string Name,
    string ExamCode,
    int ObjectivesTotal,
    int ObjectivesWithContent,
    int ObjectivesMastered,
    int ReadinessPercent);

public sealed record ActivityEntry(string Kind, int Minutes, int? LessonId, string? LessonTitle, DateTime CreatedAt);

public sealed record NextLesson(int Id, string Title, string CourseTitle, string Status, int TheoryMinutes, int LabMinutes);

public sealed record ProgressSummary(
    ProgressTotals Totals,
    WeekProgress Week,
    IReadOnlyList<CourseProgress> Courses,
    IReadOnlyList<CertificationProgress> Certifications,
    IReadOnlyList<ActivityEntry> RecentActivity,
    NextLesson? NextLesson);

public sealed record DailyMinutes(string Date, int TheoryMinutes, int LabMinutes, int QuizMinutes, int TotalMinutes);
