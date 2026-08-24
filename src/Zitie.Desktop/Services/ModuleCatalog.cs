using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Zitie.Desktop.Services;

/// <summary>模板定义：支持样式清单生成的模板，以及 module.yml 与 assets/ 组成的目录模板。</summary>
public sealed record ModuleDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>模板库分类；为空时按格型和用途自动推断。</summary>
    public string? Category { get; set; }

    /// <summary>模块处理器类型，决定编辑器形态。</summary>
    public string Kind { get; set; } = "customText";

    /// <summary>是否可用；false 时卡片显示“即将上线”。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>模块默认参数：grid / mode / repeats / traceCount / title / text。</summary>
    public ModuleDefaults Defaults { get; set; } = new();

    /// <summary>模板来源路径，用于诊断。</summary>
    public string? SourcePath { get; set; }

    /// <summary>原始 NQEZ 样式 ID。</summary>
    public string SourceTemplateId { get; set; } = string.Empty;

    /// <summary>展示顺序，保持网页列表顺序。</summary>
    public int DisplayOrder { get; set; }

    /// <summary>按网页标签映射后的分类；不包含“高级VIP”。</summary>
    public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();

    /// <summary>模板卡片使用的本地示例图。</summary>
    public string PreviewImagePath { get; set; } = string.Empty;

    /// <summary>目录模板资源缓存；键使用规范化相对路径。</summary>
    public IReadOnlyDictionary<string, byte[]> Assets { get; set; } =
        new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    public string? ReadTextAsset(string? path)
    {
        var key = NormalizePackagePath(path);
        return key is not null && Assets.TryGetValue(key, out var bytes)
            ? Encoding.UTF8.GetString(bytes)
            : null;
    }

    internal static string? NormalizePackagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var normalized = path.Trim().Replace('\\', '/');
        if (Path.IsPathFullyQualified(normalized)) return null;

        var parts = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => part.Trim())
            .ToArray();
        if (parts.Length == 0 ||
            parts.Any(static part => part is "." or ".." || part.Contains(':', StringComparison.Ordinal)))
            return null;

        return string.Join('/', parts);
    }
}

public sealed record ModuleDefaults
{
    public string? PracticeLayout { get; set; }

    public bool? BlankContentLayout { get; set; }

    public bool? FillContentAreaWithBlankCells { get; set; }

    public int? LayoutColumns { get; set; }

    public int? LayoutRows { get; set; }

    public string? Grid { get; set; }

    public double? GridSize { get; set; }

    public double? GridGap { get; set; }

    public double? GroupGap { get; set; }

    public bool? HollowGlyph { get; set; }

    public string? Mode { get; set; }

    public bool? GroupByWord { get; set; }

    public bool? ShowPinyin { get; set; }

    public bool? PinyinOnly { get; set; }

    public bool? ShowStrokeOrder { get; set; }

    public bool? Vertical { get; set; }

    public bool? ShowPoemHeader { get; set; }

    public bool? FrameBorder { get; set; }

    public string? Background { get; set; }

    public string? BackgroundColor { get; set; }

    public string? BackgroundLineColor { get; set; }

    public double? BackgroundLineSpacing { get; set; }

    public string? BackgroundArtwork { get; set; }

    public string? Author { get; set; }

    public string? Dynasty { get; set; }

    public int? Repeats { get; set; }

    public int? TraceCount { get; set; }

    public string? Title { get; set; }

    public string? TraceColor { get; set; }

    public string? TraceIntensity { get; set; }

    public int? CellsPerLine { get; set; }

    public int? BlankCellLineCount { get; set; }

    public string? GridColor { get; set; }

    public string? TextColor { get; set; }

    public string? PageSize { get; set; }

    public double? PageMargin { get; set; }

    public double? PageMarginTop { get; set; }

    public double? PageMarginBottom { get; set; }

    public double? PageMarginLeft { get; set; }

    public double? PageMarginRight { get; set; }

    public string? FontFamily { get; set; }

    public string? HeaderPreset { get; set; }

    public string? HeaderText { get; set; }

    public string? Text { get; set; }
}

/// <summary>
///     合并样式清单、内置目录模板和用户模板；.zi 仅在同名目录不存在时解压，
///     后加载的用户模板可用相同 Id 覆盖内置模板。
/// </summary>
public sealed class ModuleCatalog
{
    private const string StyleCatalogFileName = "styles.json";
    private const string ModuleFileName = "module.yml";
    private const int DirectoryTemplateOrderStart = 10_000;
    private const int UserTemplateOrderStart = 20_000;

