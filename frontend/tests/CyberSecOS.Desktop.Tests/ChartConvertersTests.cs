using System.Globalization;
using CyberSecOS.Core.ViewModels;
using CyberSecOS.Desktop.Converters;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace CyberSecOS.Desktop.Tests;

/// <summary>
/// Covers the LiveCharts adapters that turn chart-neutral <see cref="ChartData"/> from
/// the view models into LiveCharts2 series and axes. Creating the series and axis objects
/// needs no renderer or UI thread, so these run on the Windows CI job alongside the
/// value-converter tests.
/// </summary>
public class ChartConvertersTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static ChartData SampleData() =>
        new(
            Labels: new[] { "Mon", "Tue" },
            Series: new[]
            {
                new ChartSeries("Theory", new double[] { 10, 20 }, ChartColors.Theory),
                new ChartSeries("Lab", new double[] { 30, 40 }, ChartColors.Lab),
            },
            MaxValue: 100);

    private static ISeries[] Series(object? value, string? parameter) =>
        (ISeries[])new ChartSeriesConverter().Convert(value, typeof(object), parameter, Culture);

    private static Axis[] Axes(object? value, string? parameter) =>
        (Axis[])new ChartAxisConverter().Convert(value, typeof(object), parameter, Culture);

    // --- Series -------------------------------------------------------------

    [Fact]
    public void Series_is_empty_for_non_chart_data()
    {
        Assert.Empty(Series("not chart data", "stacked"));
        Assert.Empty(Series(null, "stacked"));
    }

    [Fact]
    public void Series_is_empty_for_an_unknown_parameter()
    {
        Assert.Empty(Series(SampleData(), "bogus"));
    }

    [Fact]
    public void Stacked_parameter_builds_a_stacked_column_series_per_data_series()
    {
        var series = Series(SampleData(), "stacked");
        Assert.Equal(2, series.Length);
        Assert.All(series, s => Assert.IsType<StackedColumnSeries<double>>(s));
        Assert.Equal(new[] { "Theory", "Lab" }, series.Select(s => s.Name));
    }

    [Fact]
    public void Rows_parameter_builds_row_series()
    {
        var series = Series(SampleData(), "rows");
        Assert.Equal(2, series.Length);
        Assert.All(series, s => Assert.IsType<RowSeries<double>>(s));
    }

    [Fact]
    public void Pie_parameter_drops_series_whose_values_sum_to_zero()
    {
        var data = new ChartData(
            Labels: Array.Empty<string>(),
            Series: new[]
            {
                new ChartSeries("Done", new double[] { 5 }, ChartColors.Progress),
                new ChartSeries("None", new double[] { 0 }, ChartColors.Quiz),
            });
        var series = Series(data, "pie");
        Assert.Single(series);
        Assert.IsType<PieSeries<double>>(series[0]);
        Assert.Equal("Done", series[0].Name);
    }

    [Fact]
    public void Series_convert_back_is_unsupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new ChartSeriesConverter().ConvertBack(null, typeof(object), null, Culture));
    }

    // --- Axis ---------------------------------------------------------------

    [Fact]
    public void Category_axis_uses_the_data_labels_and_hides_separators()
    {
        var axes = Axes(SampleData(), "category");
        var axis = Assert.Single(axes);
        Assert.Equal(new[] { "Mon", "Tue" }, axis.Labels);
        Assert.Null(axis.SeparatorsPaint);
    }

    [Fact]
    public void Value_axis_starts_at_zero_and_is_capped_by_the_max_value()
    {
        var axis = Assert.Single(Axes(SampleData(), "value"));
        Assert.Equal(0d, axis.MinLimit);
        Assert.Equal(100d, axis.MaxLimit);
    }

    [Fact]
    public void Value_axis_has_no_cap_when_the_data_sets_no_max()
    {
        var data = new ChartData(new[] { "A" }, new[] { new ChartSeries("S", new double[] { 1 }, "#fff") });
        var axis = Assert.Single(Axes(data, "value"));
        Assert.Equal(0d, axis.MinLimit);
        Assert.Null(axis.MaxLimit);
    }

    [Fact]
    public void Axis_falls_back_to_empty_data_for_a_null_value()
    {
        var axis = Assert.Single(Axes(null, "value"));
        Assert.Equal(0d, axis.MinLimit);
        Assert.Null(axis.MaxLimit);
    }

    [Fact]
    public void Axis_convert_back_is_unsupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new ChartAxisConverter().ConvertBack(null, typeof(object), null, Culture));
    }
}
