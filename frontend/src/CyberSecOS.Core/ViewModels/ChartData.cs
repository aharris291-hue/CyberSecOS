namespace CyberSecOS.Core.ViewModels;

/// <summary>
/// Chart-library-neutral chart description. The desktop app converts these into
/// LiveCharts2 series, which keeps view models testable without a renderer.
/// </summary>
public sealed record ChartSeries(string Name, IReadOnlyList<double> Values, string ColorHex);

public sealed record ChartData(IReadOnlyList<string> Labels, IReadOnlyList<ChartSeries> Series, double? MaxValue = null)
{
    public static ChartData Empty { get; } = new([], []);

    public bool IsEmpty => Series.Count == 0 || Series.All(s => s.Values.All(v => v == 0));
}

/// <summary>A headline number on the dashboard.</summary>
public sealed record StatTile(string Label, string Value, string Caption);

public static class ChartColors
{
    public const string Theory = "#60A5FA";
    public const string Lab = "#2DD4BF";
    public const string Quiz = "#FBBF24";
    public const string Progress = "#2DD4BF";
    public const string Readiness = "#4ADE80";
}
