using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>
/// Every graded lab across the catalog, grouped by course, with the learner's
/// best score and pass state, so they can see which hands-on skills are proven.
/// </summary>
public sealed partial class LabsViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public LabsViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    public ObservableCollection<LabCourseGroup> Courses { get; } = [];

    [ObservableProperty]
    private string _headline = string.Empty;

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            var labs = await _api.GetLabsAsync();
            Courses.Clear();
            foreach (var group in labs.GroupBy(l => (l.CourseSlug, l.CourseTitle)))
            {
                Courses.Add(new LabCourseGroup(group.Key.CourseTitle, group.Select(LabItem.From).ToList()));
            }
            Headline = BuildHeadline(labs);
        });
    }

    internal static string BuildHeadline(IReadOnlyList<LabSummary> labs)
    {
        if (labs.Count == 0)
        {
            return "No graded labs are available yet.";
        }
        var passed = labs.Count(l => l.Status.Passed);
        var started = labs.Count(l => l.Status.Attempts > 0 && !l.Status.Passed);
        var text = $"{passed} of {labs.Count} graded labs passed";
        return started > 0 ? $"{text}, {started} in progress." : $"{text}.";
    }

    [RelayCommand]
    private void OpenLab(LabItem? lab)
    {
        if (lab is not null)
        {
            _navigation.NavigateTo<LessonViewModel>(vm => vm.LessonId = lab.LessonId);
        }
    }
}

public sealed record LabCourseGroup(string CourseTitle, IReadOnlyList<LabItem> Labs);

/// <summary>One lab phrased for the list: status badge and a one-line detail.</summary>
public sealed record LabItem(int LessonId, string Title, string LessonTitle, string StatusLabel, string Detail, bool Passed)
{
    public static LabItem From(LabSummary lab)
    {
        var status = lab.Status;
        var label = status.Passed ? "Passed" : status.Attempts > 0 ? "In progress" : "Not started";
        var tasks = $"{lab.TaskCount} tasks · pass mark {lab.PassMark}%";
        var detail = status.Attempts == 0
            ? tasks
            : $"{tasks} · best {status.BestScore}% in {status.Attempts} attempt{(status.Attempts == 1 ? "" : "s")}";
        return new LabItem(lab.LessonId, lab.Title, lab.LessonTitle, label, detail, status.Passed);
    }
}
