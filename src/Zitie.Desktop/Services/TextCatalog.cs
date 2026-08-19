using System.IO;
using System.Text.Json;

namespace Zitie.Desktop.Services;

/// <summary>
///     文本库条目：诗词/蒙学/名言/英文通用形态，lines 或 words 二选一。
/// </summary>
public sealed record TextEntry
{
    public string Title { get; init; } = string.Empty;

    public string Dynasty { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Lines { get; init; } = string.Empty;

    public string Words { get; init; } = string.Empty;

    /// <summary>作为字帖正文使用：英文库取 Words，其余取 Lines。</summary>
    public string Body => string.IsNullOrWhiteSpace(Words) ? Lines : Words;
}

/// <summary>
///     扫描输出目录 texts/*.json 构建文本库，供编辑页下拉选择；无效文件跳过并记录日志。
/// </summary>
public sealed class TextCatalog
{
    public TextCatalog()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "texts");
        var entries = new List<TextEntry>();

        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    var items = JsonSerializer.Deserialize(
                        File.ReadAllText(file), ZitieJsonContext.Default.ListTextEntry);
                    if (items is null) continue;
                    foreach (var item in items.Where(static item => !string.IsNullOrWhiteSpace(item.Title)))
                        entries.Add(item);
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"文本库文件解析失败，已跳过：{file}", exception);
                }

        Entries = entries;
        ZitieLogging.Info($"文本库加载完成：{Entries.Count} 条（{directory}）");
    }

    public IReadOnlyList<TextEntry> Entries { get; }
}
