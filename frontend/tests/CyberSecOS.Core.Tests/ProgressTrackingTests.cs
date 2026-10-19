using System.Net;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class ProgressTrackingTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;

    public ProgressTrackingTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(CoursesViewModel)
            ? new CoursesViewModel(_api, _navigation!, _session)
            : new LessonViewModel(_api, _navigation!, _session));
    }

    private async Task<LessonViewModel> LoadedLesson(LessonProgress? progress = null)
    {
        _api.Lesson = id => Samples.Lesson(id, progress);
        var vm = new LessonViewModel(_api, _navigation, _session) { LessonId = 10 };
        await vm.OnNavigatedToAsync();
        return vm;
    }

    [Fact]
    public async Task ChecklistListsWhatIsLeft()
    {
        var vm = await LoadedLesson();

        Assert.Equal("To finish this lesson: complete the practical exercise and pass the quiz.", vm.ProgressChecklist);
    }

    [Fact]
    public async Task ChecklistAfterLabOnlyMentionsQuiz()
    {
        var vm = await LoadedLesson(Samples.Progress(LessonStatuses.InProgress, lab: true));

        Assert.Equal("To finish this lesson: pass the quiz.", vm.ProgressChecklist);
        Assert.True(vm.IsLabCompleted);
    }

    [Fact]
    public async Task MarkTheoryReadUpdatesProgress()
    {
        var vm = await LoadedLesson();

        await vm.MarkTheoryReadCommand.ExecuteAsync(null);

        Assert.Contains("theory:10", _api.Calls);
        Assert.True(vm.IsTheoryCompleted);
    }

    [Fact]
    public async Task CompleteLabSendsNotesAndMinutes()
    {
        var vm = await LoadedLesson();
        vm.LabNotes = "Found the handshake";
        vm.LabMinutes = "75";

        await vm.CompleteLabCommand.ExecuteAsync(null);

        var request = Assert.IsType<LabRequest>(_api.Calls.Single(c => c is LabRequest));
        Assert.Equal("Found the handshake", request.Notes);
        Assert.Equal(75, request.Minutes);
        Assert.True(vm.IsLabCompleted);
        Assert.Equal("Lab recorded.", vm.StatusMessage);
    }

    [Fact]
    public async Task CompleteLabUsesEstimateWhenMinutesBlank()
    {
        var vm = await LoadedLesson();

        await vm.CompleteLabCommand.ExecuteAsync(null);

        var request = Assert.IsType<LabRequest>(_api.Calls.Single(c => c is LabRequest));
        Assert.Null(request.Minutes);
        Assert.Null(request.Notes);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("601")]
    public async Task CompleteLabRejectsBadMinutes(string minutes)
    {
        var vm = await LoadedLesson();
        vm.LabMinutes = minutes;

        await vm.CompleteLabCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_api.Calls, c => c is LabRequest);
        Assert.Contains("between 1 and 600", vm.ErrorMessage);
    }

    [Fact]
    public async Task CompletingFinalStepCelebrates()
    {
        _api.Lab = (_, _) => Samples.Progress(LessonStatuses.Completed, lab: true, quiz: true);
        var vm = await LoadedLesson(Samples.Progress(LessonStatuses.InProgress, quiz: true));

        await vm.CompleteLabCommand.ExecuteAsync(null);

        Assert.Equal("Lesson complete. Nice work.", vm.StatusMessage);
        Assert.Equal("Lesson complete.", vm.ProgressChecklist);
    }

    [Fact]
    public async Task ReopenLab()
    {
        var vm = await LoadedLesson(Samples.Progress(LessonStatuses.InProgress, lab: true));

        await vm.ReopenLabCommand.ExecuteAsync(null);

        Assert.False(vm.IsLabCompleted);
    }

    [Fact]
    public async Task ExistingLabNotesArePrefilled()
    {
        var vm = await LoadedLesson(Samples.Progress(LessonStatuses.InProgress, lab: true) with { LabNotes = "my notes" });

        Assert.Equal("my notes", vm.LabNotes);
    }

    [Fact]
    public async Task ProgressPageLoadsSummary()
    {
        var vm = new ProgressViewModel(_api, _navigation, _session);

        await vm.OnNavigatedToAsync();

        Assert.Equal(2, vm.Courses.Count);
        Assert.Equal(2, vm.Certifications.Count);
        Assert.Equal("3 h 28 min", vm.StudyTime);
        Assert.Equal("77% hands-on (target 80%)", vm.HandsOnSummary);
        Assert.Equal("3 passed, average best score 88%", vm.QuizSummary);
        Assert.StartsWith("Completed lab: The TCP Handshake", vm.RecentActivity[0].Description);
        Assert.Equal("48 min", vm.RecentActivity[0].Duration);
    }

    [Fact]
    public async Task ProgressPageWithNoActivity()
    {
        _api.Progress = () => Samples.Summary() with
        {
            Totals = new ProgressTotals(12, 0, 0, 0, 0, 0, 0, null, 0, 0, 0, 0, null, 0, 0),
            RecentActivity = [],
        };
        var vm = new ProgressViewModel(_api, _navigation, _session);

        await vm.OnNavigatedToAsync();

        Assert.Equal("No study time logged yet", vm.HandsOnSummary);
        Assert.Equal("No quizzes taken yet", vm.QuizSummary);
        Assert.Empty(vm.RecentActivity);
    }

    [Fact]
    public async Task OpenCourseFromProgressPage()
    {
        var vm = new ProgressViewModel(_api, _navigation, _session);
        await vm.OnNavigatedToAsync();

        vm.OpenCourseCommand.Execute(vm.Courses[1]);

        Assert.Equal("windows", Assert.IsType<CoursesViewModel>(_navigation.Current).InitialCourseSlug);
    }

    [Fact]
    public async Task ProgressRequestCarriesTimezoneOffset()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "[]");
        var api = new CyberSecApiClient(handler.CreateClient());

        await api.GetDailyActivityAsync(14);

        var query = handler.Requests[0].Request.RequestUri!.Query;
        Assert.Contains("days=14", query);
        Assert.Contains($"tz_offset_minutes={CyberSecApiClient.LocalOffsetMinutes()}", query);
    }
}
