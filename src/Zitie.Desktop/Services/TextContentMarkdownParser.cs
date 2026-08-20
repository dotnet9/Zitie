using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Zitie.Desktop.Models;

namespace Zitie.Desktop.Services;

public enum TextContentDiagnosticSeverity
{
    Warning,
    Error
}

public sealed record TextContentDiagnostic(
    string Source,
    int Line,
    TextContentDiagnosticSeverity Severity,
    string Message);

public sealed record TextContentParseResult(
    IReadOnlyList<TextEntry> Entries,
    IReadOnlyList<TextContentDiagnostic> Diagnostics);

/// <summary>
///     按 H1 学科、H2 年级、H3 学期、H4 单元、H5 条目的结构解析内容库。
///     解析方式沿用 CodeWF.Markdown 对 Markdig 语法树的处理思路，但仅保留内容库所需能力。
/// </summary>
public sealed class TextContentMarkdownParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    public TextContentParseResult Parse(string? markdown, string source = "<memory>")
    {
        var entries = new List<TextEntry>();
        var diagnostics = new List<TextContentDiagnostic>();
        var headings = new string[6];
        EntryBuilder? current = null;

        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        foreach (var block in document)
            switch (block)
            {
                case HeadingBlock heading:
                    FinalizeCurrent();
                    ApplyHeading(heading);
                    break;
                case ListBlock list when current is not null:
                    ReadMetadata(list, current, source, diagnostics);
                    break;
                case ParagraphBlock paragraph when current is not null:
                    current.Body.Add(ExtractInlineText(paragraph.Inline).Trim());
                    break;
            }

        FinalizeCurrent();
        return new TextContentParseResult(entries, diagnostics);

        void ApplyHeading(HeadingBlock heading)
        {
            var level = heading.Level;
            var value = ExtractInlineText(heading.Inline).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                AddDiagnostic(heading, TextContentDiagnosticSeverity.Error, $"H{level} 标题不能为空。");
                return;
            }

            if (level is < 1 or > 5)
            {
                AddDiagnostic(heading, TextContentDiagnosticSeverity.Warning, "内容库仅识别 H1 到 H5 标题。");
                return;
            }

            for (var childLevel = level + 1; childLevel < headings.Length; childLevel++)
                headings[childLevel] = string.Empty;

            if (level > 1 && string.IsNullOrWhiteSpace(headings[level - 1]))
            {
                AddDiagnostic(heading, TextContentDiagnosticSeverity.Error,
                    $"H{level}“{value}”缺少 H{level - 1} 上级标题。");
                return;
            }

            headings[level] = value;
            if (level == 5)
                current = new EntryBuilder(headings[1], headings[2], headings[3], headings[4], value, heading.Line);
        }

        void FinalizeCurrent()
        {
            if (current is null) return;

            var entry = current.Build();
            if (string.IsNullOrWhiteSpace(entry.Body))
                diagnostics.Add(new TextContentDiagnostic(
                    source,
                    current.Line + 1,
                    TextContentDiagnosticSeverity.Error,
                    $"条目“{entry.Title}”缺少正文，已跳过。"));
            else
                entries.Add(entry);

            current = null;
        }

        void AddDiagnostic(Block block, TextContentDiagnosticSeverity severity, string message)
        {
            diagnostics.Add(new TextContentDiagnostic(source, block.Line + 1, severity, message));
        }
    }

    private static void ReadMetadata(
        ListBlock list,
        EntryBuilder entry,
        string source,
        ICollection<TextContentDiagnostic> diagnostics)
    {
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var paragraph = item.OfType<ParagraphBlock>().FirstOrDefault();
            if (paragraph is null) continue;

            var text = ExtractInlineText(paragraph.Inline).Trim();
            var separator = text.IndexOfAny(['：', ':']);
            if (separator <= 0 || separator == text.Length - 1)
            {
                diagnostics.Add(new TextContentDiagnostic(
                    source,
                    paragraph.Line + 1,
                    TextContentDiagnosticSeverity.Warning,
                    $"元数据“{text}”应使用“名称：值”格式，已忽略。"));
                continue;
            }

            var key = text[..separator].Trim();
            var value = text[(separator + 1)..].Trim();
            if (!entry.SetMetadata(key, value))
                diagnostics.Add(new TextContentDiagnostic(
                    source,
                    paragraph.Line + 1,
                    TextContentDiagnosticSeverity.Warning,
                    $"未知元数据“{key}”，已忽略。"));
        }
    }

    private static string ExtractInlineText(ContainerInline? container)
    {
        if (container is null) return string.Empty;

        var builder = new StringBuilder();
        for (var inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.AppendLine();
                    break;
                case ContainerInline nested:
                    builder.Append(ExtractInlineText(nested));
                    break;
            }

        return builder.ToString();
    }

    private sealed class EntryBuilder(
        string subject,
        string grade,
        string semester,
        string unit,
        string title,
        int line)
    {
        private string _resourceType = string.Empty;
        private string _textbook = string.Empty;
        private string _edition = string.Empty;
        private string _dynasty = string.Empty;
        private string _author = string.Empty;
        private string _source = string.Empty;
        private string _copyright = string.Empty;

        public int Line { get; } = line;

        public List<string> Body { get; } = [];

        public bool SetMetadata(string key, string value)
        {
            switch (key)
            {
                case "类型":
                    _resourceType = value;
                    break;
                case "教材":
                    _textbook = value;
                    break;
                case "版本":
                    _edition = value;
                    break;
                case "朝代":
                    _dynasty = value;
                    break;
                case "作者":
                    _author = value;
                    break;
                case "来源":
                    _source = value;
                    break;
                case "版权":
                    _copyright = value;
                    break;
                default:
                    return false;
            }

            return true;
        }

        public TextEntry Build()
        {
            return new TextEntry
            {
                Subject = subject,
                Grade = grade,
                Semester = semester,
                Unit = unit,
                Title = title,
                ResourceType = _resourceType,
                Textbook = _textbook,
                Edition = _edition,
                Dynasty = _dynasty,
                Author = _author,
                Source = _source,
                Copyright = _copyright,
                Body = string.Join(Environment.NewLine, Body.Where(static part => !string.IsNullOrWhiteSpace(part)))
            };
        }
    }
}
