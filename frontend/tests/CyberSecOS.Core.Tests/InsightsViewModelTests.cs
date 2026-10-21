using System.Net;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class InsightsViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;

    public InsightsViewModelTests()
    {
        _session = new SessionService(_api);
    }

    private InsightsViewModel Create() => new(_api, _session);

    [Fact]
    public async Task LoadsTrendPatternsAndWeakSpots()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.True(vm.HasData);
        Assert.Equal(3, vm.TrendChart.Labels.Count);
        Assert.Equal(7, vm.WeekdayChart.Labels.Count);
        Assert.Equal(2, vm.WeakestObjectives.Count);
        Assert.Equal(2, vm.WeakestDomains.Count);
    }

    [Fact]
    public async Task HeadlineReflectsRecentPace()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        // Sample completes one lesson a week in the last four weeks.
        Assert.Contains("about 1 lesson a week", vm.Headline);
    }

    [Fact]
    public async Task StalledLearnerIsNudged()
    {
        _api.Analytics = _ => Samples.Analytics() with
        {
            Velocity = new LearningVelocity(72, 8, 64, CompletedLast4Weeks: 0, 0.0, null, null),
        };
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Contains("momentum", vm.Headline);
        Assert.Contains("complete a few lessons to project a finish date", vm.VelocitySummary);
    }

    [Fact]
    public async Task VelocitySummaryProjectsFinish()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Contains("8/72 lessons done", vm.VelocitySummary);
        Assert.Contains("64 to go", vm.VelocitySummary);
        Assert.Contains("Jan 2028", vm.VelocitySummary);
    }

    [Fact]
    public async Task QuizSummaryShowsAverageAndTrend()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.True(vm.HasQuizData);
        Assert.Contains("72% average over 6 attempts", vm.QuizSummary);
        Assert.Contains("up 15 points", vm.QuizSummary);
    }

    [Fact]
    public async Task NoQuizDataReadsCleanly()
    {
        _api.Analytics = _ => Samples.Analytics() with
        {
            QuizPerformance = new QuizPerformance(0, null, null, [], []),
        };
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.False(vm.HasQuizData);
        Assert.Equal("No quiz attempts yet.", vm.QuizSummary);
        Assert.Empty(vm.WeakestObjectives);
    }

    [Fact]
    public async Task PeakStudyTimeIsPhrased()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Equal("You study most in the evening.", vm.PeakStudyTime);
    }

    [Fact]
    public async Task ApiErrorSurfacesMessage()
    {
        _api.Analytics = _ => throw FakeApi.Error(HttpStatusCode.InternalServerError, "boom");
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.False(vm.HasData);
        Assert.Equal("boom", vm.ErrorMessage);
    }

    [Fact]
    public async Task RequestsEightWeeksByDefault()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Contains("analytics:8", _api.Calls);
    }
}
