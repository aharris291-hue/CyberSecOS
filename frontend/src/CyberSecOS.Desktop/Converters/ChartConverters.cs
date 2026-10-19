using System.Globalization;
using System.Windows.Data;
using CyberSecOS.Core.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CyberSecOS.Desktop.Converters;

/// <summary>
/// Maps chart-neutral <see cref="ChartData"/> from the view models to LiveCharts2 series.
/// ConverterParameter: "stacked" (stacked columns), "rows" (horizontal bars) or "pie".
/// </summary>
public sealed class ChartSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChartData data)
        {
            return Array.Empty<ISeries>();
        }
        return (parameter as string) switch
        {
            "stacked" => data.Series.Select(s => (ISeries)new StackedColumnSeries<double>
            {
                Name = s.Name,
                Values = s.Values.ToArray(),
                Fill = Paint(s.ColorHex),
                MaxBarWidth = 22,
            }).ToArray(),
            "rows" => data.Series.Select(s => (ISeries)new RowSeries<double>
            {
                Name = s.Name,
                Values = s.Values.ToArray(),
                Fill = Paint(s.ColorHex),
                MaxBarWidth = 18,
                DataLabelsPaint = Paint("#E6EDF3"),
                DataLabelsSize = 11,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:0}%",
            }).ToArray(),
            "pie" => data.Series.Where(s => s.Values.Sum() > 0).Select(s => (ISeries)new PieSeries<double>
            {
                Name = s.Name,
                Values = s.Values.ToArray(),
                Fill = Paint(s.ColorHex),
                InnerRadius = 50,
            }).ToArray(),
            _ => Array.Empty<ISeries>(),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static SolidColorPaint Paint(string hex) => new(SKColor.Parse(hex));
}

/// <summary>
/// Builds a chart axis. ConverterParameter "category" uses the data's labels;
/// "value" is a numeric axis bounded by the data's MaxValue when set.
/// </summary>
public sealed class ChartAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var data = value as ChartData ?? ChartData.Empty;
        var axis = new Axis
        {
            LabelsPaint = ChartSeriesConverter.Paint("#9AA7B4"),
            SeparatorsPaint = ChartSeriesConverter.Paint("#2F3B47"),
            TextSize = 11,
        };
        if ((parameter as string) == "category")
        {
            axis.Labels = data.Labels.ToArray();
            axis.SeparatorsPaint = null;
        }
        else
        {
            axis.MinLimit = 0;
            if (data.MaxValue is { } max)
            {
                axis.MaxLimit = max;
            }
        }
        return new[] { axis };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
