using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class PlannerViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;

    public PlannerViewModelTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type =>
            type == typeof(LessonViewModel) ? new LessonViewModel(_api, _navigation!, _session)
            : type == typeof(ProgressViewModel) ? new ProgressViewModel(_api, _navigation!, _session)
            : new PlannerViewModel(_api, _navigation!, _session));
    }

    private PlannerViewModel Create() => new(_api, _navigation, _session);

    [Fact]
    public async Task LoadsPaceAndSchedule()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.True(vm.HasPlan);
        Assert.Equal(2, vm.Certifications.Count);
        Assert.Single(vm.Weeks);
        Assert.Equal(3, vm.Weeks[0].Lessons.Count);
    }

    [Fact]
    public async Task HeadlineReflectsOnTrack()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Contains("on track", vm.Headline);
        Assert.Contains("3 lessons left", vm.Summary);
        Assert.Contains("10 h/week", vm.Summary);
    }

    [Fact]
    public async Task HeadlineWarnsWhenBehind()
    {
        _api.StudyPlan = () => Samples.StudyPlan() with { OnTrack = false };
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Contains("miss at least one exam", vm.Headline);
    }

    [Fact]
    public async Task HeadlineCelebratesWhenComplete()
    {
        _api.StudyPlan = () => Samples.StudyPlan() with
        {
            TotalRemainingLessons = 0,
            TotalRemainingMinutes = 0,
            Schedule = [],
        };
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.True(vm.IsComplete);
        Assert.Contains("completed every lesson", vm.Headline);
    }

    [Fact]
    public async Task PaceItemDescribesCountdownAndRequiredPace()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();
        var secplus = vm.Certifications.First(c => c.Id == "secplus");

        Assert.Equal("On track", secplus.StatusLabel);
        Assert.Contains("exam in 42 days", secplus.Detail);
        Assert.Contains("/week", secplus.Detail);
    }

    [Fact]
    public async Task PaceItemWithoutDatePromptsForOne()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();
        var sc900 = vm.Certifications.First(c => c.Id == "sc900");

        Assert.Equal("No date set", sc900.StatusLabel);
        Assert.Contains("Set an exam date", sc900.Detail);
    }

    [Fact]
    public async Task WeekItemSummarisesLoad()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.StartsWith("Week 1 ·", vm.Weeks[0].Title);
        Assert.Contains("3 lessons", vm.Weeks[0].Subtitle);
        Assert.Contains("3 h", vm.Weeks[0].Subtitle); // 195 minutes
    }

    [Fact]
    public async Task OpenLessonNavigatesToTheLesson()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        var lesson = vm.Weeks[0].Lessons[1];

        vm.OpenLessonCommand.Execute(lesson);

        var opened = Assert.IsType<LessonViewModel>(_navigation.Current);
        Assert.Equal(lesson.Id, opened.LessonId);
    }

    [Fact]
    public async Task EditExamDatesNavigatesToProgress()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();

        vm.EditExamDatesCommand.Execute(null);

        Assert.IsType<ProgressViewModel>(_navigation.Current);
    }

    [Fact]
    public async Task ApiErrorIsSurfaced()
    {
        _api.StudyPlan = () => throw FakeApi.Error(System.Net.HttpStatusCode.ServiceUnavailable, "Server unavailable");
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.False(vm.HasPlan);
        Assert.Equal("Server unavailable", vm.ErrorMessage);
    }
}
