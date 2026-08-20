using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Zitie.Desktop.Services;

internal static class YamlResourceSerializer
{
    public static T? DeserializeFile<T>(string path)
    {
        var root = LoadRoot(path);
        return DeserializeRoot<T>(root);
    }

    public static T? DeserializeText<T>(string text)
    {
        using var reader = new StringReader(text);
        var yaml = new YamlStream();
        yaml.Load(reader);
        var root = yaml.Documents.Count == 0 ? null : yaml.Documents[0].RootNode;
        return DeserializeRoot<T>(root);
    }

    private static T? DeserializeRoot<T>(YamlNode? root)
    {
        if (root is null) return default;

        if (typeof(T) == typeof(ModuleDefinition))
            return (T)(object)ReadModule(AsMapping(root));
        if (typeof(T) == typeof(List<PinyinCategory>))
            return (T)(object)ReadPinyinCategories(AsSequence(root));
        if (typeof(T) == typeof(TextbookCatalogDocument))
            return (T)(object)ReadTextbookDocument(AsMapping(root));

        throw new NotSupportedException($"不支持的 YAML 资源类型：{typeof(T).FullName}");
    }

    public static string Serialize<T>(T value)
    {
        if (value is ModuleDefinition module)
            return string.Join(Environment.NewLine, WriteModule(module)) + Environment.NewLine;

        throw new NotSupportedException($"不支持的 YAML 资源类型：{typeof(T).FullName}");
    }

    private static YamlNode? LoadRoot(string path)
    {
        using var reader = File.OpenText(path);
        var yaml = new YamlStream();
        yaml.Load(reader);
        return yaml.Documents.Count == 0 ? null : yaml.Documents[0].RootNode;
    }

    private static ModuleDefinition ReadModule(YamlMappingNode map)
    {
        return new ModuleDefinition
        {
            Id = GetString(map, "id"),
            Name = GetString(map, "name"),
            Description = GetString(map, "description"),
            Kind = GetString(map, "kind", "customText"),
            Enabled = GetBoolean(map, "enabled") ?? true,
            Defaults = ReadModuleDefaults(GetMapping(map, "defaults"))
        };
    }

    private static ModuleDefaults ReadModuleDefaults(YamlMappingNode? map)
    {
        if (map is null) return new ModuleDefaults();

        return new ModuleDefaults
        {
            Grid = GetOptionalString(map, "grid"),
            GridSize = GetDouble(map, "gridSize"),
            GridGap = GetDouble(map, "gridGap"),
            GroupGap = GetDouble(map, "groupGap"),
            HollowGlyph = GetBoolean(map, "hollowGlyph"),
            Mode = GetOptionalString(map, "mode"),
            GroupByWord = GetBoolean(map, "groupByWord"),
            ShowPinyin = GetBoolean(map, "showPinyin"),
            PinyinOnly = GetBoolean(map, "pinyinOnly"),
            Vertical = GetBoolean(map, "vertical"),
            ShowPoemHeader = GetBoolean(map, "showPoemHeader"),
            FrameBorder = GetBoolean(map, "frameBorder"),
            Background = GetOptionalString(map, "background"),
            BackgroundColor = GetOptionalString(map, "backgroundColor"),
            BackgroundLineColor = GetOptionalString(map, "backgroundLineColor"),
            BackgroundLineSpacing = GetDouble(map, "backgroundLineSpacing"),
            BackgroundArtwork = GetOptionalString(map, "backgroundArtwork"),
            Author = GetOptionalString(map, "author"),
            Dynasty = GetOptionalString(map, "dynasty"),
            Repeats = GetInt32(map, "repeats"),
            TraceCount = GetInt32(map, "traceCount"),
            Title = GetOptionalString(map, "title"),
            TraceColor = GetOptionalString(map, "traceColor"),
            TraceIntensity = GetOptionalString(map, "traceIntensity"),
            CellsPerLine = GetInt32(map, "cellsPerLine"),
            BlankCellLineCount = GetInt32(map, "blankCellLineCount"),
            GridColor = GetOptionalString(map, "gridColor"),
            TextColor = GetOptionalString(map, "textColor"),
            PageSize = GetOptionalString(map, "pageSize"),
            PageMargin = GetDouble(map, "pageMargin"),
            PageMarginTop = GetDouble(map, "pageMarginTop"),
            PageMarginBottom = GetDouble(map, "pageMarginBottom"),
            PageMarginLeft = GetDouble(map, "pageMarginLeft"),
            PageMarginRight = GetDouble(map, "pageMarginRight"),
            FontFamily = GetOptionalString(map, "fontFamily"),
            HeaderPreset = GetOptionalString(map, "headerPreset"),
            HeaderText = GetOptionalString(map, "headerText"),
            Text = GetOptionalString(map, "text")
        };
    }

