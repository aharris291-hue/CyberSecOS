using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>
/// Study planner: turns the weekly goal, exam dates and remaining work into a
/// pace check per certification and a week-by-week schedule the learner can follow.
/// </summary>
public sealed partial class PlannerViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public PlannerViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Headline), nameof(Summary), nameof(HasPlan), nameof(IsComplete))]
    private StudyPlan? _plan;

    public ObservableCollection<CertificationPaceItem> Certifications { get; } = [];

    public ObservableCollection<PlanWeekItem> Weeks { get; } = [];

    public bool HasPlan => Plan is not null;

    /// <summary>True once every lesson is done and nothing is left to schedule.</summary>
    public bool IsComplete => Plan is { TotalRemainingLessons: 0 };

    public string Headline => Plan switch
    {
        null => string.Empty,
        { TotalRemainingLessons: 0 } => "You've completed every lesson. Keep reviewing and sitting practice exams.",
        { OnTrack: true } => "You're on track to be ready for every scheduled exam.",
        { OnTrack: false } => "At your current pace you'll miss at least one exam date. Raise your weekly goal or move a date.",
        _ => "Set a target exam date on a certification to see whether your pace gets you there in time.",
    };

    public string Summary
    {
        get
        {
            if (Plan is not { } plan)
            {
                return string.Empty;
            }

            if (plan.TotalRemainingLessons == 0)
            {
                return $"0 lessons left · {plan.WeeklyGoalHours} h/week goal";
            }

            var work = $"{plan.TotalRemainingLessons} lessons left ({FormatHours(plan.TotalRemainingMinutes)})";
            var pace = $"{plan.WeeklyGoalHours} h/week goal";
            var finish = plan.EstimatedWeeksToFinish > 0
                ? $"about {plan.EstimatedWeeksToFinish} weeks at this pace"
                : null;
            return finish is null ? $"{work} · {pace}" : $"{work} · {pace} · {finish}";
        }
    }

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            var plan = await _api.GetStudyPlanAsync();
            Plan = plan;
            Replace(Certifications, plan.Certifications.Select(CertificationPaceItem.From));
            Replace(Weeks, plan.Schedule.Select(PlanWeekItem.From));
        });
    }

    [RelayCommand]
    private void OpenLesson(PlannedLesson? lesson)
    {
        if (lesson is not null)
        {
            _navigation.NavigateTo<LessonViewModel>(vm => vm.LessonId = lesson.Id);
        }
    }

    [RelayCommand]
    private void EditExamDates() => _navigation.NavigateTo<ProgressViewModel>();

    internal static string FormatHours(int minutes) =>
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

/// <summary>One certification's pace, phrased for display.</summary>
public sealed record CertificationPaceItem(
    string Id,
    string Name,
    string ExamCode,
    string StatusLabel,
    string Detail)
{
    public static CertificationPaceItem From(CertificationPace pace)
    {
        var label = pace.Status switch
        {
            "ready" => "Ready",
            "on_track" => "On track",
            "behind" => "Behind",
            "overdue" => "Overdue",
            _ => "No date set",
        };
        return new CertificationPaceItem(pace.Id, pace.Name, pace.ExamCode, label, Describe(pace));
    }

    private static string Describe(CertificationPace pace)
    {
        if (pace.RemainingLessons == 0)
        {
            return "All objectives covered by completed lessons.";
        }

        var work = $"{pace.RemainingLessons} lessons left ({PlannerViewModel.FormatHours(pace.RemainingMinutes)})";
        if (pace.TargetDate is not { } exam)
        {
            return $"{work}. Set an exam date to pace this.";
        }

        var when = pace.DaysUntilExam switch
        {
            < 0 and var d => $"exam was {-d} days ago",
            0 => "exam is today",
            > 0 and var d => $"exam in {d} days",
            null => "date set",
        };
        var need = pace.RequiredMinutesPerWeek is { } required
            ? $" — needs {PlannerViewModel.FormatHours(required)}/week"
            : string.Empty;
        return $"{work}, {exam:d MMM yyyy} ({when}){need}.";
    }
}

/// <summary>One scheduled week, phrased for display.</summary>
public sealed record PlanWeekItem(string Title, string Subtitle, IReadOnlyList<PlannedLesson> Lessons)
{
    public static PlanWeekItem From(PlanWeek week)
    {
        var title = DateOnly.TryParse(week.WeekStart, out var start)
            ? $"Week {week.WeekIndex} · {start:d MMM}"
            : $"Week {week.WeekIndex}";
        var subtitle = $"{week.Lessons.Count} lessons · {PlannerViewModel.FormatHours(week.PlannedMinutes)}";
        return new PlanWeekItem(title, subtitle, week.Lessons);
    }
}
