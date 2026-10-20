using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CyberSecOS.Core.Text;

namespace CyberSecOS.Desktop.Behaviors;

/// <summary>
/// Attached property that fills a <see cref="TextBlock"/>'s inlines from a parsed
/// Markdown paragraph, so lesson theory shows real bold and inline-code styling
/// instead of literal markers.
/// </summary>
public static class MarkdownInlines
{
    public static readonly DependencyProperty RunsProperty = DependencyProperty.RegisterAttached(
        "Runs",
        typeof(IReadOnlyList<MarkdownRun>),
        typeof(MarkdownInlines),
        new PropertyMetadata(null, OnRunsChanged));

    public static void SetRuns(DependencyObject element, IReadOnlyList<MarkdownRun>? value) =>
        element.SetValue(RunsProperty, value);

    public static IReadOnlyList<MarkdownRun>? GetRuns(DependencyObject element) =>
        (IReadOnlyList<MarkdownRun>?)element.GetValue(RunsProperty);

    private static void OnRunsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        textBlock.Inlines.Clear();
        if (e.NewValue is not IReadOnlyList<MarkdownRun> runs)
        {
            return;
        }

        var codeBrush = textBlock.TryFindResource("AccentBrush") as Brush ?? Brushes.Teal;
        foreach (var run in runs)
        {
            switch (run.Kind)
            {
                case MarkdownRunKind.Bold:
                    textBlock.Inlines.Add(new Bold(new Run(run.Text)));
                    break;
                case MarkdownRunKind.Code:
                    textBlock.Inlines.Add(new Run(run.Text)
                    {
                        FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
                        Foreground = codeBrush,
                    });
                    break;
                default:
                    textBlock.Inlines.Add(new Run(run.Text));
                    break;
            }
        }
    }
}
