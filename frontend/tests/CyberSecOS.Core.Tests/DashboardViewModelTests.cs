using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class DashboardViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;

    public DashboardViewModelTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(CoursesViewModel)
            ? new CoursesViewModel(_api, _navigation!, _session)
            : new LessonViewModel(_api, _navigation!, _session));
    }

    private async Task<DashboardViewModel> Loaded(DateTime? now = null)
    {
        var vm = new DashboardViewModel(_api, _navigation, _session) { Now = () => now ?? new DateTime(2026, 10, 19, 20, 0, 0) };
        await vm.OnNavigatedToAsync();
        return vm;
    }

    [Theory]
    [InlineData(8, "Good morning")]
    [InlineData(13, "Good afternoon")]
    [InlineData(21, "Good evening")]
    public void GreetingFollowsTimeOfDay(int hour, string expected)
    {
        Assert.Equal(expected, DashboardViewModel.GreetingFor(new DateTime(2026, 10, 19, hour, 0, 0)));
    }

    [Fact]
    public async Task HeaderAndTiles()
    {
        var vm = await Loaded();

        Assert.Equal("Good evening, Adam", vm.Greeting);
        Assert.Equal("Your path to SOC Analyst: 3 of 12 lessons complete", vm.Subtitle);
        Assert.Equal(["Course completion", "Study streak", "This week", "Labs completed", "Quiz average"], vm.Tiles.Select(t => t.Label));
        Assert.Equal("25%", vm.Tiles[0].Value);
        Assert.Equal("4 days", vm.Tiles[1].Value);
        Assert.Equal("Best: 6 days", vm.Tiles[1].Caption);
        Assert.Equal("2.5 h", vm.Tiles[2].Value);
        Assert.Equal("77% hands-on (target 80%)", vm.Tiles[3].Caption);
        Assert.Equal("88%", vm.Tiles[4].Value);
        Assert.Equal("2.5 h of 10 h this week", vm.WeekSummary);
        Assert.Equal(25, vm.WeekGoalPercent);
        Assert.True(vm.HasStudied);
    }

    [Fact]
    public async Task ChartsCarryDashboardData()
    {
        var vm = await Loaded();

        Assert.Equal(14, vm.ActivityChart.Labels.Count);
        Assert.Equal("1 Oct", vm.ActivityChart.Labels[0]);
        Assert.Equal(["Theory", "Labs", "Quizzes"], vm.ActivityChart.Series.Select(s => s.Name));
        Assert.Equal(4.0 * 13, vm.ActivityChart.Series[1].Values[13]);

        Assert.Equal(["Networking", "Windows Security"], vm.DomainChart.Labels);
        Assert.Equal([67.0, 50.0], vm.DomainChart.Series[0].Values);
        Assert.Equal(100, vm.DomainChart.MaxValue);

        Assert.Equal(["SY0-701", "SC-900"], vm.CertificationChart.Labels);
        Assert.Equal([36.0], vm.StudyMixChart.Series[0].Values);
        Assert.Equal([160.0], vm.StudyMixChart.Series[1].Values);
    }

    [Fact]
    public async Task NextLessonCard()
    {
        var vm = await Loaded();

        Assert.True(vm.HasNextLesson);
        Assert.Equal("Start lesson", vm.NextLessonActionLabel);
        Assert.Equal("Networking Foundations  ·  10 min theory, 50 min lab", vm.NextLessonCaption);

        vm.ContinueLearningCommand.Execute(null);

        Assert.Equal(11, Assert.IsType<LessonViewModel>(_navigation.Current).LessonId);
    }

    [Fact]
    public async Task InProgressLessonSaysContinue()
    {
        _api.Dashboard = () => Samples.Dashboard() with
        {
            NextLesson = new NextLesson(5, "Sysmon", "Windows", LessonStatuses.InProgress, 12, 55),
        };

        var vm = await Loaded();

        Assert.Equal("Continue lesson", vm.NextLessonActionLabel);
    }

    [Fact]
    public async Task AllLessonsDone()
    {
        _api.Dashboard = () => Samples.Dashboard() with { NextLesson = null };

        var vm = await Loaded();

        Assert.False(vm.HasNextLesson);
        Assert.False(vm.ContinueLearningCommand.CanExecute(null));
        Assert.Contains("completed every lesson", vm.NextLessonCaption);
    }

    [Fact]
    public async Task NewLearnerHasEmptyState()
    {
        _api.Dashboard = () => Samples.Dashboard() with
        {
            Totals = new ProgressTotals(12, 0, 0, 0, 0, 0, 0, null, 0, 0, 0, 0, null, 0, 0),
            Week = new WeekProgress("2026-10-19", 0, 600, 0),
            DailyActivity = Samples.Daily(14).Select(d => d with { TheoryMinutes = 0, LabMinutes = 0, TotalMinutes = 0 }).ToList(),
            RecentActivity = [],
        };

        var vm = await Loaded();

        Assert.False(vm.HasStudied);
        Assert.True(vm.ActivityChart.IsEmpty);
        Assert.Equal("-", vm.Tiles[4].Value);
        Assert.Equal("Target: 80% hands-on", vm.Tiles[3].Caption);
        Assert.Equal("0 days", vm.Tiles[1].Value);
    }

    [Fact]
    public async Task BrowseCoursesNavigates()
    {
        var vm = await Loaded();

        vm.BrowseCoursesCommand.Execute(null);

        Assert.IsType<CoursesViewModel>(_navigation.Current);
    }

    [Theory]
    [InlineData(0, "0 h")]
    [InlineData(150, "2.5 h")]
    [InlineData(600, "10 h")]
    [InlineData(100, "1.7 h")]
    public void FormatsHours(int minutes, string expected)
    {
        Assert.Equal(expected, DashboardViewModel.FormatHours(minutes));
    }
}
