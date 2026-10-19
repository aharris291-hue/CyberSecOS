using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>Home screen: headline stats, what to do next, and progress charts.</summary>
public sealed partial class DashboardViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public DashboardViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    /// <summary>Local clock; replaceable in tests.</summary>
    public Func<DateTime> Now { get; init; } = () => DateTime.Now;

    [ObservableProperty]
    private string _greeting = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNextLesson), nameof(NextLessonCaption), nameof(NextLessonActionLabel))]
    [NotifyCanExecuteChangedFor(nameof(ContinueLearningCommand))]
    private NextLesson? _nextLesson;

    [ObservableProperty]
    private bool _hasStudied;

    [ObservableProperty]
    private int _weekGoalPercent;

    [ObservableProperty]
    private string _weekSummary = string.Empty;

    [ObservableProperty]
    private ChartData _activityChart = ChartData.Empty;

    [ObservableProperty]
    private ChartData _domainChart = ChartData.Empty;

    [ObservableProperty]
    private ChartData _certificationChart = ChartData.Empty;

    [ObservableProperty]
    private ChartData _studyMixChart = ChartData.Empty;

    public ObservableCollection<StatTile> Tiles { get; } = [];

    public ObservableCollection<ActivityItem> RecentActivity { get; } = [];

    public bool HasNextLesson => NextLesson is not null;

    public string NextLessonCaption => NextLesson is null
        ? "You have completed every lesson available. New content is on the roadmap."
        : $"{NextLesson.CourseTitle}  ·  {NextLesson.TheoryMinutes} min theory, {NextLesson.LabMinutes} min lab";

    public string NextLessonActionLabel => NextLesson?.Status == LessonStatuses.InProgress ? "Continue lesson" : "Start lesson";

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () => Apply(await _api.GetDashboardAsync()));
    }

    internal void Apply(DashboardData data)
    {
        var totals = data.Totals;
        Greeting = $"{GreetingFor(Now())}, {data.DisplayName}";
        Subtitle = $"Your path to {data.TargetRole}: {totals.CompletedLessons} of {totals.TotalLessons} lessons complete";
        NextLesson = data.NextLesson;
        HasStudied = totals.TotalMinutes > 0;

        WeekGoalPercent = data.Week.GoalPercent;
        WeekSummary = $"{FormatHours(data.Week.Minutes)} of {FormatHours(data.Week.GoalMinutes)} this week";

        Tiles.Clear();
        Tiles.Add(new StatTile("Course completion", $"{totals.CompletionPercent}%", $"{totals.CompletedLessons}/{totals.TotalLessons} lessons"));
        Tiles.Add(new StatTile("Study streak", Plural(totals.CurrentStreakDays, "day"), $"Best: {Plural(totals.LongestStreakDays, "day")}"));
        Tiles.Add(new StatTile("This week", FormatHours(data.Week.Minutes), $"{data.Week.GoalPercent}% of weekly goal"));
        Tiles.Add(new StatTile("Labs completed", totals.LabsCompleted.ToString(CultureInfo.InvariantCulture),
            totals.HandsOnPercent is { } handsOn ? $"{handsOn}% hands-on (target 80%)" : "Target: 80% hands-on"));
        Tiles.Add(new StatTile("Quiz average", totals.AverageQuizScore is { } avg ? $"{avg}%" : "-",
            $"{totals.QuizzesPassed} passed"));

        ActivityChart = new ChartData(
            data.DailyActivity.Select(d => DayLabel(d.Date)).ToList(),
            [
                new ChartSeries("Theory", data.DailyActivity.Select(d => (double)d.TheoryMinutes).ToList(), ChartColors.Theory),
                new ChartSeries("Labs", data.DailyActivity.Select(d => (double)d.LabMinutes).ToList(), ChartColors.Lab),
                new ChartSeries("Quizzes", data.DailyActivity.Select(d => (double)d.QuizMinutes).ToList(), ChartColors.Quiz),
            ]);

        DomainChart = new ChartData(
            data.Courses.Select(c => c.Domain).ToList(),
            [new ChartSeries("Completed %", data.Courses.Select(c => (double)c.Percent).ToList(), ChartColors.Progress)],
            MaxValue: 100);

        CertificationChart = new ChartData(
            data.Certifications.Select(c => c.ExamCode).ToList(),
            [new ChartSeries("Readiness %", data.Certifications.Select(c => (double)c.ReadinessPercent).ToList(), ChartColors.Readiness)],
            MaxValue: 100);

        StudyMixChart = new ChartData(
            ["Theory", "Labs", "Quizzes"],
            [
                new ChartSeries("Theory", [totals.TheoryMinutes], ChartColors.Theory),
                new ChartSeries("Labs", [totals.LabMinutes], ChartColors.Lab),
                new ChartSeries("Quizzes", [totals.QuizMinutes], ChartColors.Quiz),
            ]);

        RecentActivity.Clear();
        foreach (var entry in data.RecentActivity)
        {
            RecentActivity.Add(ActivityItem.From(entry));
        }
    }

    [RelayCommand(CanExecute = nameof(HasNextLesson))]
    private void ContinueLearning()
    {
        if (NextLesson is { } next)
        {
            _navigation.NavigateTo<LessonViewModel>(vm => vm.LessonId = next.Id);
        }
    }

    [RelayCommand]
    private void BrowseCourses() => _navigation.NavigateTo<CoursesViewModel>();

    internal static string GreetingFor(DateTime now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening",
    };

    internal static string FormatHours(int minutes) =>
        minutes % 60 == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{minutes / 60} h")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes / 60.0:0.#} h");

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";

    private static string DayLabel(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day.ToString("d MMM", CultureInfo.InvariantCulture)
            : isoDate;
}
