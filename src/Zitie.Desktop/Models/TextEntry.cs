namespace Zitie.Desktop.Models;

/// <summary>可用于生成字帖的结构化内容条目。</summary>
public sealed record TextEntry
{
    public string Subject { get; init; } = string.Empty;

    public string Grade { get; init; } = string.Empty;

    public string Semester { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string Textbook { get; init; } = string.Empty;

    public string Edition { get; init; } = string.Empty;

    public string Dynasty { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Copyright { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string DisplayPath => string.Join(" · ", new[] { Grade, Semester, Unit, ResourceType }
        .Where(static value => !string.IsNullOrWhiteSpace(value)));

    public string Identity => string.Join('\u001f', Subject, Grade, Semester, Unit, Title);
}
