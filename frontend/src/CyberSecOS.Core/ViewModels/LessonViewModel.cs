using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Text;
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
    [NotifyPropertyChangedFor(nameof(HasPrevious), nameof(HasNext), nameof(TheoryShare), nameof(CertificationTags), nameof(ExerciseSteps), nameof(TheoryBlocks), nameof(HasGradedLab), nameof(CanSelfMarkLab), nameof(IsGradedLabPending))]
    [NotifyCanExecuteChangedFor(nameof(PreviousLessonCommand), nameof(NextLessonCommand))]
    private LessonDetail? _lesson;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressChecklist), nameof(IsLabCompleted), nameof(IsTheoryCompleted), nameof(CanSelfMarkLab), nameof(IsGradedLabPending))]
    private LessonProgress? _progress;

    [ObservableProperty]
    private string _labNotes = string.Empty;

    /// <summary>Minutes actually spent on the lab; blank uses the lesson estimate.</summary>
    [ObservableProperty]
    private string _labMinutes = string.Empty;

    public bool IsTheoryCompleted => Progress?.TheoryCompleted == true;

    public bool IsLabCompleted => Progress?.LabCompleted == true;

    /// <summary>What is left before the lesson counts as complete.</summary>
    public string ProgressChecklist
    {
        get
        {
            if (Progress is null)
            {
                return string.Empty;
            }
            if (Progress.Status == LessonStatuses.Completed)
            {
                return "Lesson complete.";
            }
            var remaining = new List<string>();
            if (!Progress.LabCompleted)
            {
                remaining.Add(HasGradedLab ? "pass the graded lab" : "complete the practical exercise");
            }
            if (!Progress.QuizPassed)
            {
                remaining.Add("pass the quiz");
            }
            return "To finish this lesson: " + string.Join(" and ", remaining) + ".";
        }
    }

    public ObservableCollection<QuizQuestionViewModel> Questions { get; } = [];

    /// <summary>True when the lab is marked by the server rather than self-reported.</summary>
    public bool HasGradedLab => Lesson?.HasGradedLab == true;

    /// <summary>Self-marking is only offered for lessons without a graded lab.</summary>
    public bool CanSelfMarkLab => !HasGradedLab && !IsLabCompleted;

    /// <summary>A graded lab not yet passed: the exercise is completed by passing it, not by a button.</summary>
    public bool IsGradedLabPending => HasGradedLab && !IsLabCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabStatusText))]
    private LabDetail? _lab;

    public ObservableCollection<LabTaskViewModel> LabTasks { get; } = [];

    [ObservableProperty]
    private LabArtifact? _selectedArtifact;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabResultSummary))]
    private LabResult? _labResult;

    public string? LabResultSummary => LabResult is null
        ? null
        : $"{LabResult.Score}% ({LabResult.Correct}/{LabResult.Total}) - " +
          (LabResult.Passed ? "passed, lab complete" : $"pass mark is {LabResult.PassMark}%, use the hints and try again");

    /// <summary>Attempts so far, e.g. "Best score 80% after 2 attempts - passed".</summary>
    public string LabStatusText
    {
        get
        {
            if (Lab is null)
            {
                return string.Empty;
            }
            var status = Lab.Status;
            if (status.Attempts == 0)
            {
                return $"Not attempted yet. Pass mark {Lab.PassMark}%.";
            }
            var attempts = status.Attempts == 1 ? "1 attempt" : $"{status.Attempts} attempts";
            return $"Best score {status.BestScore}% after {attempts}" + (status.Passed ? " - passed." : $". Pass mark {Lab.PassMark}%.");
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuizSummary))]
    private QuizResult? _quizResult;

    /// <summary>Lesson theory parsed into paragraphs of styled runs for rich display.</summary>
    public IReadOnlyList<MarkdownParagraph> TheoryBlocks => MarkdownText.Parse(Lesson?.Theory);

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
        await RunAsync(async () =>
        {
            var lesson = await _api.GetLessonAsync(LessonId);
            Apply(lesson);
            ApplyLab(lesson.HasGradedLab ? await _api.GetLabAsync(lesson.Id) : null);
        });
    }

    private void ApplyLab(LabDetail? lab)
    {
        Lab = lab;
        LabResult = null;
        LabTasks.Clear();
        foreach (var task in lab?.Tasks ?? [])
        {
            LabTasks.Add(new LabTaskViewModel(task));
        }
        SelectedArtifact = lab?.Artifacts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SubmitLabAsync()
    {
        if (Lesson is null || Lab is null)
        {
            return;
        }
        if (LabTasks.Any(t => !t.IsAnswered))
        {
            ErrorMessage = "Answer every lab task before submitting.";
            return;
        }
        var answers = LabTasks.Select(t => t.SubmittedAnswer).ToList();
        await RunAsync(async () =>
        {
            var result = await _api.SubmitLabAsync(Lesson.Id, new LabSubmission(answers));
            foreach (var (task, taskResult) in LabTasks.Zip(result.Results))
            {
                task.ApplyResult(taskResult);
            }
            LabResult = result;
            Lab = Lab with
            {
                Status = new LabStatus(
                    Lab.Status.Attempts + 1,
                    Math.Max(Lab.Status.BestScore ?? 0, result.Score),
                    Lab.Status.Passed || result.Passed),
            };
            await RefreshProgressAsync();
            if (result.Passed)
            {
                StatusMessage = Progress?.Status == LessonStatuses.Completed ? "Lab passed. Lesson complete. Nice work." : "Lab passed.";
            }
        });
    }

    private void Apply(LessonDetail lesson)
    {
        Lesson = lesson;
        Progress = lesson.Progress;
        LabNotes = lesson.Progress.LabNotes ?? string.Empty;
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
    private async Task MarkTheoryReadAsync()
    {
        if (Lesson is not null)
        {
            await RunAsync(async () => Progress = await _api.MarkTheoryReadAsync(Lesson.Id));
        }
    }

    [RelayCommand]
    private async Task CompleteLabAsync()
    {
        if (Lesson is null)
        {
            return;
        }
        int? minutes = null;
        if (!string.IsNullOrWhiteSpace(LabMinutes))
        {
            if (!int.TryParse(LabMinutes, out var parsed) || parsed is < 1 or > 600)
            {
                ErrorMessage = "Lab time must be a whole number of minutes between 1 and 600.";
                return;
            }
            minutes = parsed;
        }
        var notes = string.IsNullOrWhiteSpace(LabNotes) ? null : LabNotes;
        await RunAsync(async () =>
        {
            Progress = await _api.CompleteLabAsync(Lesson.Id, new LabRequest(notes, minutes));
            StatusMessage = Progress.Status == LessonStatuses.Completed ? "Lesson complete. Nice work." : "Lab recorded.";
        });
    }

    [RelayCommand]
    private async Task ReopenLabAsync()
    {
        if (Lesson is not null)
        {
            await RunAsync(async () => Progress = await _api.ReopenLabAsync(Lesson.Id));
        }
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
