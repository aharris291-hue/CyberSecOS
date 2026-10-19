using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class CoursesViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;

    public CoursesViewModelTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(LessonViewModel)
            ? new LessonViewModel(_api, _navigation!, _session)
            : new CoursesViewModel(_api, _navigation!, _session));
    }

    [Fact]
    public async Task LoadsCoursesAndSelectsFirst()
    {
        var vm = new CoursesViewModel(_api, _navigation, _session);

        await vm.OnNavigatedToAsync();

        Assert.Equal(2, vm.Courses.Count);
        Assert.Equal("networking", vm.SelectedCourse?.Slug);
        Assert.Equal(2, vm.Lessons.Count);
        Assert.Equal("networking-1", vm.Lessons[0].Slug);
    }

    [Fact]
    public async Task HonoursInitialCourse()
    {
        var vm = new CoursesViewModel(_api, _navigation, _session) { InitialCourseSlug = "windows" };

        await vm.OnNavigatedToAsync();

        Assert.Equal("windows", vm.SelectedCourse?.Slug);
        Assert.Equal("windows", vm.CourseDetail?.Slug);
    }

    [Fact]
    public async Task OpeningLessonNavigatesWithId()
    {
        var vm = new CoursesViewModel(_api, _navigation, _session);
        await vm.OnNavigatedToAsync();

        vm.OpenLessonCommand.Execute(vm.Lessons[1]);

        var lesson = Assert.IsType<LessonViewModel>(_navigation.Current);
        Assert.Equal(11, lesson.LessonId);
    }
}
