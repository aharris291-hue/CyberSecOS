using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>
/// Insights: looks across the learner's history rather than the current snapshot —
/// how study is trending, when they study, how fast they are moving, and where their
/// quiz scores say to revise.
/// </summary>
public sealed partial class InsightsViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;

    public InsightsViewModel(ICyberSecApi api, SessionService session)
        : base(session)
    {
        _api = api;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(HasData), nameof(Headline), nameof(VelocitySummary),
        nameof(QuizSummary), nameof(PeakStudyTime), nameof(HasQuizData))]
    private Analytics? _analytics;

    [ObservableProperty]
    private ChartData _trendChart = ChartData.Empty;

    [ObservableProperty]
    private ChartData _weekdayChart = ChartData.Empty;

    public ObservableCollection<WeakAreaItem> WeakestObjectives { get; } = [];

    public ObservableCollection<WeakAreaItem> WeakestDomains { get; } = [];

    public bool HasData => Analytics is not null;

    public bool HasQuizData => Analytics is { QuizPerformance.TotalAttempts: > 0 };

    public string Headline => Analytics?.Velocity switch
    {
        null => string.Empty,
        { CompletedLast4Weeks: 0 } => "You haven't finished a lesson in the last four weeks. A short session gets your momentum back.",
        { LessonsPerWeek: var pace } => $"You're completing about {FormatPace(pace)} a week.",
    };

    public string VelocitySummary
    {
        get
        {
            if (Analytics?.Velocity is not { } v)
            {
                return string.Empty;
            }

            var done = $"{v.CompletedLessons}/{v.TotalLessons} lessons done";
            if (v.RemainingLessons == 0)
            {
                return $"{done} — every lesson complete.";
            }

            var left = $"{v.RemainingLessons} to go";
            return v.ProjectedFinishDate is { } finish
                ? $"{done} · {left} · on track to finish around {finish:d MMM yyyy} at this pace"
                : $"{done} · {left} · complete a few lessons to project a finish date";
        }
    }

    public string QuizSummary
    {
        get
        {
            if (Analytics?.QuizPerformance is not { } q || q.TotalAttempts == 0)
            {
                return "No quiz attempts yet.";
            }

            var avg = q.AverageScore is { } a ? $"{a}% average over {q.TotalAttempts} attempts" : $"{q.TotalAttempts} attempts";
            return q.ScoreTrend switch
            {
                > 0 and var t => $"{avg} · up {t} points lately",
                < 0 and var t => $"{avg} · down {-t} points lately",
                _ => avg,
            };
        }
    }

    public string PeakStudyTime => Analytics?.StudyPatterns.PeakPartOfDay is { } peak
        ? $"You study most in the {peak.ToLowerInvariant()}."
        : string.Empty;

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            var analytics = await _api.GetAnalyticsAsync();
            Analytics = analytics;

            TrendChart = new ChartData(
                analytics.StudyTrend.Select(w => WeekLabel(w.WeekStart)).ToList(),
                [new ChartSeries("Minutes", analytics.StudyTrend.Select(w => (double)w.Minutes).ToList(), ChartColors.Lab)]);

            WeekdayChart = new ChartData(
                analytics.StudyPatterns.ByWeekday.Select(d => d.Label).ToList(),
                [new ChartSeries("Minutes", analytics.StudyPatterns.ByWeekday.Select(d => (double)d.Minutes).ToList(), ChartColors.Theory)]);

            Replace(WeakestObjectives, analytics.QuizPerformance.WeakestObjectives.Select(WeakAreaItem.From));
            Replace(WeakestDomains, analytics.QuizPerformance.WeakestDomains.Select(WeakAreaItem.From));
        });
    }

    internal static string FormatPace(double lessonsPerWeek) =>
        string.Create(CultureInfo.InvariantCulture, $"{lessonsPerWeek:0.#} lesson{(lessonsPerWeek == 1 ? string.Empty : "s")}");

    private static string WeekLabel(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day.ToString("d MMM", CultureInfo.InvariantCulture)
            : isoDate;

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}

/// <summary>A weak domain or objective, phrased for display.</summary>
public sealed record WeakAreaItem(string Title, string Caption, string ScoreLabel)
{
    public static WeakAreaItem From(WeakObjective objective) =>
        new(objective.Title,
            $"{objective.Certification} · {Plural(objective.LessonsAttempted, "lesson")} attempted",
            $"{objective.AverageScore}%");

    public static WeakAreaItem From(WeakDomain domain) =>
        new(domain.Domain,
            Plural(domain.LessonsAttempted, "lesson") + " attempted",
            $"{domain.AverageScore}%");

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
