namespace Zitie.Desktop.Models;

public sealed record TextHighlightSegment(string Text, bool IsHighlight);

public sealed class TextContentSearchResult
{
    private const int ExcerptLength = 96;
    private const int ExcerptLead = 28;

    private TextContentSearchResult(
        TextEntry entry,
        IReadOnlyList<TextHighlightSegment> titleSegments,
        IReadOnlyList<TextHighlightSegment> pathSegments,
        IReadOnlyList<TextHighlightSegment> excerptSegments)
    {
        Entry = entry;
        TitleSegments = titleSegments;
        PathSegments = pathSegments;
        ExcerptSegments = excerptSegments;
    }

    public TextEntry Entry { get; }

    public IReadOnlyList<TextHighlightSegment> TitleSegments { get; }

    public IReadOnlyList<TextHighlightSegment> PathSegments { get; }

    public IReadOnlyList<TextHighlightSegment> ExcerptSegments { get; }

    public string Title => Entry.Title;

    public string DisplayPath => Entry.DisplayPath;

    public static TextContentSearchResult Create(TextEntry entry, IReadOnlyList<string> terms)
    {
        return new TextContentSearchResult(
            entry,
            BuildSegments(entry.Title, terms),
            BuildSegments(entry.DisplayPath, terms),
            BuildSegments(CreateExcerpt(entry.Body, terms), terms));
    }

    private static string CreateExcerpt(string body, IReadOnlyList<string> terms)
    {
        var text = string.Join(" ", body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length <= ExcerptLength) return text;

        var matchIndex = terms
            .Select(term => text.IndexOf(term, StringComparison.CurrentCultureIgnoreCase))
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();
        var start = Math.Max(0, matchIndex - ExcerptLead);
        var length = Math.Min(ExcerptLength, text.Length - start);
        var excerpt = text.Substring(start, length);

        if (start > 0) excerpt = "..." + excerpt;
        if (start + length < text.Length) excerpt += "...";
        return excerpt;
    }

    private static IReadOnlyList<TextHighlightSegment> BuildSegments(string value, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrEmpty(value) || terms.Count == 0)
            return [new TextHighlightSegment(value, false)];

        var ranges = new List<(int Start, int End)>();
        foreach (var term in terms.Where(static term => term.Length > 0))
        {
            var start = 0;
            while (start < value.Length)
            {
                var index = value.IndexOf(term, start, StringComparison.CurrentCultureIgnoreCase);
                if (index < 0) break;

                ranges.Add((index, index + term.Length));
                start = index + Math.Max(1, term.Length);
            }
        }

        if (ranges.Count == 0) return [new TextHighlightSegment(value, false)];

        var merged = ranges
            .OrderBy(static range => range.Start)
            .ThenBy(static range => range.End)
            .Aggregate(new List<(int Start, int End)>(), static (items, range) =>
            {
                if (items.Count == 0 || range.Start > items[^1].End)
                {
                    items.Add(range);
                    return items;
                }

                var previous = items[^1];
                items[^1] = (previous.Start, Math.Max(previous.End, range.End));
                return items;
            });

        var segments = new List<TextHighlightSegment>();
        var cursor = 0;
        foreach (var (start, end) in merged)
        {
            if (start > cursor)
                segments.Add(new TextHighlightSegment(value[cursor..start], false));

            segments.Add(new TextHighlightSegment(value[start..end], true));
            cursor = end;
        }

        if (cursor < value.Length)
            segments.Add(new TextHighlightSegment(value[cursor..], false));

        return segments;
    }
}
