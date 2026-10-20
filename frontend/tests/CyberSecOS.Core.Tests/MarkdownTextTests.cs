using CyberSecOS.Core.Text;

namespace CyberSecOS.Core.Tests;

public class MarkdownTextTests
{
    [Fact]
    public void EmptyOrWhitespaceReturnsNoParagraphs()
    {
        Assert.Empty(MarkdownText.Parse(null));
        Assert.Empty(MarkdownText.Parse("   \n  \n"));
    }

    [Fact]
    public void SplitsOnBlankLines()
    {
        var result = MarkdownText.Parse("First paragraph.\n\nSecond paragraph.");

        Assert.Equal(2, result.Count);
        Assert.Equal("First paragraph.", result[0].Runs.Single().Text);
        Assert.Equal("Second paragraph.", result[1].Runs.Single().Text);
    }

    [Fact]
    public void WrappedLinesInOneParagraphBecomeOneFlowingText()
    {
        var result = MarkdownText.Parse("A line\nwrapped across\nthree lines.");

        var para = Assert.Single(result);
        Assert.Equal("A line wrapped across three lines.", para.Runs.Single().Text);
    }

    [Fact]
    public void ParsesBoldRun()
    {
        var runs = MarkdownText.Parse("Use **least privilege** always.").Single().Runs;

        Assert.Equal(3, runs.Count);
        Assert.Equal(MarkdownRunKind.Text, runs[0].Kind);
        Assert.Equal("Use ", runs[0].Text);
        Assert.Equal(MarkdownRunKind.Bold, runs[1].Kind);
        Assert.Equal("least privilege", runs[1].Text);
        Assert.Equal(" always.", runs[2].Text);
    }

    [Fact]
    public void ParsesInlineCode()
    {
        var runs = MarkdownText.Parse("Run `nmap -sn` first.").Single().Runs;

        Assert.Equal(MarkdownRunKind.Code, runs[1].Kind);
        Assert.Equal("nmap -sn", runs[1].Text);
    }

    [Fact]
    public void ParsesBoldAndCodeInSameParagraph()
    {
        var runs = MarkdownText.Parse("**SLE** is `value x factor`.").Single().Runs;

        Assert.Equal(MarkdownRunKind.Bold, runs[0].Kind);
        Assert.Equal("SLE", runs[0].Text);
        Assert.Contains(runs, r => r.Kind == MarkdownRunKind.Code && r.Text == "value x factor");
    }

    [Fact]
    public void UnmatchedBoldMarkerIsLiteral()
    {
        var runs = MarkdownText.Parse("A 3 ** 2 expression").Single().Runs;

        var run = Assert.Single(runs);
        Assert.Equal(MarkdownRunKind.Text, run.Kind);
        Assert.Contains("**", run.Text);
    }

    [Fact]
    public void UnmatchedCodeMarkerIsLiteral()
    {
        var runs = MarkdownText.Parse("A lone ` backtick").Single().Runs;

        var run = Assert.Single(runs);
        Assert.Equal(MarkdownRunKind.Text, run.Kind);
        Assert.Contains("`", run.Text);
    }

    [Fact]
    public void CollapsesMultipleBlankLinesIntoOneBreak()
    {
        var result = MarkdownText.Parse("One.\n\n\n\nTwo.");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void BoldAtStartAndEndAreCaptured()
    {
        var runs = MarkdownText.Parse("**Governance** directs **control**").Single().Runs;

        Assert.Equal(MarkdownRunKind.Bold, runs[0].Kind);
        Assert.Equal("Governance", runs[0].Text);
        Assert.Equal(MarkdownRunKind.Bold, runs[^1].Kind);
        Assert.Equal("control", runs[^1].Text);
    }

    [Fact]
    public void RealLessonTheoryParsesIntoMultipleParagraphs()
    {
        const string theory = "**Governance** is how an organisation directs security.\nIt covers roles and policies.\n\n**Compliance** is meeting obligations like `GDPR` and `HIPAA`.";

        var result = MarkdownText.Parse(theory);

        Assert.Equal(2, result.Count);
        Assert.Equal(MarkdownRunKind.Bold, result[0].Runs[0].Kind);
        Assert.Contains(result[1].Runs, r => r.Kind == MarkdownRunKind.Code && r.Text == "GDPR");
    }
}
