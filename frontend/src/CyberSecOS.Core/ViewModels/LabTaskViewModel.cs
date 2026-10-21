using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.ViewModels;

/// <summary>One graded-lab task: a typed answer, or a picked option for choice tasks.</summary>
public sealed partial class LabTaskViewModel : ObservableObject
{
    public LabTaskViewModel(LabTask task)
    {
        Definition = task;
        Options = new ObservableCollection<QuizOptionViewModel>(task.Options.Select((text, i) => new QuizOptionViewModel(i, text)));
        foreach (var option in Options)
        {
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(QuizOptionViewModel.IsSelected) && option.IsSelected)
                {
                    foreach (var other in Options.Where(o => o != option))
                    {
                        other.IsSelected = false;
                    }
                }
            };
        }
    }

    public LabTask Definition { get; }

    public string Prompt => $"{Definition.Index + 1}. {Definition.Prompt}";

    public bool IsChoice => Definition.Type == LabTaskTypes.Choice;

    public ObservableCollection<QuizOptionViewModel> Options { get; }

    /// <summary>How to format the answer, shown under the text box.</summary>
    public string AnswerHint => Definition.Type switch
    {
        LabTaskTypes.Number => "Enter a number.",
        LabTaskTypes.Set => "Separate items with commas; order does not matter.",
        _ => string.Empty,
    };

    [ObservableProperty]
    private string _answer = string.Empty;

    [ObservableProperty]
    private bool _isGraded;

    [ObservableProperty]
    private bool _isCorrect;

    [ObservableProperty]
    private string? _feedback;

    public bool IsAnswered => IsChoice ? Options.Any(o => o.IsSelected) : !string.IsNullOrWhiteSpace(Answer);

    /// <summary>The value sent to the server: the option index for choices, the trimmed text otherwise.</summary>
    public string? SubmittedAnswer => IsChoice
        ? Options.FirstOrDefault(o => o.IsSelected)?.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : string.IsNullOrWhiteSpace(Answer) ? null : Answer.Trim();

    public void Select(int index) => Options[index].IsSelected = true;

    public void ApplyResult(LabTaskResult result)
    {
        IsGraded = true;
        IsCorrect = result.IsCorrect;
        Feedback = (result.IsCorrect ? "Correct. " : "Not quite. ") + result.Feedback;
    }
}
