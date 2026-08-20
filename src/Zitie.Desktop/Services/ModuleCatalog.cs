using System.IO;

namespace Zitie.Desktop.Services;

/// <summary>
///     模块定义：YAML 配方文件的数据形态。文件位于输出目录 resources/modules/ 下。
/// </summary>
public sealed record ModuleDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>模块处理器类型，决定编辑器形态；首版仅 customText。</summary>
    public string Kind { get; set; } = "customText";

    /// <summary>是否可用；false 时卡片显示“即将上线”。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>模块默认参数：grid / mode / repeats / traceCount / title / text。</summary>
    public ModuleDefaults Defaults { get; set; } = new();
}

public sealed record ModuleDefaults
{
    public string? Grid { get; set; }

    public double? GridSize { get; set; }

    public double? GridGap { get; set; }

    public bool? HollowGlyph { get; set; }

    public string? Mode { get; set; }

    public bool? GroupByWord { get; set; }

    public bool? ShowPinyin { get; set; }

    public bool? PinyinOnly { get; set; }

    public bool? Vertical { get; set; }

    public bool? ShowPoemHeader { get; set; }

    public bool? FrameBorder { get; set; }

    public string? Background { get; set; }

    public string? BackgroundColor { get; set; }

    public string? BackgroundLineColor { get; set; }

    public double? BackgroundLineSpacing { get; set; }

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

    public string? FontFamily { get; set; }

    public string? HeaderPreset { get; set; }

    public string? HeaderText { get; set; }

    public string? Text { get; set; }
}

/// <summary>
///     扫描内置与用户模板目录构建模块目录；用户模板可用相同 Id 覆盖内置模板。
/// </summary>
public sealed class ModuleCatalog
{
    public ModuleCatalog()
    {
        BuiltInDirectory = ResourcePaths.Modules;
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        UserDirectory = Path.Combine(localApplicationData, "Zitie", "modules");

        Reload();
    }

    public string BuiltInDirectory { get; }

    public string UserDirectory { get; }

    public IReadOnlyList<ModuleDefinition> Modules { get; private set; } = Array.Empty<ModuleDefinition>();

    public void Reload()
    {
        var modules = new List<ModuleDefinition>();
        LoadDirectory(BuiltInDirectory, modules);
        LoadDirectory(UserDirectory, modules);

        Modules = modules
            .GroupBy(module => module.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(module => module.Enabled ? 0 : 1)
            .ThenBy(module => module.Name, StringComparer.CurrentCulture)
            .ToList();

        ZitieLogging.Info($"模块目录加载完成：{Modules.Count} 个模块（内置：{BuiltInDirectory}；用户：{UserDirectory}）");
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

    private static void LoadDirectory(string directory, ICollection<ModuleDefinition> modules)
    {
        if (!Directory.Exists(directory)) return;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*.yml");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法扫描模板目录，已跳过：{directory}", exception);
            return;
        }

        foreach (var file in files)
            try
            {
                var module = YamlResourceSerializer.DeserializeFile<ModuleDefinition>(file);
                if (module is not null && !string.IsNullOrWhiteSpace(module.Id))
                    modules.Add(module);
                else
                    ZitieLogging.Warn($"模块文件缺少 Id，已跳过：{file}");
            }
            catch (Exception exception)
            {
                ZitieLogging.Warn($"模块文件解析失败，已跳过：{file}", exception);
            }
    }
}
