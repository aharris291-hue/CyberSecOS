namespace CyberSecOS.Core.Models;

public static class LessonStatuses
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
}

public sealed record CertObjective(string Id, string CertificationId, string Code, string Title);

public sealed record Certification(string Id, string Name, string ExamCode, int Priority, IReadOnlyList<CertObjective> Objectives);

public sealed record CourseSummary(
    int Id,
    string Slug,
    string Title,
    string Domain,
    string Description,
    int Order,
    int LessonCount,
    int CompletedLessons,
    int TotalMinutes);

public sealed record CourseDetail(
    int Id,
    string Slug,
    string Title,
    string Domain,
    string Description,
    int Order,
    int LessonCount,
    int CompletedLessons,
    int TotalMinutes,
    IReadOnlyList<LessonSummary> Lessons);

public sealed record LessonSummary(
    int Id,
    string Slug,
    string Title,
    int Order,
    string Summary,
    string Difficulty,
    int TheoryMinutes,
    int LabMinutes,
    string Status);

public sealed record Example(string Title, string Body);

public sealed record Exercise(string Title, string Environment, IReadOnlyList<string> Tools, IReadOnlyList<string> Steps, string Deliverable);

public sealed record QuizQuestion(int Id, int Order, string Prompt, IReadOnlyList<string> Options);

public sealed record LessonProgress(
    string Status,
    bool TheoryCompleted,
    bool LabCompleted,
    int? BestQuizScore,
    bool QuizPassed,
    string? LabNotes,
    DateTime? CompletedAt);

public sealed record LessonDetail(
    int Id,
    string Slug,
    string Title,
    int Order,
    string Summary,
    string Difficulty,
    int TheoryMinutes,
    int LabMinutes,
    string Status,
    string CourseSlug,
    string CourseTitle,
    IReadOnlyList<string> LearningObjectives,
    string Theory,
    IReadOnlyList<Example> Examples,
    Exercise Exercise,
    string ExpectedOutcome,
    IReadOnlyList<QuizQuestion> Questions,
    IReadOnlyList<CertObjective> CertificationObjectives,
    LessonProgress Progress,
    int? PreviousLessonId,
    int? NextLessonId,
    bool HasGradedLab = false);

public sealed record QuizSubmission(IReadOnlyList<int?> Answers);

public sealed record QuestionResult(int QuestionId, int? SelectedIndex, int CorrectIndex, bool IsCorrect, string Explanation);

public sealed record QuizResult(int Score, int Correct, int Total, bool Passed, int PassMark, IReadOnlyList<QuestionResult> Results, string LessonStatus);
