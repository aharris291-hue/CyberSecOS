using System.Text.Json.Serialization;

namespace CyberSecOS.Core.Models;

/// <summary>One week of the study trend: total minutes, active days and a four-week rolling average.</summary>
public sealed record WeeklyStudy(string WeekStart, int Minutes, int ActiveDays, int RollingAverageMinutes);

/// <summary>Minutes studied on one weekday (Monday = 0).</summary>
public sealed record WeekdayStudy(int Weekday, string Label, int Minutes);

/// <summary>Minutes studied in one part of the day (Morning / Afternoon / Evening / Night).</summary>
public sealed record PartOfDayStudy(string Part, int Minutes);

public sealed record StudyPatterns(
    IReadOnlyList<WeekdayStudy> ByWeekday,
    IReadOnlyList<PartOfDayStudy> ByPartOfDay,
    string? PeakPartOfDay);

public sealed record LearningVelocity(
    int TotalLessons,
    int CompletedLessons,
    int RemainingLessons,
    [property: JsonPropertyName("completed_last_4_weeks")] int CompletedLast4Weeks,
    double LessonsPerWeek,
    int? ProjectedWeeksToFinish,
    DateOnly? ProjectedFinishDate);

public sealed record WeakDomain(string Domain, int AverageScore, int LessonsAttempted);

public sealed record WeakObjective(string Id, string Title, string Certification, int AverageScore, int LessonsAttempted);

public sealed record QuizPerformance(
    int TotalAttempts,
    int? AverageScore,
    int? ScoreTrend,
    IReadOnlyList<WeakDomain> WeakestDomains,
    IReadOnlyList<WeakObjective> WeakestObjectives);

public sealed record Analytics(
    IReadOnlyList<WeeklyStudy> StudyTrend,
    StudyPatterns StudyPatterns,
    LearningVelocity Velocity,
    QuizPerformance QuizPerformance);
