using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

/// <summary>Course catalog: pick a course on the left, see its lessons on the right.</summary>
public sealed partial class CoursesViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public CoursesViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    public ObservableCollection<CourseSummary> Courses { get; } = [];

    public ObservableCollection<LessonSummary> Lessons { get; } = [];

    [ObservableProperty]
    private CourseSummary? _selectedCourse;

    [ObservableProperty]
    private CourseDetail? _courseDetail;

    /// <summary>Set before navigating here to preselect a course.</summary>
    public string? InitialCourseSlug { get; set; }

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            Courses.Clear();
            foreach (var course in await _api.GetCoursesAsync())
            {
                Courses.Add(course);
            }
        });
        SelectedCourse = Courses.FirstOrDefault(c => c.Slug == InitialCourseSlug) ?? Courses.FirstOrDefault();
    }

    partial void OnSelectedCourseChanged(CourseSummary? value)
    {
        if (value is not null)
        {
            _ = LoadCourseAsync(value.Slug);
        }
    }

    internal async Task LoadCourseAsync(string slug)
    {
        await RunAsync(async () =>
        {
            var detail = await _api.GetCourseAsync(slug);
            CourseDetail = detail;
            Lessons.Clear();
            foreach (var lesson in detail.Lessons)
            {
                Lessons.Add(lesson);
            }
        });
    }

    [RelayCommand]
    private void OpenLesson(LessonSummary? lesson)
    {
        if (lesson is not null)
        {
            _navigation.NavigateTo<LessonViewModel>(vm => vm.LessonId = lesson.Id);
        }
    }
}
