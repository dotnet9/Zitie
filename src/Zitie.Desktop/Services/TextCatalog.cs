using Zitie.Desktop.Models;

namespace Zitie.Desktop.Services;

/// <summary>扫描输出目录 resources/texts/**/*.md，构建可筛选的字帖内容库。</summary>
public sealed class TextCatalog
{
    public TextCatalog() : this(ResourcePaths.Texts)
    {
    }

    public TextCatalog(string directory)
    {
        var parser = new TextContentMarkdownParser();
        var entries = new List<TextEntry>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories)
                         .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
                try
                {
                    var result = parser.Parse(File.ReadAllText(file), file);
                    LogDiagnostics(result.Diagnostics);
                    foreach (var entry in result.Entries)
                    {
                        if (identities.Add(entry.Identity))
                            entries.Add(entry);
                        else
                            ZitieLogging.Warn(
                                $"文本库存在重复条目，已跳过：{entry.Subject}/{entry.Grade}/{entry.Semester}/{entry.Unit}/{entry.Title}");
                    }
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"文本库文件解析失败，已跳过：{file}", exception);
                }

        Entries = entries;
        ZitieLogging.Info($"文本库加载完成：{Entries.Count} 条（{directory}）");
    }

    public IReadOnlyList<TextEntry> Entries { get; }

    private static void LogDiagnostics(IEnumerable<TextContentDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var message = $"{diagnostic.Source}:{diagnostic.Line} {diagnostic.Message}";
            if (diagnostic.Severity == TextContentDiagnosticSeverity.Error)
                ZitieLogging.Warn(message);
            else
                ZitieLogging.Info(message);
        }
    }
}