    public static readonly IReadOnlyList<string> StyleCategoryOrder =
    [
        "汉字", "拼音", "数字", "英文", "有笔顺", "组词", "书法", "生字", "脱格", "文章", "试卷", "测试卡", "名字", "封面"
    ];

    private static readonly IReadOnlyDictionary<int, string> TagNames = new Dictionary<int, string>
    {
        [1] = "汉字",
        [2] = "拼音",
        [3] = "数字",
        [4] = "英文",
        [5] = "有笔顺",
        [6] = "组词",
        [7] = "书法",
        [8] = "生字",
        [9] = "脱格",
        [10] = "文章",
        [11] = "试卷",
        [12] = "测试卡",
        [13] = "高级VIP",
        [14] = "名字",
        [15] = "封面"
    };

    public ModuleCatalog()
        : this(
            ResourcePaths.ModuleStyles,
            ResourcePaths.ModuleStyleImages,
            ResourcePaths.Modules,
            ResolveDefaultUserDirectory())
    {
    }

    public ModuleCatalog(string styleDirectory)
        : this(styleDirectory, Path.Combine(styleDirectory, "images"), string.Empty, string.Empty)
    {
    }

    public ModuleCatalog(string styleDirectory, string imageDirectory)
        : this(styleDirectory, imageDirectory, string.Empty, string.Empty)
    {
    }

    public ModuleCatalog(
        string styleDirectory,
        string imageDirectory,
        string builtInDirectory,
        string userDirectory)
    {
        StyleDirectory = styleDirectory;
        ImageDirectory = imageDirectory;
        BuiltInDirectory = builtInDirectory;
        UserDirectory = userDirectory;
        Reload();
    }

    public string StyleDirectory { get; }

    public string ImageDirectory { get; }

    public string BuiltInDirectory { get; }

    public string UserDirectory { get; }

    public IReadOnlyList<ModuleDefinition> Modules { get; private set; } = Array.Empty<ModuleDefinition>();

