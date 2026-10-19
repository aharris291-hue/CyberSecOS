using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>Detailed progress: per course, per certification, and study history.</summary>
public sealed partial class ProgressViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public ProgressViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StudyTime), nameof(HandsOnSummary), nameof(QuizSummary))]
    private ProgressTotals? _totals;

    public ObservableCollection<CourseProgress> Courses { get; } = [];

    public ObservableCollection<CertificationProgress> Certifications { get; } = [];

    public ObservableCollection<ActivityItem> RecentActivity { get; } = [];

    public string StudyTime => Totals is null ? string.Empty : FormatMinutes(Totals.TotalMinutes);

    public string HandsOnSummary => Totals?.HandsOnPercent is { } percent
        ? $"{percent}% hands-on (target 80%)"
        : "No study time logged yet";

    public string QuizSummary => Totals?.AverageQuizScore is { } score
        ? $"{Totals.QuizzesPassed} passed, average best score {score}%"
        : "No quizzes taken yet";

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            var summary = await _api.GetProgressAsync();
            Totals = summary.Totals;
            Replace(Courses, summary.Courses);
            Replace(Certifications, summary.Certifications);
            Replace(RecentActivity, summary.RecentActivity.Select(ActivityItem.From));
        });
    }

    [RelayCommand]
    private void OpenCourse(CourseProgress? course)
    {
        if (course is not null)
        {
            _navigation.NavigateTo<CoursesViewModel>(vm => vm.InitialCourseSlug = course.Slug);
        }
    }

    internal static string FormatMinutes(int minutes) =>
        minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h {minutes % 60:D2} min";

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}

/// <summary>A study log entry phrased for display.</summary>
public sealed record ActivityItem(string Description, string When, string Duration)
{
    public static ActivityItem From(ActivityEntry entry)
    {
        var verb = entry.Kind switch
        {
            "theory" => "Read theory",
            "lab" => "Completed lab",
            "quiz" => "Took quiz",
            _ => entry.Kind,
        };
        var description = entry.LessonTitle is null ? verb : $"{verb}: {entry.LessonTitle}";
        var local = DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc).ToLocalTime();
        return new ActivityItem(description, local.ToString("ddd d MMM, HH:mm"), ProgressViewModel.FormatMinutes(entry.Minutes));
    }
}