    private static List<PinyinCategory> ReadPinyinCategories(YamlSequenceNode sequence)
    {
        return sequence.Children
            .Select(AsMapping)
            .Select(static map => new PinyinCategory
            {
                Category = GetString(map, "category"),
                Words = GetSequence(map, "words")
                    .Children
                    .Select(AsMapping)
                    .Select(static word => new PinyinWord
                    {
                        Word = GetString(word, "word"),
                        Pinyin = GetString(word, "pinyin")
                    })
                    .ToList()
            })
            .ToList();
    }

    private static TextbookCatalogDocument ReadTextbookDocument(YamlMappingNode map)
    {
        return new TextbookCatalogDocument
        {
            CatalogYear = GetInt32(map, "catalogYear") ?? 0,
            SourceName = GetString(map, "sourceName"),
            SourceUrl = GetString(map, "sourceUrl"),
            DataVersionUrl = GetString(map, "dataVersionUrl"),
            SourceModuleVersion = GetInt64(map, "sourceModuleVersion") ?? 0,
            RetrievedDate = GetString(map, "retrievedDate"),
            ItemCount = GetInt32(map, "itemCount") ?? 0,
            Items = GetSequence(map, "items")
                .Children
                .Select(AsMapping)
                .Select(static item => new TextbookCatalogItem
                {
                    Id = GetString(item, "id"),
                    Title = GetString(item, "title"),
                    Phase = GetString(item, "phase"),
                    Subject = GetString(item, "subject"),
                    Edition = GetString(item, "edition"),
                    Grades = GetSequence(item, "grades").Children.Select(Scalar).ToList(),
                    Volume = GetString(item, "volume"),
                    Publisher = GetString(item, "publisher"),
                    UpdatedAt = GetString(item, "updatedAt"),
                    OnlineAt = GetString(item, "onlineAt")
                })
                .ToList()
        };
    }