    public void Reload()
    {
        var modules = new List<ModuleDefinition>();
        modules.AddRange(LoadStyleModules());
        LoadDirectory(BuiltInDirectory, modules, DirectoryTemplateOrderStart, isUserDirectory: false);
        LoadDirectory(UserDirectory, modules, UserTemplateOrderStart, isUserDirectory: true);

        Modules = modules
            .GroupBy(module => module.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(module => module.Enabled ? 0 : 1)
            .ThenBy(module => module.DisplayOrder)
            .ThenBy(module => module.Name, StringComparer.CurrentCulture)
            .ToList();

        ZitieLogging.Info(
            $"模板目录加载完成：{Modules.Count} 个模板（样式：{StyleDirectory}；内置：{BuiltInDirectory}；用户：{UserDirectory}）");
    }

    public ModuleDefinition? Find(string? id)
    {
        return string.IsNullOrEmpty(id)
            ? null
            : Modules.FirstOrDefault(module => string.Equals(
                module.Id,
                id,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveDefaultUserDirectory()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        return Path.Combine(localApplicationData, "Zitie", "modules");
    }

    private IReadOnlyList<ModuleDefinition> LoadStyleModules()
    {
        var catalogPath = Path.Combine(StyleDirectory, StyleCatalogFileName);
        if (!File.Exists(catalogPath))
        {
            ZitieLogging.Warn($"样式模板清单不存在，已跳过：{catalogPath}");
            return Array.Empty<ModuleDefinition>();
        }

        try
        {
            using var stream = File.OpenRead(catalogPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                ZitieLogging.Warn($"样式模板清单缺少 items 数组：{catalogPath}");
                return Array.Empty<ModuleDefinition>();
            }

            var modules = new List<ModuleDefinition>(items.GetArrayLength());
            var order = 0;
            foreach (var item in items.EnumerateArray())
            {
                var module = ReadStyleModule(item, catalogPath, order);
                if (module is not null)
                    modules.Add(module);
                order++;
            }

            return modules;
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"样式模板清单解析失败，已跳过：{catalogPath}", exception);
            return Array.Empty<ModuleDefinition>();
        }
    }

    private ModuleDefinition? ReadStyleModule(JsonElement item, string catalogPath, int order)
    {
        var sourceId = RequiredString(item, "id");
        var title = RequiredString(item, "title");
        var imageFileName = RequiredString(item, "imageFileName");
        if (string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(imageFileName))
        {
            ZitieLogging.Warn($"样式模板条目缺少必要字段，已跳过：{catalogPath} #{order + 1}");
            return null;
        }

        var tags = ReadTags(item);
        var categories = ResolveCategories(tags);
        var previewPath = Path.Combine(ImageDirectory, imageFileName);
        var description = categories.Count == 0
            ? "按示例图生成的字帖模板"
            : $"按示例图生成 · {string.Join(" / ", categories)}";

        var defaults = CreateDefaults(title, tags);
        ApplyDefaultOverrides(defaults, ReadDefaultOverrides(item, catalogPath, order));

        return new ModuleDefinition
        {
            Id = $"nqez-{sourceId}",
            SourceTemplateId = sourceId,
            Name = title,
            Description = description,
            Kind = "styleTemplate",
            Enabled = true,
            SourcePath = catalogPath,
            DisplayOrder = order,
            Categories = categories,
            PreviewImagePath = previewPath,
            Defaults = defaults
        };
    }

    private static IReadOnlyList<string> ResolveCategories(IReadOnlySet<int> tags)
    {
        var categories = StyleCategoryOrder
            .Where(category => tags.Any(tag =>
                TagNames.TryGetValue(tag, out var tagName) &&
                string.Equals(tagName, category, StringComparison.Ordinal)))
            .ToArray();

        return categories.Length == 0 ? ["汉字"] : categories;
    }

    private static ModuleDefaults CreateDefaults(string title, IReadOnlySet<int> tags)
    {
        var blankLayout = IsBlankLayout(title, tags);
        var grid = ResolveGrid(title, tags, blankLayout);
        var vertical = IsVerticalLayout(title, tags);
        var fillBlankCells = !blankLayout && !IsPoetryLike(title);
        var groupByWord = !blankLayout && ShouldGroupByWord(title, tags);
        var mode = ResolveMode(title, tags, blankLayout);
        var (columns, rows) = ResolveBlankLayout(title, tags, blankLayout, vertical);
        var showPinyin = !blankLayout && ShouldShowPinyin(title, tags);
        var showStrokeOrder = !blankLayout && ShouldShowStrokeOrder(title, tags);
        var pinyinOnly = !blankLayout && showPinyin && ContainsAny(title, "看拼音写", "拼音测试", "注音练习");
        var poemHeader = !blankLayout && ContainsAny(title, "古诗", "诗词");
        var repeats = ResolveRepeats(title, tags, groupByWord, showStrokeOrder);
        var traceCount = ResolveTraceCount(title, mode, blankLayout, repeats, showStrokeOrder);

        return new ModuleDefaults
        {
            BlankContentLayout = blankLayout,
            FillContentAreaWithBlankCells = fillBlankCells,
            LayoutColumns = columns,
            LayoutRows = rows,
            Grid = grid,
            GridSize = ResolveGridSize(title, tags, grid, blankLayout),
            GridGap = ContainsAny(title, "带间距") ? 2.5 : 1.5,
            GroupGap = groupByWord || vertical ? 2.5 : 2,
            HollowGlyph = ContainsAny(title, "空心", "双钩"),
            Mode = mode,
            GroupByWord = groupByWord,
            ShowPinyin = showPinyin,
            PinyinOnly = pinyinOnly,
            ShowStrokeOrder = showStrokeOrder,
            Vertical = vertical,
            ShowPoemHeader = poemHeader,
            FrameBorder = blankLayout || tags.Contains(7) || tags.Contains(15),
            Background = ResolveBackground(title, tags),
            Repeats = repeats,
            TraceCount = traceCount,
            Title = title,
            TraceIntensity = traceCount == 0 ? "white" : "light",
            CellsPerLine = ResolveCellsPerLine(title, tags, grid, groupByWord, blankLayout, showStrokeOrder),
            BlankCellLineCount = ContainsAny(title, "脱格", "横线") ? 1 : 0,
            GridColor = ResolveGridColor(title, tags),
            TextColor = "#1A1A1A",
            PageSize = ResolvePageSize(title),
            PageMargin = ResolvePageMargin(title, tags, blankLayout),
            FontFamily = ResolveFontFamily(title, tags),
            HeaderPreset = ResolveHeaderPreset(title, tags, blankLayout, poemHeader),
            HeaderText = "姓名_班级---年_月_日",
            Text = blankLayout ? null : ResolveSampleText(title, tags)
        };
    }

    private static bool IsBlankLayout(string title, IReadOnlySet<int> tags)
    {
        if (tags.Contains(11) || tags.Contains(12) || tags.Contains(15)) return true;
        return ContainsAny(title,
            "作文格", "运算纸", "练习题", "试卷", "测试", "检测", "测评", "测试卡",
            "封面", "课程目录", "目录", "打卡", "记录卡", "生成表格", "表格",
            "课前试写", "学前学后", "幼小衔接", "找书写错误", "纠错", "连一连", "补齐");
    }

    private static string ResolveGrid(string title, IReadOnlySet<int> tags, bool blankLayout)
    {
        if (tags.Contains(4) || ContainsAny(title, "英文", "英语", "单词", "短语")) return "english";
        if (ContainsAny(title, "九宫")) return "nine";
        if (ContainsAny(title, "回宫")) return "huigong";
        if (ContainsAny(title, "田格", "田字")) return "tian";
        if (ContainsAny(title, "方格", "作文格", "横线", "脱格", "文章", "表格")) return "plain";
        if (tags.Contains(2) && !tags.Contains(1) && !tags.Contains(6)) return "pinyin";
        if (tags.Contains(3) && blankLayout) return "plain";
        return "mi";
    }

    private static bool IsVerticalLayout(string title, IReadOnlySet<int> tags)
    {
        if (ContainsAny(title, "横排", "A4横", "A3横")) return false;
        return ContainsAny(title, "竖排", "竖式") ||
               tags.Contains(7) && ContainsAny(title, "古诗", "诗词", "书法", "春联");
    }

    private static bool IsPoetryLike(string title)
    {
        return ContainsAny(title, "古诗", "诗词", "春联");
    }

    private static bool ShouldGroupByWord(string title, IReadOnlySet<int> tags)
    {
        return tags.Contains(6) ||
               tags.Contains(10) ||
               ContainsAny(title, "组词", "词语", "成语", "单词", "短语", "句子", "文章", "听写", "HSK");
    }

    private static string ResolveMode(string title, IReadOnlySet<int> tags, bool blankLayout)
    {
        if (blankLayout ||
            ContainsAny(title, "临写", "抄写", "听写", "脱格", "横线", "测试", "试卷"))
            return "copy";

        return "trace";
    }

    private static int ResolveTraceCount(
        string title,
        string mode,
        bool blankLayout,
        int repeats,
        bool showStrokeOrder)
    {
        if (blankLayout || mode == "copy") return 0;
        if (showStrokeOrder) return Math.Max(0, repeats - 1);
        if (ContainsAny(title, "描红", "描写", "描临", "描字")) return 3;
        return 2;
    }

    private static (int? Columns, int? Rows) ResolveBlankLayout(
        string title,
        IReadOnlySet<int> tags,
        bool blankLayout,
        bool vertical)
    {
        if (!blankLayout) return (null, null);
        if (tags.Contains(15) || ContainsAny(title, "封面")) return (null, null);
        if (ContainsAny(title, "作文格", "文章3行")) return (20, 20);
        if (ContainsAny(title, "横线", "脱格")) return (1, 24);
        if (tags.Contains(11)) return (12, 18);
        if (tags.Contains(12)) return (8, 12);
        if (tags.Contains(3)) return (10, 16);
        return vertical ? (8, 16) : (12, 16);
    }

    private static bool ShouldShowPinyin(string title, IReadOnlySet<int> tags)
    {
        return tags.Contains(2) || ContainsAny(title, "拼音", "注音");
    }

    private static bool ShouldShowStrokeOrder(string title, IReadOnlySet<int> tags)
    {
        return tags.Contains(5) || ContainsAny(title, "笔顺", "书写顺序");
    }

    private static double ResolveGridSize(string title, IReadOnlySet<int> tags, string grid, bool blankLayout)
    {
        if (ContainsAny(title, "8mm", "8毫米")) return 8;
        if (ContainsAny(title, "10mm", "10毫米")) return 10;
        if (ContainsAny(title, "加大")) return 18;
        if (ContainsAny(title, "加宽")) return 16;
        if (grid == "english") return 9;
        if (grid == "pinyin") return 11;
        if (blankLayout) return 10;
        if (tags.Contains(7)) return 18;
        return 14;
    }

    private static int ResolveRepeats(
        string title,
        IReadOnlySet<int> tags,
        bool groupByWord,
        bool showStrokeOrder)
    {
        if (groupByWord) return 1;
        if (tags.Contains(3)) return 8;
        if (showStrokeOrder) return 8;
        if (ContainsAny(title, "单字", "每字一页")) return 8;
        if (ContainsAny(title, "两列", "2列", "双列")) return 4;
        return 5;
    }

    private static int? ResolveCellsPerLine(
        string title,
        IReadOnlySet<int> tags,
        string grid,
        bool groupByWord,
        bool blankLayout,
        bool showStrokeOrder)
    {
        if (blankLayout) return null;
        if (grid == "english") return 24;
        if (grid == "pinyin") return 12;
        if (showStrokeOrder && !groupByWord) return 8;
        if (ContainsAny(title, "两列", "2列", "双列")) return 10;
        if (groupByWord) return tags.Contains(10) ? 16 : 12;
        if (tags.Contains(7)) return 10;
        return 15;
    }

    private static string ResolveBackground(string title, IReadOnlySet<int> tags)
    {
        if (ContainsAny(title, "春联", "红纸")) return "redgrid";
        if (tags.Contains(7) || ContainsAny(title, "书法", "古诗", "宣纸")) return "ricepaper";
        if (tags.Contains(4) || ContainsAny(title, "英文", "英语")) return "letter";
        return "plain";
    }

    private static string ResolveGridColor(string title, IReadOnlySet<int> tags)
    {
        if (tags.Contains(4)) return "#778A99";
        if (tags.Contains(7)) return "#B04A3F";
        if (ContainsAny(title, "横线", "脱格")) return "#9BA7B0";
        return "#29A86C";
    }

    private static string ResolvePageSize(string title)
    {
        if (ContainsAny(title, "A4横", "A3横")) return "a4Landscape";
        if (ContainsAny(title, "A3")) return "a3Portrait";
        return "a4Portrait";
    }

    private static double ResolvePageMargin(string title, IReadOnlySet<int> tags, bool blankLayout)
    {
        if (tags.Contains(15)) return 20;
        if (blankLayout) return 10;
        if (tags.Contains(7)) return 16;
        if (ContainsAny(title, "加大", "加宽")) return 12;
        return 14;
    }

    private static string? ResolveFontFamily(string title, IReadOnlySet<int> tags)
    {
        if (tags.Contains(4)) return "Arial";
        if (tags.Contains(7)) return "KaiTi";
        return null;
    }

    private static string ResolveHeaderPreset(
        string title,
        IReadOnlySet<int> tags,
        bool blankLayout,
        bool poemHeader)
    {
        if (tags.Contains(15) || ContainsAny(title, "封面")) return "none";
        if (poemHeader) return "poem";
        if (blankLayout && ContainsAny(title, "作文格", "横线", "运算纸")) return "fields";
        return "titleAndFields";
    }

    private static string ResolveSampleText(string title, IReadOnlySet<int> tags)
    {
        if (tags.Contains(4) || ContainsAny(title, "英文", "英语", "单词"))
            return ContainsAny(title, "句子")
                ? "This is my school. I like reading books."
                : "cat dog pig cow sheep";
        if (tags.Contains(3) || ContainsAny(title, "数字"))
            return "1 2 3 4 5 6 7 8 9 0";
        if (tags.Contains(14) || ContainsAny(title, "姓名", "名字"))
            return "王小明";
        if (ContainsAny(title, "拼音") && !tags.Contains(1))
            return "bā bō mī fū dā";
        if (tags.Contains(7) || ContainsAny(title, "古诗", "诗词", "书法"))
            return "床前明月光，疑是地上霜。举头望明月，低头思故乡。";
        if (tags.Contains(10) || ContainsAny(title, "文章"))
            return "春天来了，小草发芽，花儿开放。";
        if (tags.Contains(6) || ContainsAny(title, "组词", "词语", "成语"))
            return "春天 花朵 朋友 竹林";
        return "春风化雨";
    }

    private static string RequiredString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static ModuleDefaults? ReadDefaultOverrides(JsonElement item, string catalogPath, int order)
    {
        if (!item.TryGetProperty("defaults", out var defaults) ||
            defaults.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;

        try
        {
            return defaults.Deserialize(ZitieJsonContext.Default.ModuleDefaults);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"样式模板默认值覆盖解析失败，已跳过：{catalogPath} #{order + 1}", exception);
            return null;
        }
    }

    private static void ApplyDefaultOverrides(ModuleDefaults target, ModuleDefaults? overrides)
    {
        if (overrides is null) return;

        target.BlankContentLayout = overrides.BlankContentLayout ?? target.BlankContentLayout;
        target.PracticeLayout = overrides.PracticeLayout ?? target.PracticeLayout;
        target.FillContentAreaWithBlankCells = overrides.FillContentAreaWithBlankCells ?? target.FillContentAreaWithBlankCells;
        target.LayoutColumns = overrides.LayoutColumns ?? target.LayoutColumns;
        target.LayoutRows = overrides.LayoutRows ?? target.LayoutRows;
        target.Grid = overrides.Grid ?? target.Grid;
        target.GridSize = overrides.GridSize ?? target.GridSize;
        target.GridGap = overrides.GridGap ?? target.GridGap;
        target.GroupGap = overrides.GroupGap ?? target.GroupGap;
        target.HollowGlyph = overrides.HollowGlyph ?? target.HollowGlyph;
        target.Mode = overrides.Mode ?? target.Mode;
        target.GroupByWord = overrides.GroupByWord ?? target.GroupByWord;
        target.ShowPinyin = overrides.ShowPinyin ?? target.ShowPinyin;
        target.PinyinOnly = overrides.PinyinOnly ?? target.PinyinOnly;
        target.ShowStrokeOrder = overrides.ShowStrokeOrder ?? target.ShowStrokeOrder;
        target.Vertical = overrides.Vertical ?? target.Vertical;
        target.ShowPoemHeader = overrides.ShowPoemHeader ?? target.ShowPoemHeader;
        target.FrameBorder = overrides.FrameBorder ?? target.FrameBorder;
        target.Background = overrides.Background ?? target.Background;
        target.BackgroundColor = overrides.BackgroundColor ?? target.BackgroundColor;
        target.BackgroundLineColor = overrides.BackgroundLineColor ?? target.BackgroundLineColor;
        target.BackgroundLineSpacing = overrides.BackgroundLineSpacing ?? target.BackgroundLineSpacing;
        target.BackgroundArtwork = overrides.BackgroundArtwork ?? target.BackgroundArtwork;
        target.Author = overrides.Author ?? target.Author;
        target.Dynasty = overrides.Dynasty ?? target.Dynasty;
        target.Repeats = overrides.Repeats ?? target.Repeats;
        target.TraceCount = overrides.TraceCount ?? target.TraceCount;
        target.Title = overrides.Title ?? target.Title;
        target.TraceColor = overrides.TraceColor ?? target.TraceColor;
        target.TraceIntensity = overrides.TraceIntensity ?? target.TraceIntensity;
        target.CellsPerLine = overrides.CellsPerLine ?? target.CellsPerLine;
        target.BlankCellLineCount = overrides.BlankCellLineCount ?? target.BlankCellLineCount;
        target.GridColor = overrides.GridColor ?? target.GridColor;
        target.TextColor = overrides.TextColor ?? target.TextColor;
        target.PageSize = overrides.PageSize ?? target.PageSize;
        target.PageMargin = overrides.PageMargin ?? target.PageMargin;
        target.PageMarginTop = overrides.PageMarginTop ?? target.PageMarginTop;
        target.PageMarginBottom = overrides.PageMarginBottom ?? target.PageMarginBottom;
        target.PageMarginLeft = overrides.PageMarginLeft ?? target.PageMarginLeft;
        target.PageMarginRight = overrides.PageMarginRight ?? target.PageMarginRight;
        target.FontFamily = overrides.FontFamily ?? target.FontFamily;
        target.HeaderPreset = overrides.HeaderPreset ?? target.HeaderPreset;
        target.HeaderText = overrides.HeaderText ?? target.HeaderText;
        target.Text = overrides.Text ?? target.Text;
    }

    private static IReadOnlySet<int> ReadTags(JsonElement item)
    {
        if (!item.TryGetProperty("tags", out var tags) ||
            tags.ValueKind != JsonValueKind.Array)
            return new HashSet<int>();

        return tags.EnumerateArray()
            .Where(static tag => tag.ValueKind == JsonValueKind.Number && tag.TryGetInt32(out _))
            .Select(static tag => tag.GetInt32())
            .ToHashSet();
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term => text.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private static void LoadDirectory(
        string directory,
        ICollection<ModuleDefinition> modules,
        int orderStart,
        bool isUserDirectory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

        try
        {
            foreach (var package in Directory
                         .EnumerateFiles(directory, "*.zi", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var moduleDirectory = Path.Combine(directory, Path.GetFileNameWithoutExtension(package));
                if (!Directory.Exists(moduleDirectory))
                    TryExtractModulePackage(package, moduleDirectory);
            }

            var order = orderStart;
            foreach (var moduleDirectory in Directory
                         .EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                LoadModuleDirectory(moduleDirectory, modules, order++, isUserDirectory);
            }
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法扫描模板目录，已跳过：{directory}", exception);
        }
    }

    private static void TryExtractModulePackage(string packagePath, string moduleDirectory)
    {
        var parentDirectory = Path.GetDirectoryName(moduleDirectory);
        if (string.IsNullOrWhiteSpace(parentDirectory)) return;

        var temporaryDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(moduleDirectory)}.extracting-{Guid.NewGuid():N}");

        try
        {
            ZipFile.ExtractToDirectory(packagePath, temporaryDirectory);

            if (!Directory.Exists(moduleDirectory))
                Directory.Move(temporaryDirectory, moduleDirectory);

            ZitieLogging.Info($"模板包已解压：{packagePath} -> {moduleDirectory}");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"模板包解压失败，已跳过：{packagePath}", exception);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                try
                {
                    Directory.Delete(temporaryDirectory, recursive: true);
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"无法清理模板解压临时目录：{temporaryDirectory}", exception);
                }
            }
        }
    }

