using System.Text;

namespace CyberSecOS.Core.Text;

/// <summary>The kind of an inline run inside a paragraph.</summary>
public enum MarkdownRunKind
{
    Text,
    Bold,
    Code,
}

/// <summary>A styled span of text within a paragraph.</summary>
public sealed record MarkdownRun(string Text, MarkdownRunKind Kind);

/// <summary>A paragraph: a sequence of inline runs.</summary>
public sealed record MarkdownParagraph(IReadOnlyList<MarkdownRun> Runs);

/// <summary>
/// A deliberately small Markdown reader for lesson theory. It understands only
/// what the content uses: blank-line-separated paragraphs, <c>**bold**</c> spans
/// and <c>`inline code`</c>. Everything else is left as plain text, and an
/// unmatched marker is treated literally rather than swallowing the rest of the
/// text. Code blocks (example bodies) are rendered elsewhere as preformatted
/// text and are not handled here.
/// </summary>
public static class MarkdownText
{
    public static IReadOnlyList<MarkdownParagraph> Parse(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        var normalised = source.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        var paragraphs = new List<MarkdownParagraph>();
        foreach (var block in SplitParagraphs(normalised))
        {
            var text = CollapseWhitespace(block);
            if (text.Length == 0)
            {
                continue;
            }
            paragraphs.Add(new MarkdownParagraph(ParseInline(text)));
        }
        return paragraphs;
    }

    private static IEnumerable<string> SplitParagraphs(string text)
    {
        var current = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            if (line.Trim().Length == 0)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
            {
                if (current.Length > 0)
                {
                    current.Append('\n');
                }
                current.Append(line);
            }
        }
        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary>Join wrapped lines into one flowing paragraph and collapse runs of spaces.</summary>
    private static string CollapseWhitespace(string block)
    {
        var sb = new StringBuilder(block.Length);
        var lastWasSpace = false;
        foreach (var ch in block)
        {
            var c = ch == '\n' || ch == '\t' ? ' ' : ch;
            if (c == ' ')
            {
                if (lastWasSpace)
                {
                    continue;
                }
                lastWasSpace = true;
            }
            else
            {
                lastWasSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private static List<MarkdownRun> ParseInline(string text)
    {
        var runs = new List<MarkdownRun>();
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length > 0)
            {
                runs.Add(new MarkdownRun(buffer.ToString(), MarkdownRunKind.Text));
                buffer.Clear();
            }
        }

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > i + 1)
                {
                    Flush();
                    runs.Add(new MarkdownRun(text[(i + 2)..end], MarkdownRunKind.Bold));
                    i = end + 2;
                    continue;
                }
            }
            else if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    Flush();
                    runs.Add(new MarkdownRun(text[(i + 1)..end], MarkdownRunKind.Code));
                    i = end + 1;
                    continue;
                }
            }
            buffer.Append(text[i]);
            i++;
        }
        Flush();
        return runs;
    }
}
