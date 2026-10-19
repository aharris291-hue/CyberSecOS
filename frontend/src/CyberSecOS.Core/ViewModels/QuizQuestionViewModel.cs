using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CyberSecOS.Core.Models;

namespace CyberSecOS.Core.ViewModels;

public sealed partial class QuizOptionViewModel : ObservableObject
{
    public QuizOptionViewModel(int index, string text)
    {
        Index = index;
        Text = text;
    }

    public int Index { get; }

    public string Text { get; }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>True on the right answer once the quiz is graded.</summary>
    [ObservableProperty]
    private bool _isCorrectAnswer;
}

public sealed partial class QuizQuestionViewModel : ObservableObject
{
    public QuizQuestionViewModel(QuizQuestion question, int number)
    {
        Question = question;
        Number = number;
        Options = new ObservableCollection<QuizOptionViewModel>(question.Options.Select((text, i) => new QuizOptionViewModel(i, text)));
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
                    OnPropertyChanged(nameof(SelectedIndex));
                }
            };
        }
    }

    public QuizQuestion Question { get; }

    public int Number { get; }

    public string Prompt => $"{Number}. {Question.Prompt}";

    public ObservableCollection<QuizOptionViewModel> Options { get; }

    public int? SelectedIndex => Options.FirstOrDefault(o => o.IsSelected)?.Index;

    [ObservableProperty]
    private bool _isGraded;

    [ObservableProperty]
    private bool _isCorrect;

    [ObservableProperty]
    private string? _explanation;

    public void Select(int index) => Options[index].IsSelected = true;

    public void ApplyResult(QuestionResult result)
    {
        IsGraded = true;
        IsCorrect = result.IsCorrect;
        Explanation = result.Explanation;
        foreach (var option in Options)
        {
            option.IsCorrectAnswer = option.Index == result.CorrectIndex;
        }
    }

    public void Reset()
    {
        IsGraded = false;
        IsCorrect = false;
        Explanation = null;
        foreach (var option in Options)
        {
            option.IsSelected = false;
            option.IsCorrectAnswer = false;
        }
    }
}
