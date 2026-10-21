using System.Net;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class GradedLabTests
{
    private readonly FakeApi _api = new();
    private readonly LessonViewModel _vm;

    public GradedLabTests()
    {
        var session = new SessionService(_api);
        NavigationService? navigation = null;
        navigation = new NavigationService(_ => new LessonViewModel(_api, navigation!, session));
        _api.Lesson = id => Samples.Lesson(id) with { HasGradedLab = true };
        _vm = new LessonViewModel(_api, navigation, session) { LessonId = 10 };
    }

    private void AnswerAll(string ip = "1.2.3.4")
    {
        _vm.LabTasks[0].Answer = ip;
        _vm.LabTasks[1].Answer = " 42 ";
        _vm.LabTasks[2].Answer = "22,80";
        _vm.LabTasks[3].Select(1);
    }

    [Fact]
    public async Task LoadsTheLabWithItsFilesAndTasks()
    {
        await _vm.OnNavigatedToAsync();

        Assert.True(_vm.HasGradedLab);
        Assert.Contains("lab:10", _api.Calls);
        Assert.Equal(4, _vm.LabTasks.Count);
        Assert.Equal("auth.log", _vm.SelectedArtifact?.Name);
        Assert.Equal("1. Which IP?", _vm.LabTasks[0].Prompt);
        Assert.True(_vm.LabTasks[3].IsChoice);
        Assert.Equal(3, _vm.LabTasks[3].Options.Count);
        Assert.Equal("Separate items with commas; order does not matter.", _vm.LabTasks[2].AnswerHint);
        Assert.Equal("Not attempted yet. Pass mark 80%.", _vm.LabStatusText);
    }

    [Fact]
    public async Task GradedLabReplacesSelfMarking()
    {
        await _vm.OnNavigatedToAsync();

        Assert.False(_vm.CanSelfMarkLab);
        Assert.True(_vm.IsGradedLabPending);
        Assert.Equal("To finish this lesson: pass the graded lab and pass the quiz.", _vm.ProgressChecklist);
    }

    [Fact]
    public async Task LessonsWithoutAGradedLabKeepSelfMarkingAndSkipTheLabCall()
    {
        _api.Lesson = id => Samples.Lesson(id);

        await _vm.OnNavigatedToAsync();

        Assert.False(_vm.HasGradedLab);
        Assert.True(_vm.CanSelfMarkLab);
        Assert.DoesNotContain("lab:10", _api.Calls);
        Assert.Empty(_vm.LabTasks);
    }

    [Fact]
    public async Task SubmissionNeedsEveryTaskAnswered()
    {
        await _vm.OnNavigatedToAsync();
        _vm.LabTasks[0].Answer = "1.2.3.4";

        await _vm.SubmitLabCommand.ExecuteAsync(null);

        Assert.Equal("Answer every lab task before submitting.", _vm.ErrorMessage);
        Assert.DoesNotContain(_api.Calls, c => c is LabSubmission);
    }

    [Fact]
    public async Task SubmitsTrimmedAnswersAndChoiceIndexInTaskOrder()
    {
        await _vm.OnNavigatedToAsync();
        AnswerAll();

        await _vm.SubmitLabCommand.ExecuteAsync(null);

        var submission = Assert.Single(_api.Calls.OfType<LabSubmission>());
        Assert.Equal(["1.2.3.4", "42", "22,80", "1"], submission.Answers);
    }

    [Fact]
    public async Task PassingShowsFeedbackRefreshesProgressAndStatus()
    {
        await _vm.OnNavigatedToAsync();
        AnswerAll();
        _api.Lesson = id => Samples.Lesson(id, Samples.Progress(LessonStatuses.InProgress, lab: true)) with { HasGradedLab = true };

        await _vm.SubmitLabCommand.ExecuteAsync(null);

        Assert.Equal("100% (4/4) - passed, lab complete", _vm.LabResultSummary);
        Assert.All(_vm.LabTasks, t => Assert.True(t.IsCorrect));
        Assert.Equal("Correct. Why 0", _vm.LabTasks[0].Feedback);
        Assert.True(_vm.IsLabCompleted);
        Assert.False(_vm.IsGradedLabPending);
        Assert.Equal("Lab passed.", _vm.StatusMessage);
        Assert.Equal("Best score 100% after 1 attempt - passed.", _vm.LabStatusText);
    }

    [Fact]
    public async Task FailingShowsHintsAndKeepsAnswersForAnotherTry()
    {
        await _vm.OnNavigatedToAsync();
        AnswerAll(ip: "9.9.9.9");
        _vm.LabTasks[1].Answer = "41";

        await _vm.SubmitLabCommand.ExecuteAsync(null);

        Assert.Equal("50% (2/4) - pass mark is 80%, use the hints and try again", _vm.LabResultSummary);
        Assert.Equal("Not quite. Hint 0", _vm.LabTasks[0].Feedback);
        Assert.False(_vm.LabTasks[0].IsCorrect);
        Assert.Equal("9.9.9.9", _vm.LabTasks[0].Answer);
        Assert.Equal("Best score 50% after 1 attempt. Pass mark 80%.", _vm.LabStatusText);
    }

    [Fact]
    public async Task ServerErrorsSurfaceOnThePage()
    {
        await _vm.OnNavigatedToAsync();
        AnswerAll();
        _api.LabSubmit = (_, _) => throw FakeApi.Error(HttpStatusCode.UnprocessableEntity, "Expected 5 answers, got 4");

        await _vm.SubmitLabCommand.ExecuteAsync(null);

        Assert.Equal("Expected 5 answers, got 4", _vm.ErrorMessage);
        Assert.Null(_vm.LabResult);
    }

    [Fact]
    public async Task ApiClientPostsAnswersWithNullsAndParsesTheResult()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """
            {"score":50,"correct":1,"total":2,"passed":false,"pass_mark":80,
             "results":[{"index":0,"is_correct":true,"feedback":"why"},{"index":1,"is_correct":false,"feedback":"hint"}],
             "lab_completed":false,"lesson_status":"in_progress"}
            """);
        var api = new CyberSecApiClient(handler.CreateClient());

        var result = await api.SubmitLabAsync(7, new LabSubmission(["10.0.0.1", null]));

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("/api/lessons/7/lab/attempts", request.RequestUri!.AbsolutePath);
        Assert.Equal("""{"answers":["10.0.0.1",null]}""", body);
        Assert.False(result.LabCompleted);
        Assert.Equal("hint", result.Results[1].Feedback);
    }

    [Fact]
    public async Task ApiClientReadsTheLabAndTheLessonFlag()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """
            {"id":1,"slug":"lab-x","title":"T","lesson_id":7,"scenario":"S","pass_mark":80,
             "artifacts":[{"name":"auth.log","kind":"log","description":"d","content":"c"}],
             "tasks":[{"index":0,"prompt":"P","type":"choice","options":["a","b","c"]}],
             "status":{"attempts":2,"best_score":60,"passed":false}}
            """);
        var api = new CyberSecApiClient(handler.CreateClient());

        var lab = await api.GetLabAsync(7);

        Assert.Equal("/api/lessons/7/lab", Assert.Single(handler.Requests).Request.RequestUri!.AbsolutePath);
        Assert.Equal("auth.log", lab.Artifacts[0].Name);
        Assert.Equal(LabTaskTypes.Choice, lab.Tasks[0].Type);
        Assert.Equal(60, lab.Status.BestScore);
    }
}
