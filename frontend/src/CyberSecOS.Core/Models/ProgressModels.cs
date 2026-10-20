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
    int ReadinessPercent,
    DateOnly? TargetDate = null,
    int? DaysUntilExam = null);

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

public sealed record CertificationPace(
    string Id,
    string Name,
    string ExamCode,
    DateOnly? TargetDate,
    int? DaysUntilExam,
    int RemainingLessons,
    int RemainingMinutes,
    int? RequiredMinutesPerWeek,
    string Status);

public sealed record PlannedLesson(
    int Id,
    string Title,
    string CourseTitle,
    int TheoryMinutes,
    int LabMinutes,
    DateOnly? TargetDate);

public sealed record PlanWeek(int WeekIndex, string WeekStart, int PlannedMinutes, IReadOnlyList<PlannedLesson> Lessons);

public sealed record StudyPlan(
    int WeeklyGoalHours,
    int WeeklyTargetMinutes,
    int TotalRemainingLessons,
    int TotalRemainingMinutes,
    int EstimatedWeeksToFinish,
    DateOnly? EstimatedFinishDate,
    bool? OnTrack,
    string? FocusCertificationId,
    IReadOnlyList<CertificationPace> Certifications,
    IReadOnlyList<PlanWeek> Schedule);
