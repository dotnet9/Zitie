namespace Zitie.Desktop.Services;

/// <summary>教材目录快照文件。</summary>
public sealed record TextbookCatalogDocument
{
    public int CatalogYear { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string DataVersionUrl { get; set; } = string.Empty;

    public long SourceModuleVersion { get; set; }

    public string RetrievedDate { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public List<TextbookCatalogItem> Items { get; set; } = [];
}

/// <summary>公开教材版本元数据，不包含教材正文或页面资源。</summary>
public sealed record TextbookCatalogItem
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Edition { get; set; } = string.Empty;

    public List<string> Grades { get; set; } = [];

    public string Volume { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public string UpdatedAt { get; set; } = string.Empty;

    public string OnlineAt { get; set; } = string.Empty;
}

/// <summary>扫描输出目录 resources/textbooks/*.yml，加载公开教材版本索引。</summary>
public sealed class TextbookCatalog
{
    public TextbookCatalog() : this(ResourcePaths.Textbooks)
    {
    }

    public TextbookCatalog(string directory)
    {
        var documents = new List<TextbookCatalogDocument>();
        var items = new List<TextbookCatalogItem>();

        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.yml")
                         .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
                try
                {
                    var document = YamlResourceSerializer.DeserializeFile<TextbookCatalogDocument>(file);
                    if (document is null) continue;

                    documents.Add(document);
                    items.AddRange(document.Items);
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"教材版本目录解析失败，已跳过：{file}", exception);
                }

        Documents = documents;
        Items = items
            .Where(static item =>
                !string.IsNullOrWhiteSpace(item.Subject) &&
                !string.IsNullOrWhiteSpace(item.Edition) &&
                item.Grades.Count > 0)
            .DistinctBy(static item => item.Id)
            .OrderBy(static item => item.Phase, StringComparer.CurrentCulture)
            .ThenBy(static item => item.Subject, StringComparer.CurrentCulture)
            .ThenBy(static item => item.Edition, StringComparer.CurrentCulture)
            .ThenBy(static item => item.Title, StringComparer.CurrentCulture)
            .ToArray();

        ZitieLogging.Info($"教材版本目录加载完成：{Items.Count} 条（{directory}）");
    }

    public IReadOnlyList<TextbookCatalogDocument> Documents { get; }

    public IReadOnlyList<TextbookCatalogItem> Items { get; }

    public IReadOnlyList<string> EditionsFor(string subject, string grade)
    {
        return Items
            .Where(item =>
                (string.IsNullOrWhiteSpace(subject) ||
                 string.Equals(item.Subject, subject, StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(grade) ||
                 item.Grades.Contains(grade, StringComparer.Ordinal)))
            .Select(static item => item.Edition)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static edition => edition, StringComparer.CurrentCulture)
            .ToArray();
    }
}
