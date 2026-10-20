using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class ExamsViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;
    private DateTime _now = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    public ExamsViewModelTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(CoursesViewModel)
            ? new CoursesViewModel(_api, _navigation!, _session)
            : new ExamsViewModel(_api, _navigation!, _session));
        _api.Certifications = () =>
        [
            Samples.Cert("secplus", "CompTIA Security+", "SY0-701", 1),
            Samples.Cert("sc200", "Microsoft SC-200", "SC-200", 3),
        ];
    }

    private ExamsViewModel Create() => new(_api, _navigation, _session) { Now = () => _now };

    private static void Answer(QuizQuestionViewModel q, int index) => q.Options[index].IsSelected = true;

    [Fact]
    public async Task LoadsCertificationsAndSelectsFirstWithQuestions()
    {
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.Equal(2, vm.Options.Count);
        Assert.Equal("secplus", vm.SelectedExam?.Info.CertificationId);
        Assert.Equal(ExamStage.Setup, vm.Stage);
        Assert.True(vm.CanStart);
    }

    [Fact]
    public async Task ShowsBestAttemptFromHistory()
    {
        _api.ExamHistory = cert => cert == "secplus"
            ?
            [
                Samples.ExamAttempt(1, "secplus", 72, false),
                Samples.ExamAttempt(2, "secplus", 90, true),
            ]
            : [];
        var vm = Create();

        await vm.OnNavigatedToAsync();

        var secplus = vm.Options.First(o => o.Info.CertificationId == "secplus");
        Assert.Equal(90, secplus.BestAttempt?.Score);
        Assert.Contains("90%", secplus.BestSummary);
    }

    [Fact]
    public async Task EffectiveCountCapsAtAvailableQuestions()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();

        vm.QuestionCount = 100; // secplus has 40 available

        Assert.Equal(40, vm.EffectiveCount);
        Assert.Contains("40 questions", vm.LengthCaption);
    }

    [Fact]
    public async Task StartExamLoadsPaperAndBeginsTimer()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 10;

        await vm.StartExamCommand.ExecuteAsync(null);

        Assert.Equal(ExamStage.Taking, vm.Stage);
        Assert.Equal(10, vm.Questions.Count);
        Assert.Equal("00:00", vm.ElapsedDisplay);
        Assert.Equal(0, vm.AnsweredCount);
    }

    [Fact]
    public async Task UpdateElapsedFormatsRunningTime()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        await vm.StartExamCommand.ExecuteAsync(null);

        _now = _now.AddSeconds(95);
        vm.UpdateElapsed();

        Assert.Equal("01:35", vm.ElapsedDisplay);
    }

    [Fact]
    public async Task AnsweringQuestionsUpdatesProgress()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 5;
        await vm.StartExamCommand.ExecuteAsync(null);

        Answer(vm.Questions[0], 0);
        Answer(vm.Questions[1], 2);

        Assert.Equal(2, vm.AnsweredCount);
        Assert.Contains("2 of 5", vm.ProgressCaption);
    }

    [Fact]
    public async Task SubmitGradesAndShowsResultWithWeakAreas()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 5;
        await vm.StartExamCommand.ExecuteAsync(null);
        foreach (var q in vm.Questions)
        {
            Answer(q, 1); // wrong (Samples grades index 0 correct) -> 0%
        }
        _now = _now.AddSeconds(300);

        await vm.SubmitExamCommand.ExecuteAsync(null);

        Assert.Equal(ExamStage.Results, vm.Stage);
        Assert.Equal(0, vm.Result?.Score);
        Assert.False(vm.Result?.Passed);
        Assert.NotEmpty(vm.WeakAreas);
        Assert.Contains("Not yet", vm.ResultHeadline);
    }

    [Fact]
    public async Task SubmitSendsSelectedAnswersAndDuration()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 5;
        await vm.StartExamCommand.ExecuteAsync(null);
        Answer(vm.Questions[0], 0);
        _now = _now.AddSeconds(42);

        await vm.SubmitExamCommand.ExecuteAsync(null);

        var submission = Assert.IsType<ExamSubmission>(_api.Calls.Last());
        Assert.Equal(5, submission.Answers.Count);
        Assert.Equal(42, submission.DurationSeconds);
        Assert.Equal(0, submission.Answers[0].SelectedIndex);
        Assert.Null(submission.Answers[1].SelectedIndex);
    }

    [Fact]
    public async Task SubmitWithUnansweredWarnsButStillSubmits()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 5;
        await vm.StartExamCommand.ExecuteAsync(null);
        Answer(vm.Questions[0], 0);

        await vm.SubmitExamCommand.ExecuteAsync(null);

        Assert.Equal(ExamStage.Results, vm.Stage);
    }

    [Fact]
    public async Task PassingExamShowsPassedHeadline()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        vm.QuestionCount = 5;
        await vm.StartExamCommand.ExecuteAsync(null);
        foreach (var q in vm.Questions)
        {
            Answer(q, 0); // all correct -> 100%
        }

        await vm.SubmitExamCommand.ExecuteAsync(null);

        Assert.True(vm.Result?.Passed);
        Assert.Empty(vm.WeakAreas);
        Assert.Contains("Passed", vm.ResultHeadline);
    }

    [Fact]
    public async Task BackToSetupReloadsOptions()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();
        await vm.StartExamCommand.ExecuteAsync(null);

        vm.BackToSetupCommand.Execute(null);

        Assert.Equal(ExamStage.Setup, vm.Stage);
        Assert.Equal(2, vm.Options.Count);
    }

    [Fact]
    public async Task BrowseCoursesNavigatesToCourses()
    {
        var vm = Create();
        await vm.OnNavigatedToAsync();

        vm.BrowseCoursesCommand.Execute(null);

        Assert.IsType<CoursesViewModel>(_navigation.Current);
    }

    [Fact]
    public async Task CannotStartWhenNoQuestionsAvailable()
    {
        _api.Certifications = () => [Samples.Cert("empty", "Empty Cert", "EX-000", 9)];
        _api.ExamInfo = _ => new ExamInfo("empty", "Empty Cert", "EX-000", 0, 20, 80);
        var vm = Create();

        await vm.OnNavigatedToAsync();

        Assert.False(vm.CanStart);
        Assert.False(vm.StartExamCommand.CanExecute(null));
    }
}