    private static void LoadModuleDirectory(
        string moduleDirectory,
        ICollection<ModuleDefinition> modules,
        int displayOrder,
        bool isUserDirectory)
    {
        try
        {
            var modulePath = Directory
                .EnumerateFiles(moduleDirectory, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileName(path),
                    ModuleFileName,
                    StringComparison.OrdinalIgnoreCase));
            if (modulePath is null)
            {
                ZitieLogging.Warn($"模板目录缺少 {ModuleFileName}，已跳过：{moduleDirectory}");
                return;
            }

            var module = YamlResourceSerializer.DeserializeText<ModuleDefinition>(File.ReadAllText(modulePath));
            if (module is null || string.IsNullOrWhiteSpace(module.Id))
            {
                ZitieLogging.Warn($"模板目录模块缺少 Id，已跳过：{moduleDirectory}");
                return;
            }

            module.SourcePath = moduleDirectory;
            module.DisplayOrder = displayOrder;
            module.Assets = ReadAssets(moduleDirectory, modulePath);
            if (module.Categories.Count == 0)
                module.Categories = [ResolveDirectoryCategory(module, isUserDirectory)];
            modules.Add(module);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"模板目录解析失败，已跳过：{moduleDirectory}", exception);
        }
    }

    private static string ResolveDirectoryCategory(ModuleDefinition module, bool isUserDirectory)
    {
        if (!string.IsNullOrWhiteSpace(module.Category)) return module.Category.Trim();
        if (isUserDirectory) return "自定义";

        var grid = module.Defaults.Grid?.Trim();
        if (string.Equals(grid, "english", StringComparison.OrdinalIgnoreCase)) return "英文";
        if (string.Equals(grid, "pinyin", StringComparison.OrdinalIgnoreCase)) return "拼音";
        if (module.Id.Contains("mengxue", StringComparison.OrdinalIgnoreCase)) return "蒙学";
        if (!string.IsNullOrWhiteSpace(module.Defaults.BackgroundArtwork)) return "主题";
        if (module.Defaults.Vertical == true || module.Id.Contains("poem", StringComparison.OrdinalIgnoreCase))
            return "诗词";
        return "基础";
    }

    private static IReadOnlyDictionary<string, byte[]> ReadAssets(string moduleDirectory, string modulePath)
    {
        var assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(moduleDirectory, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(path, modulePath, StringComparison.OrdinalIgnoreCase)) continue;

            var key = ModuleDefinition.NormalizePackagePath(Path.GetRelativePath(moduleDirectory, path));
            if (key is not null) assets[key] = File.ReadAllBytes(path);
        }

        return assets;
    }
}
