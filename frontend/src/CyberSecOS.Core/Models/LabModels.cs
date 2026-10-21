namespace CyberSecOS.Core.Models;

public static class LabTaskTypes
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Choice = "choice";
    public const string Set = "set";
}

/// <summary>A file the learner analyses: a log, capture summary, CSV, config or email.</summary>
public sealed record LabArtifact(string Name, string Kind, string Description, string Content);

/// <summary>A lab question as the server shows it; the answer key never leaves the server.</summary>
public sealed record LabTask(int Index, string Prompt, string Type, IReadOnlyList<string> Options);

public sealed record LabStatus(int Attempts, int? BestScore, bool Passed);

public sealed record LabDetail(
    int Id,
    string Slug,
    string Title,
    int LessonId,
    string Scenario,
    int PassMark,
    IReadOnlyList<LabArtifact> Artifacts,
    IReadOnlyList<LabTask> Tasks,
    LabStatus Status);

/// <summary>One answer per task, in task order. Choice tasks send the option index as text.</summary>
public sealed record LabSubmission(IReadOnlyList<string?> Answers);

public sealed record LabTaskResult(int Index, bool IsCorrect, string Feedback);

public sealed record LabResult(
    int Score,
    int Correct,
    int Total,
    bool Passed,
    int PassMark,
    IReadOnlyList<LabTaskResult> Results,
    bool LabCompleted,
    string LessonStatus);

public sealed record LabSummary(
    int Id,
    string Slug,
    string Title,
    int LessonId,
    string LessonTitle,
    string CourseSlug,
    string CourseTitle,
    int TaskCount,
    int PassMark,
    LabStatus Status);
