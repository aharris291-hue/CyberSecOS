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

    public static LabDetail Lab(int lessonId) =>
        new(5, "lab-ssh", "Prove the brute force", lessonId, "Read **auth.log** and answer.", 80,
            [new LabArtifact("auth.log", "log", "sshd entries", "Failed password for root from 185.220.101.33"),
             new LabArtifact("notes.txt", "text", "context", "web02")],
            [new LabTask(0, "Which IP?", LabTaskTypes.Text, []),
             new LabTask(1, "How many?", LabTaskTypes.Number, []),
             new LabTask(2, "Which ports?", LabTaskTypes.Set, []),
             new LabTask(3, "Best response?", LabTaskTypes.Choice, ["Ignore", "Isolate", "Reboot"])],
            new LabStatus(0, null, false));

    /// <summary>Marks a lab whose answers are "1.2.3.4", "42", "22,80" and option 1.</summary>
    public static LabResult GradeLab(LabSubmission submission)
    {
        string?[] key = ["1.2.3.4", "42", "22,80", "1"];
        var results = submission.Answers
            .Select((a, i) => new LabTaskResult(i, a == key[i], a == key[i] ? $"Why {i}" : $"Hint {i}"))
            .ToList();
        var correct = results.Count(r => r.IsCorrect);
        var score = (int)Math.Round(100.0 * correct / results.Count);
        return new LabResult(score, correct, results.Count, score >= 80, 80, results, score >= 80, LessonStatuses.InProgress);
    }

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

    public static Certification Cert(string id = "secplus", string name = "CompTIA Security+", string code = "SY0-701", int priority = 1) =>
        new(id, name, code, priority, []);

    public static ExamInfo ExamInfo(string certId) => certId switch
    {
        "secplus" => new ExamInfo("secplus", "CompTIA Security+", "SY0-701", 40, 20, 80),
        _ => new ExamInfo(certId, certId, certId.ToUpperInvariant(), 8, 20, 80),
    };

    public static ExamPaper ExamPaper(string certId, int size)
    {
        var count = Math.Min(size, ExamInfo(certId).AvailableQuestions);
        var questions = Enumerable.Range(1, count)
            .Select(i => new QuizQuestion(i, i, $"Question {i}", ["a", "b", "c", "d"]))
            .ToList();
        var info = ExamInfo(certId);
        return new ExamPaper(certId, info.Name, info.ExamCode, info.PassMark, questions);
    }

    /// <summary>Grades every answer as correct when its selected index is 0.</summary>
    public static ExamResult ExamResult(string certId, ExamSubmission submission)
    {
        var total = submission.Answers.Count;
        var correct = submission.Answers.Count(a => a.SelectedIndex == 0);
        var score = total == 0 ? 0 : (int)Math.Round(100.0 * correct / total);
        var breakdown = new List<ObjectiveScore>
        {
            new("secplus:1.1", "General Security Concepts", correct, total, total == 0 ? 0 : (int)Math.Round(100.0 * correct / total)),
        };
        return new ExamResult(certId, score, correct, total, score >= 80, 80, breakdown);
    }

    public static ExamAttempt ExamAttempt(int id, string certId, int score, bool passed) =>
        new(id, certId, score, score, 20, passed, 600, new DateTime(2026, 10, 19, 20, 0, 0));

    public static DashboardData Dashboard()
    {
        var summary = Summary();
        return new DashboardData("Adam", "SOC Analyst", summary.Totals, summary.Week, summary.NextLesson,
            Daily(14), summary.Courses, summary.Certifications, summary.RecentActivity);
    }

    public static StudyPlan StudyPlan() => new(
        WeeklyGoalHours: 10,
        WeeklyTargetMinutes: 600,
        TotalRemainingLessons: 3,
        TotalRemainingMinutes: 195,
        EstimatedWeeksToFinish: 1,
        EstimatedFinishDate: new DateOnly(2026, 10, 27),
        OnTrack: true,
        FocusCertificationId: "secplus",
        Certifications:
        [
            new CertificationPace("secplus", "CompTIA Security+", "SY0-701",
                new DateOnly(2026, 12, 1), 42, 2, 130, 65, "on_track"),
            new CertificationPace("sc900", "Microsoft SC-900", "SC-900",
                null, null, 1, 65, null, "no_date"),
        ],
        Schedule:
        [
            new PlanWeek(1, "2026-10-20", 195,
            [
                new PlannedLesson(1, "Active Directory and Authentication Monitoring", "Windows Security",
                    12, 54, new DateOnly(2026, 12, 1)),
                new PlannedLesson(2, "Investigating an Alert in the SIEM", "SIEM Operations",
                    12, 52, new DateOnly(2026, 12, 1)),
                new PlannedLesson(3, "Identity Fundamentals", "Cloud Security", 10, 55, null),
            ]),
        ]);

    public static Analytics Analytics() => new(
        StudyTrend:
        [
            new WeeklyStudy("2026-10-05", 0, 0, 0),
            new WeeklyStudy("2026-10-12", 120, 2, 60),
            new WeeklyStudy("2026-10-19", 180, 3, 100),
        ],
        StudyPatterns: new StudyPatterns(
            ByWeekday:
            [
                new WeekdayStudy(0, "Mon", 60),
                new WeekdayStudy(1, "Tue", 0),
                new WeekdayStudy(2, "Wed", 90),
                new WeekdayStudy(3, "Thu", 0),
                new WeekdayStudy(4, "Fri", 30),
                new WeekdayStudy(5, "Sat", 120),
                new WeekdayStudy(6, "Sun", 0),
            ],
            ByPartOfDay:
            [
                new PartOfDayStudy("Morning", 60),
                new PartOfDayStudy("Afternoon", 30),
                new PartOfDayStudy("Evening", 180),
                new PartOfDayStudy("Night", 30),
            ],
            PeakPartOfDay: "Evening"),
        Velocity: new LearningVelocity(
            TotalLessons: 72,
            CompletedLessons: 8,
            RemainingLessons: 64,
            CompletedLast4Weeks: 4,
            LessonsPerWeek: 1.0,
            ProjectedWeeksToFinish: 64,
            ProjectedFinishDate: new DateOnly(2028, 1, 10)),
        QuizPerformance: new QuizPerformance(
            TotalAttempts: 6,
            AverageScore: 72,
            ScoreTrend: 15,
            WeakestDomains:
            [
                new WeakDomain("Linux Security", 55, 2),
                new WeakDomain("Networking", 70, 3),
            ],
            WeakestObjectives:
            [
                new WeakObjective("secplus:4.9", "Incident response", "CompTIA Security+", 50, 1),
                new WeakObjective("cysa:2.1", "Threat intelligence", "CompTIA CySA+", 65, 2),
            ]));
}