    private static IEnumerable<string> WriteModule(ModuleDefinition module)
    {
        var lines = new List<string>();
        Add(lines, 0, "id", module.Id);
        Add(lines, 0, "name", module.Name);
        Add(lines, 0, "description", module.Description);
        Add(lines, 0, "kind", module.Kind);
        Add(lines, 0, "enabled", module.Enabled);
        lines.Add("defaults:");

        var defaults = module.Defaults;
        Add(lines, 2, "grid", defaults.Grid);
        Add(lines, 2, "gridSize", defaults.GridSize);
        Add(lines, 2, "gridGap", defaults.GridGap);
        Add(lines, 2, "groupGap", defaults.GroupGap);
        Add(lines, 2, "hollowGlyph", defaults.HollowGlyph);
        Add(lines, 2, "mode", defaults.Mode);
        Add(lines, 2, "groupByWord", defaults.GroupByWord);
        Add(lines, 2, "showPinyin", defaults.ShowPinyin);
        Add(lines, 2, "pinyinOnly", defaults.PinyinOnly);
        Add(lines, 2, "vertical", defaults.Vertical);
        Add(lines, 2, "showPoemHeader", defaults.ShowPoemHeader);
        Add(lines, 2, "frameBorder", defaults.FrameBorder);
        Add(lines, 2, "background", defaults.Background);
        Add(lines, 2, "backgroundColor", defaults.BackgroundColor);
        Add(lines, 2, "backgroundLineColor", defaults.BackgroundLineColor);
        Add(lines, 2, "backgroundLineSpacing", defaults.BackgroundLineSpacing);
        Add(lines, 2, "backgroundArtwork", defaults.BackgroundArtwork);
        Add(lines, 2, "author", defaults.Author);
        Add(lines, 2, "dynasty", defaults.Dynasty);
        Add(lines, 2, "repeats", defaults.Repeats);
        Add(lines, 2, "traceCount", defaults.TraceCount);
        Add(lines, 2, "title", defaults.Title);
        Add(lines, 2, "traceColor", defaults.TraceColor);
        Add(lines, 2, "traceIntensity", defaults.TraceIntensity);
        Add(lines, 2, "cellsPerLine", defaults.CellsPerLine);
        Add(lines, 2, "blankCellLineCount", defaults.BlankCellLineCount);
        Add(lines, 2, "gridColor", defaults.GridColor);
        Add(lines, 2, "textColor", defaults.TextColor);
        Add(lines, 2, "pageSize", defaults.PageSize);
        Add(lines, 2, "pageMargin", defaults.PageMargin);
        Add(lines, 2, "pageMarginTop", defaults.PageMarginTop);
        Add(lines, 2, "pageMarginBottom", defaults.PageMarginBottom);
        Add(lines, 2, "pageMarginLeft", defaults.PageMarginLeft);
        Add(lines, 2, "pageMarginRight", defaults.PageMarginRight);
        Add(lines, 2, "fontFamily", defaults.FontFamily);
        Add(lines, 2, "headerPreset", defaults.HeaderPreset);
        Add(lines, 2, "headerText", defaults.HeaderText);
        Add(lines, 2, "text", defaults.Text);
        return lines;
    }

    private static void Add(ICollection<string> lines, int indent, string key, object? value)
    {
        if (value is null) return;
        lines.Add($"{new string(' ', indent)}{key}: {FormatScalar(value)}");
    }

    private static string FormatScalar(object value)
    {
        return value switch
        {
            bool flag => flag ? "true" : "false",
            int number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            float number => number.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            string text => Quote(text),
            _ => Quote(value.ToString() ?? string.Empty)
        };
    }

    private static string Quote(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal) + "\"";
    }

    private static YamlMappingNode AsMapping(YamlNode node)
    {
        return node as YamlMappingNode ?? new YamlMappingNode();
    }

    private static YamlSequenceNode AsSequence(YamlNode node)
    {
        return node as YamlSequenceNode ?? new YamlSequenceNode();
    }

    private static YamlMappingNode? GetMapping(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) ? node as YamlMappingNode : null;
    }

    private static YamlSequenceNode GetSequence(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) ? AsSequence(node) : new YamlSequenceNode();
    }

    private static string GetString(YamlMappingNode map, string key, string fallback = "")
    {
        return GetOptionalString(map, key) ?? fallback;
    }

    private static string? GetOptionalString(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) ? Scalar(node) : null;
    }

    private static bool? GetBoolean(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) &&
               bool.TryParse(Scalar(node), out var result)
            ? result
            : null;
    }

    private static int? GetInt32(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) &&
               int.TryParse(Scalar(node), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static long? GetInt64(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) &&
               long.TryParse(Scalar(node), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static double? GetDouble(YamlMappingNode map, string key)
    {
        return TryGetNode(map, key, out var node) &&
               double.TryParse(Scalar(node), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static bool TryGetNode(YamlMappingNode map, string key, out YamlNode value)
    {
        foreach (var (nodeKey, nodeValue) in map.Children)
            if (string.Equals(Scalar(nodeKey), key, StringComparison.OrdinalIgnoreCase))
            {
                value = nodeValue;
                return true;
            }

        value = new YamlScalarNode();
        return false;
    }

    private static string Scalar(YamlNode node)
    {
        return node is YamlScalarNode scalar ? scalar.Value ?? string.Empty : string.Empty;
    }
}
