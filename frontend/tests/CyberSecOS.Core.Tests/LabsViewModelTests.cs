using System.Net;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class LabsViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly NavigationService _navigation;
    private readonly LabsViewModel _vm;

    public LabsViewModelTests()
    {
        var session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(LessonViewModel)
            ? new LessonViewModel(_api, _navigation!, session)
            : new LabsViewModel(_api, _navigation!, session));
        _vm = new LabsViewModel(_api, _navigation, session);
        _api.Labs = () =>
        [
            Summary(1, 10, "networking", "Networking", new LabStatus(2, 100, true)),
            Summary(2, 12, "networking", "Networking", new LabStatus(1, 40, false)),
            Summary(3, 30, "linux", "Linux", new LabStatus(0, null, false)),
        ];
    }

    private static LabSummary Summary(int id, int lessonId, string slug, string course, LabStatus status) =>
        new(id, $"lab-{id}", $"Lab {id}", lessonId, $"Lesson {lessonId}", slug, course, 5, 80, status);

    [Fact]
    public async Task GroupsLabsByCourseInServerOrder()
    {
        await _vm.OnNavigatedToAsync();

        Assert.Equal(["Networking", "Linux"], _vm.Courses.Select(c => c.CourseTitle));
        Assert.Equal(2, _vm.Courses[0].Labs.Count);
        Assert.Equal("1 of 3 graded labs passed, 1 in progress.", _vm.Headline);
    }

    [Fact]
    public async Task PhrasesStatusAndDetailPerLab()
    {
        await _vm.OnNavigatedToAsync();

        var passed = _vm.Courses[0].Labs[0];
        Assert.Equal("Passed", passed.StatusLabel);
        Assert.True(passed.Passed);
        Assert.Equal("5 tasks · pass mark 80% · best 100% in 2 attempts", passed.Detail);
        Assert.Equal("In progress", _vm.Courses[0].Labs[1].StatusLabel);
        Assert.Equal("5 tasks · pass mark 80% · best 40% in 1 attempt", _vm.Courses[0].Labs[1].Detail);
        Assert.Equal("Not started", _vm.Courses[1].Labs[0].StatusLabel);
        Assert.Equal("5 tasks · pass mark 80%", _vm.Courses[1].Labs[0].Detail);
    }

    [Fact]
    public void HeadlineHandlesNoLabsAndNoneStarted()
    {
        Assert.Equal("No graded labs are available yet.", LabsViewModel.BuildHeadline([]));
        Assert.Equal("0 of 1 graded labs passed.",
            LabsViewModel.BuildHeadline([Summary(1, 1, "x", "X", new LabStatus(0, null, false))]));
    }

    [Fact]
    public async Task OpeningALabGoesToItsLesson()
    {
        await _vm.OnNavigatedToAsync();

        _vm.OpenLabCommand.Execute(_vm.Courses[1].Labs[0]);

        var lesson = Assert.IsType<LessonViewModel>(_navigation.Current);
        Assert.Equal(30, lesson.LessonId);
    }

    [Fact]
    public async Task ApiErrorsSurface()
    {
        _api.Labs = () => throw FakeApi.Error(HttpStatusCode.ServiceUnavailable, "Server unavailable");

        await _vm.OnNavigatedToAsync();

        Assert.Equal("Server unavailable", _vm.ErrorMessage);
        Assert.Empty(_vm.Courses);
    }

    [Fact]
    public void LabsPageIsInTheSideBarAfterCourses()
    {
        var shell = new MainViewModel(_navigation, new SessionService(_api));
        var labels = shell.NavigationItems.Select(i => i.Label).ToList();
        Assert.Equal(labels.IndexOf("Courses") + 1, labels.IndexOf("Labs"));
    }
}
