using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.Tests.Fakes;

/// <summary>Canned DTOs for view model tests.</summary>
public static class Samples
{
    public static CourseSummary Course(string slug, int order, int completed = 0) =>
        new(order, slug, $"Course {slug}", "Networking", "desc", order, 2, completed, 120);

    public static CourseDetail CourseDetail(string slug) =>
        new(1, slug, $"Course {slug}", "Networking", "desc", 1, 2, 0, 120,
        [
            new LessonSummary(10, $"{slug}-1", "First", 1, "s", "beginner", 12, 48, LessonStatuses.NotStarted),
            new LessonSummary(11, $"{slug}-2", "Second", 2, "s", "beginner", 10, 50, LessonStatuses.NotStarted),
        ]);

    public static LessonProgress Progress(string status = LessonStatuses.NotStarted, bool theory = false, bool lab = false, bool quiz = false) =>
        new(status, theory, lab, quiz ? 100 : null, quiz, null, null);

    public static LessonDetail Lesson(int id, LessonProgress? progress = null, int? previous = null, int? next = 11) =>
        new(id, $"lesson-{id}", "The TCP Handshake", 1, "summary", "beginner", 12, 48,
            progress?.Status ?? LessonStatuses.NotStarted, "networking", "Networking Foundations",
            ["Explain the handshake"], "Theory text", [new Example("Example", "body")],
            new Exercise("Lab", "VM", ["Wireshark"], ["step 1", "step 2"], "notes"),
            "You can read a handshake",
            [
                new QuizQuestion(1, 1, "Q1", ["a", "b", "c"]),
                new QuizQuestion(2, 2, "Q2", ["a", "b", "c"]),
            ],
            [new CertObjective("secplus:4.9", "secplus", "4.9", "Use data sources"), new CertObjective("cysa:1.1", "cysa", "1.1", "Architecture")],
            progress ?? Progress(), previous, next);

    /// <summary>Grades as if every question's correct answer is <paramref name="correctIndex"/>.</summary>
    public static QuizResult Grade(QuizSubmission submission, int correctIndex)
    {
        var results = submission.Answers
            .Select((a, i) => new QuestionResult(i + 1, a, correctIndex, a == correctIndex, $"Because {i}"))
            .ToList();
        var correct = results.Count(r => r.IsCorrect);
        var score = (int)Math.Round(100.0 * correct / results.Count);
        return new QuizResult(score, correct, results.Count, score >= 70, 70, results,
            score >= 70 ? LessonStatuses.InProgress : LessonStatuses.InProgress);
    }
}
