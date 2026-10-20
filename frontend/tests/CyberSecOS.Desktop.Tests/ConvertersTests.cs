using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CyberSecOS.Desktop.Converters;

namespace CyberSecOS.Desktop.Tests;

/// <summary>
/// Covers the pure value converters: their Convert logic and that ConvertBack is
/// unsupported. These need the WPF types (Visibility), so the suite targets
/// net9.0-windows and runs on the Windows CI job. The resource-dependent
/// LessonStatusBrushConverter is exercised by the manual view smoke test instead,
/// since it needs a running Application with the merged resource dictionaries.
/// </summary>
public class ConvertersTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static object? Convert(IValueConverter converter, object? value, object? parameter = null) =>
        converter.Convert(value, typeof(object), parameter, Culture);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("hello", true)]
    public void NotEmptyToVisibility_is_visible_only_for_non_empty(object? value, bool visible)
    {
        var result = Convert(new NotEmptyToVisibilityConverter(), value);
        Assert.Equal(visible ? Visibility.Visible : Visibility.Collapsed, result);
    }

    [Fact]
    public void NotEmptyToVisibility_is_visible_for_a_non_null_object()
    {
        Assert.Equal(Visibility.Visible, Convert(new NotEmptyToVisibilityConverter(), new object()));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(null, true)]
    public void InverseBoolToVisibility_is_collapsed_only_when_true(object? value, bool visible)
    {
        var result = Convert(new InverseBoolToVisibilityConverter(), value);
        Assert.Equal(visible ? Visibility.Visible : Visibility.Collapsed, result);
    }

    [Theory]
    [InlineData("completed", "Completed")]
    [InlineData("in_progress", "In progress")]
    [InlineData("not_started", "Not started")]
    [InlineData("anything else", "Not started")]
    public void LessonStatusLabel_maps_status_codes(string status, string label)
    {
        Assert.Equal(label, Convert(new LessonStatusLabelConverter(), status));
    }

    [Theory]
    [InlineData("**bold** and `code`", "bold and code")]
    [InlineData(null, "")]
    public void PlainText_strips_markdown_markers(string? value, string expected)
    {
        Assert.Equal(expected, Convert(new PlainTextConverter(), value));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(null, true)]
    public void InverseBool_negates(object? value, bool expected)
    {
        Assert.Equal(expected, Convert(new InverseBoolConverter(), value));
    }

    [Fact]
    public void InverseBool_round_trips_both_ways()
    {
        var converter = new InverseBoolConverter();
        Assert.Equal(false, converter.ConvertBack(true, typeof(object), null, Culture));
        Assert.Equal(true, converter.ConvertBack(false, typeof(object), null, Culture));
    }

    [Theory]
    [InlineData("Line", "Line", true)]
    [InlineData("line", "Line", true)]  // case-insensitive
    [InlineData("Bar", "Line", false)]
    [InlineData(null, "Line", false)]
    public void EnumEqualsVisibility_matches_parameter_name(object? value, string parameter, bool visible)
    {
        var result = Convert(new EnumEqualsVisibilityConverter(), value, parameter);
        Assert.Equal(visible ? Visibility.Visible : Visibility.Collapsed, result);
    }

    [Theory]
    [InlineData(5, "Exam in 5 days")]
    [InlineData(2, "Exam in 2 days")]
    [InlineData(1, "Exam tomorrow")]
    [InlineData(0, "Exam today")]
    [InlineData(-3, "Exam was 3 days ago")]
    public void ExamCountdown_reads_days_until_exam(int days, string expected)
    {
        Assert.Equal(expected, Convert(new ExamCountdownConverter(), days));
    }

    [Fact]
    public void ExamCountdown_is_empty_when_there_is_no_date()
    {
        Assert.Equal(string.Empty, Convert(new ExamCountdownConverter(), null));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("x", true)]
    public void NotNullToVisibility_is_visible_only_for_non_null(object? value, bool visible)
    {
        var result = Convert(new NotNullToVisibilityConverter(), value);
        Assert.Equal(visible ? Visibility.Visible : Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_is_unsupported_on_the_one_way_converters()
    {
        var oneWay = new IValueConverter[]
        {
            new NotEmptyToVisibilityConverter(),
            new InverseBoolToVisibilityConverter(),
            new LessonStatusLabelConverter(),
            new PlainTextConverter(),
            new EnumEqualsVisibilityConverter(),
            new ExamCountdownConverter(),
            new NotNullToVisibilityConverter(),
        };
        foreach (var converter in oneWay)
        {
            Assert.Throws<NotSupportedException>(
                () => converter.ConvertBack(null, typeof(object), null, Culture));
        }
    }
}
