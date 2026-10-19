using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;
using CyberSecOS.Core.Tests.Fakes;
using CyberSecOS.Core.ViewModels;

namespace CyberSecOS.Core.Tests;

public class LessonViewModelTests
{
    private readonly FakeApi _api = new();
    private readonly SessionService _session;
    private readonly NavigationService _navigation;
    private readonly LessonViewModel _vm;

    public LessonViewModelTests()
    {
        _session = new SessionService(_api);
        _navigation = new NavigationService(type => type == typeof(LessonViewModel)
            ? new LessonViewModel(_api, _navigation!, _session)
            : new CoursesViewModel(_api, _navigation!, _session));
        _vm = new LessonViewModel(_api, _navigation, _session) { LessonId = 10 };
    }

    [Fact]
    public async Task LoadsLessonSectionsAndQuiz()
    {
        await _vm.OnNavigatedToAsync();

        Assert.Equal("The TCP Handshake", _vm.Lesson?.Title);
        Assert.Equal(2, _vm.Questions.Count);
        Assert.Equal("1. Q1", _vm.Questions[0].Prompt);
        Assert.Equal("20% theory / 80% hands-on", _vm.TheoryShare);
        Assert.Equal("Security+ 4.9  ·  CySA+ 1.1", _vm.CertificationTags);
        Assert.Equal(new NumberedStep(2, "step 2"), _vm.ExerciseSteps[1]);
        Assert.False(_vm.HasPrevious);
        Assert.True(_vm.HasNext);
    }

    [Fact]
    public void SelectingAnOptionDeselectsOthers()
    {
        var question = new QuizQuestionViewModel(new QuizQuestion(1, 1, "Q", ["a", "b", "c"]), 1);

        question.Select(0);
        question.Select(2);

        Assert.Equal(2, question.SelectedIndex);
        Assert.Single(question.Options, o => o.IsSelected);
    }

    [Fact]
    public async Task QuizRequiresEveryAnswer()
    {
        await _vm.OnNavigatedToAsync();
        _vm.Questions[0].Select(1);

        await _vm.SubmitQuizCommand.ExecuteAsync(null);

        Assert.Equal("Answer every question before submitting.", _vm.ErrorMessage);
        Assert.DoesNotContain(_api.Calls, c => c is QuizSubmission);
    }

    [Fact]
    public async Task SubmittingQuizShowsResultsAndExplanations()
    {
        await _vm.OnNavigatedToAsync();
        _vm.Questions[0].Select(1);
        _vm.Questions[1].Select(0);

        await _vm.SubmitQuizCommand.ExecuteAsync(null);

        var submission = Assert.IsType<QuizSubmission>(_api.Calls.Single(c => c is QuizSubmission));
        Assert.Equal([1, 0], submission.Answers);
        Assert.Equal(50, _vm.QuizResult?.Score);
        Assert.Contains("pass mark is 70%", _vm.QuizSummary);
        Assert.True(_vm.Questions[0].IsCorrect);
        Assert.False(_vm.Questions[1].IsCorrect);
        Assert.True(_vm.Questions[1].Options[1].IsCorrectAnswer);
        Assert.Equal("Because 1", _vm.Questions[1].Explanation);
    }

    [Fact]
    public async Task RetryClearsAnswers()
    {
        await _vm.OnNavigatedToAsync();
        _vm.Questions[0].Select(1);
        _vm.Questions[1].Select(1);
        await _vm.SubmitQuizCommand.ExecuteAsync(null);

        _vm.RetryQuizCommand.Execute(null);

        Assert.Null(_vm.QuizResult);
        Assert.All(_vm.Questions, q => Assert.Null(q.SelectedIndex));
        Assert.All(_vm.Questions, q => Assert.False(q.IsGraded));
    }

    [Fact]
    public async Task NextLessonNavigates()
    {
        await _vm.OnNavigatedToAsync();

        _vm.NextLessonCommand.Execute(null);

        Assert.Equal(11, Assert.IsType<LessonViewModel>(_navigation.Current).LessonId);
    }

    [Fact]
    public async Task BackToCourseSelectsLessonsCourse()
    {
        await _vm.OnNavigatedToAsync();

        _vm.BackToCourseCommand.Execute(null);

        Assert.Equal("networking", Assert.IsType<CoursesViewModel>(_navigation.Current).InitialCourseSlug);
    }
}
