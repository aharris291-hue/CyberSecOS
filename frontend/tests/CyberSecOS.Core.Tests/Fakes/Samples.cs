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

    public static ProgressSummary Summary() => new(
        new ProgressTotals(12, 3, 2, 25, 4, 3, 5, 88, 36, 160, 12, 208, 77, 4, 6),
        new WeekProgress("2026-10-19", 150, 600, 25),
        [
            new CourseProgress("networking", "Networking Foundations", "Networking", 3, 2, 67),
            new CourseProgress("windows", "Windows Security", "Windows Security", 2, 1, 50),
        ],
        [
            new CertificationProgress("secplus", "CompTIA Security+", "SY0-701", 28, 14, 7, 25),
            new CertificationProgress("sc900", "Microsoft SC-900", "SC-900", 4, 3, 1, 25),
        ],
        [
            new ActivityEntry("lab", 48, 10, "The TCP Handshake", new DateTime(2026, 10, 19, 20, 0, 0)),
            new ActivityEntry("theory", 12, 10, "The TCP Handshake", new DateTime(2026, 10, 19, 19, 0, 0)),
        ],
        new NextLesson(11, "Ports, Services and DNS", "Networking Foundations", LessonStatuses.NotStarted, 10, 50));

    public static IReadOnlyList<DailyMinutes> Daily(int days) =>
        Enumerable.Range(0, days)
            .Select(i => new DailyMinutes(new DateOnly(2026, 10, 1).AddDays(i).ToString("yyyy-MM-dd"), i, 4 * i, 0, 5 * i))
            .ToList();

    public static DashboardData Dashboard()
    {
        var summary = Summary();
        return new DashboardData("Adam", "SOC Analyst", summary.Totals, summary.Week, summary.NextLesson,
            Daily(14), summary.Courses, summary.Certifications, summary.RecentActivity);
    }
}
