using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.ViewModels;

public enum ExamStage
{
    Setup,
    Taking,
    Results,
}

/// <summary>One certification the learner can sit a practice exam for.</summary>
public sealed partial class ExamOptionViewModel : ObservableObject
{
    public ExamOptionViewModel(ExamInfo info)
    {
        Info = info;
    }

    public ExamInfo Info { get; }

    public string Name => Info.Name;

    public string ExamCode => Info.ExamCode;

    public int AvailableQuestions => Info.AvailableQuestions;

    [ObservableProperty]
    private ExamAttempt? _bestAttempt;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private ExamTarget? _target;

    public string BestSummary => BestAttempt is null
        ? "Not attempted yet"
        : $"Best: {BestAttempt.Score}% ({(BestAttempt.Passed ? "passed" : "not yet")})";

    public string TargetSummary => Target is null
        ? "No exam date set"
        : Target.DaysUntilExam switch
        {
            > 1 => $"Exam {Target.ExamDate:yyyy-MM-dd} — {Target.DaysUntilExam} days to go",
            1 => $"Exam {Target.ExamDate:yyyy-MM-dd} — tomorrow",
            0 => $"Exam {Target.ExamDate:yyyy-MM-dd} — today",
            _ => $"Exam {Target.ExamDate:yyyy-MM-dd} — {-Target.DaysUntilExam} days ago",
        };

    partial void OnTargetChanged(ExamTarget? value) => OnPropertyChanged(nameof(TargetSummary));
}

/// <summary>
/// Drives a timed practice exam: choose a certification and length, answer the
/// questions, then review the score and a per-objective breakdown of weak areas.
/// </summary>
public sealed partial class ExamsViewModel : PageViewModel, IPageViewModel
{
    private readonly ICyberSecApi _api;
    private readonly INavigationService _navigation;
    private DateTime _startedAt;

    public ExamsViewModel(ICyberSecApi api, INavigationService navigation, SessionService session)
        : base(session)
    {
        _api = api;
        _navigation = navigation;
    }

    /// <summary>Clock used for the exam timer; replaceable in tests.</summary>
    public Func<DateTime> Now { get; init; } = () => DateTime.UtcNow;

    public ObservableCollection<ExamOptionViewModel> Options { get; } = [];

    public ObservableCollection<QuizQuestionViewModel> Questions { get; } = [];

