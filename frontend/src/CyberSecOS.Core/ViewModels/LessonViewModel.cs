using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

public sealed record NumberedStep(int Number, string Text);

/// <summary>
/// One lesson, laid out in the order required by the education standards:
/// objectives, theory, examples, practical exercise, quiz, expected outcome.
/// </summary>
public sealed partial class LessonViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;

    public LessonViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    public int LessonId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrevious), nameof(HasNext), nameof(TheoryShare), nameof(CertificationTags), nameof(ExerciseSteps))]
    [NotifyCanExecuteChangedFor(nameof(PreviousLessonCommand), nameof(NextLessonCommand))]
    private LessonDetail? _lesson;

    [ObservableProperty]
    private LessonProgress? _progress;

    public ObservableCollection<QuizQuestionViewModel> Questions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuizSummary))]
    private QuizResult? _quizResult;

    public IReadOnlyList<NumberedStep> ExerciseSteps =>
        Lesson?.Exercise.Steps.Select((text, i) => new NumberedStep(i + 1, text)).ToList() ?? [];

    public bool HasPrevious => Lesson?.PreviousLessonId is not null;

    public bool HasNext => Lesson?.NextLessonId is not null;

    /// <summary>Share of the lesson spent on theory, e.g. "20% theory / 80% hands-on".</summary>
    public string TheoryShare
    {
        get
        {
            if (Lesson is null || Lesson.TheoryMinutes + Lesson.LabMinutes == 0)
            {
                return string.Empty;
            }
            var theory = (int)Math.Round(100.0 * Lesson.TheoryMinutes / (Lesson.TheoryMinutes + Lesson.LabMinutes));
            return $"{theory}% theory / {100 - theory}% hands-on";
        }
    }

    public string CertificationTags =>
        Lesson is null ? string.Empty : string.Join("  ·  ", Lesson.CertificationObjectives.Select(FormatObjective));

    public string? QuizSummary => QuizResult is null
        ? null
        : $"{QuizResult.Score}% ({QuizResult.Correct}/{QuizResult.Total}) - " +
          (QuizResult.Passed ? "passed" : $"pass mark is {QuizResult.PassMark}%, review and try again");

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () => Apply(await _api.GetLessonAsync(LessonId)));
    }

    private void Apply(LessonDetail lesson)
    {
        Lesson = lesson;
        Progress = lesson.Progress;
        QuizResult = null;
        Questions.Clear();
        for (var i = 0; i < lesson.Questions.Count; i++)
        {
            Questions.Add(new QuizQuestionViewModel(lesson.Questions[i], i + 1));
        }
    }

    [RelayCommand]
    private async Task SubmitQuizAsync()
    {
        if (Lesson is null)
        {
            return;
        }
        if (Questions.Any(q => q.SelectedIndex is null))
        {
            ErrorMessage = "Answer every question before submitting.";
            return;
        }
        var answers = Questions.Select(q => q.SelectedIndex).ToList();
        await RunAsync(async () =>
        {
            var result = await _api.SubmitQuizAsync(Lesson.Id, new QuizSubmission(answers));
            QuizResult = result;
            foreach (var (question, questionResult) in Questions.Zip(result.Results))
            {
                question.ApplyResult(questionResult);
            }
            await RefreshProgressAsync();
        });
    }

    [RelayCommand]
    private void RetryQuiz()
    {
        QuizResult = null;
        foreach (var question in Questions)
        {
            question.Reset();
        }
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))]
    private void PreviousLesson() => GoTo(Lesson?.PreviousLessonId);

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void NextLesson() => GoTo(Lesson?.NextLessonId);

    [RelayCommand]
    private void BackToCourse()
    {
        var slug = Lesson?.CourseSlug;
        _navigation.NavigateTo<CoursesViewModel>(vm => vm.InitialCourseSlug = slug);
    }

    private void GoTo(int? lessonId)
    {
        if (lessonId is { } id)
        {
            _navigation.NavigateTo<LessonViewModel>(vm => vm.LessonId = id);
        }
    }

    /// <summary>Re-reads lesson state after an action changed it on the server.</summary>
    private async Task RefreshProgressAsync()
    {
        var latest = await _api.GetLessonAsync(LessonId);
        Lesson = latest;
        Progress = latest.Progress;
    }

    internal static string FormatObjective(CertObjective objective) => objective.CertificationId switch
    {
        "secplus" => $"Security+ {objective.Code}",
        "sc900" => $"SC-900 skill {objective.Code}",
        "sc200" => $"SC-200 skill {objective.Code}",
        "cysa" => $"CySA+ {objective.Code}",
        _ => $"{objective.CertificationId} {objective.Code}",
    };
}