    [ObservableProperty]
    private ExamStage _stage = ExamStage.Setup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart), nameof(LengthCaption))]
    [NotifyCanExecuteChangedFor(nameof(StartExamCommand))]
    private ExamOptionViewModel? _selectedExam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LengthCaption))]
    [NotifyCanExecuteChangedFor(nameof(StartExamCommand))]
    private int _questionCount = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnsweredCount), nameof(ProgressCaption))]
    private ExamPaper? _paper;

    [ObservableProperty]
    private string _elapsedDisplay = "00:00";

    [ObservableProperty]
    private ExamResult? _result;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetTargetCommand))]
    private string _targetDateInput = string.Empty;

    public ObservableCollection<ObjectiveScore> WeakAreas { get; } = [];

    public bool CanStart => SelectedExam is { AvailableQuestions: > 0 } && QuestionCount >= ExamLimits.Min;

    public string LengthCaption => SelectedExam is null
        ? string.Empty
        : $"{EffectiveCount} questions (up to {SelectedExam.AvailableQuestions} available)";

    /// <summary>Requested length, capped at what the certification has.</summary>
    public int EffectiveCount => SelectedExam is null
        ? QuestionCount
        : Math.Min(QuestionCount, SelectedExam.AvailableQuestions);

    public int AnsweredCount => Questions.Count(q => q.SelectedIndex is not null);

    public string ProgressCaption => Paper is null ? string.Empty : $"{AnsweredCount} of {Questions.Count} answered";

    public string ResultHeadline => Result is null
        ? string.Empty
        : Result.Passed ? $"Passed — {Result.Score}%" : $"Not yet — {Result.Score}% (need {Result.PassMark}%)";

    public async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            Stage = ExamStage.Setup;
            Options.Clear();
            foreach (var cert in await _api.GetCertificationsAsync())
            {
                var info = await _api.GetExamInfoAsync(cert.Id);
                var option = new ExamOptionViewModel(info);
                var history = await _api.GetExamHistoryAsync(cert.Id);
                option.BestAttempt = history.OrderByDescending(a => a.Score).FirstOrDefault();
                option.Target = await _api.GetExamTargetAsync(cert.Id);
                Options.Add(option);
            }
            SelectedExam = Options.FirstOrDefault(o => o.AvailableQuestions > 0) ?? Options.FirstOrDefault();
        });
    }

    [RelayCommand]
    private void Select(ExamOptionViewModel? option)
    {
        if (option is not null)
        {
            SelectedExam = option;
        }
    }

    private bool CanSetTarget => SelectedExam is not null && DateOnly.TryParse(TargetDateInput, out _);

    [RelayCommand(CanExecute = nameof(CanSetTarget))]
    private async Task SetTargetAsync()
    {
        if (SelectedExam is null || !DateOnly.TryParse(TargetDateInput, out var when))
        {
            return;
        }
        var cert = SelectedExam;
        await RunAsync(async () =>
        {
            cert.Target = await _api.SetExamTargetAsync(cert.Info.CertificationId, new ExamTargetUpdate(when));
        });
    }

    [RelayCommand]
    private async Task ClearTargetAsync()
    {
        if (SelectedExam is null)
        {
            return;
        }
        var cert = SelectedExam;
        await RunAsync(async () =>
        {
            await _api.ClearExamTargetAsync(cert.Info.CertificationId);
            cert.Target = null;
            TargetDateInput = string.Empty;
        });
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartExamAsync()
    {
        if (SelectedExam is null)
        {
            return;
        }
        await RunAsync(async () =>
        {
            var paper = await _api.GetExamPaperAsync(SelectedExam.Info.CertificationId, EffectiveCount);
            Paper = paper;
            Questions.Clear();
            for (var i = 0; i < paper.Questions.Count; i++)
            {
                var vm = new QuizQuestionViewModel(paper.Questions[i], i + 1);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(QuizQuestionViewModel.SelectedIndex))
                    {
                        OnPropertyChanged(nameof(AnsweredCount));
                        OnPropertyChanged(nameof(ProgressCaption));
                    }
                };
                Questions.Add(vm);
            }
            Result = null;
            _startedAt = Now();
            ElapsedDisplay = "00:00";
            Stage = ExamStage.Taking;
        });
    }

    /// <summary>Called by the view's timer tick to refresh the elapsed display.</summary>
    public void UpdateElapsed()
    {
        if (Stage != ExamStage.Taking)
        {
            return;
        }
        var elapsed = Now() - _startedAt;
        ElapsedDisplay = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
    }

    [RelayCommand]
    private async Task SubmitExamAsync()
    {
        if (Paper is null)
        {
            return;
        }
        if (AnsweredCount < Questions.Count)
        {
            ErrorMessage = $"{Questions.Count - AnsweredCount} question(s) left unanswered. You can still submit, or finish them first.";
        }
        var duration = (int)Math.Max(0, (Now() - _startedAt).TotalSeconds);
        var answers = Questions.Select(q => new ExamAnswer(q.Question.Id, q.SelectedIndex)).ToList();
        await RunAsync(async () =>
        {
            var result = await _api.SubmitExamAsync(Paper.CertificationId, new ExamSubmission(answers, duration));
            Result = result;
            WeakAreas.Clear();
            foreach (var objective in result.Breakdown.Where(o => o.Percent < result.PassMark).OrderBy(o => o.Percent))
            {
                WeakAreas.Add(objective);
            }
            ErrorMessage = null;
            Stage = ExamStage.Results;
        });
    }

    [RelayCommand]
    private void BackToSetup()
    {
        Stage = ExamStage.Setup;
        _ = OnNavigatedToAsync();
    }

    [RelayCommand]
    private void BrowseCourses() => _navigation.NavigateTo<CoursesViewModel>();

    partial void OnSelectedExamChanged(ExamOptionViewModel? value)
    {
        foreach (var option in Options)
        {
            option.IsSelected = ReferenceEquals(option, value);
        }
        TargetDateInput = value?.Target is { } t ? t.ExamDate.ToString("yyyy-MM-dd") : string.Empty;
    }

    partial void OnResultChanged(ExamResult? value) => OnPropertyChanged(nameof(ResultHeadline));
}

public static class ExamLimits
{
    public const int Min = 5;
    public const int Max = 100;
}
