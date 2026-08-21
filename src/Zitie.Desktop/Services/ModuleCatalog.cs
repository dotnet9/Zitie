using System.IO;
using System.IO.Compression;
using System.Text;

namespace Zitie.Desktop.Services;

/// <summary>
///     模块定义：.zi 模板包内 module.yml 的数据形态。素材从同一个包内的 assets/ 读取。
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

    /// <summary>模板包路径，用于诊断与后续导出。</summary>
    public string? SourcePath { get; set; }

    /// <summary>模板包内资源缓存；键使用 zip 内的规范化相对路径。</summary>
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
    public bool? BlankContentLayout { get; set; }

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
///     扫描内置与用户模板目录构建模块目录；用户模板可用相同 Id 覆盖内置模板。
/// </summary>
public sealed class ModuleCatalog
{
    private const string ModuleFileName = "module.yml";

    public ModuleCatalog()
        : this(ResourcePaths.Modules, ResolveDefaultUserDirectory())
    {
    }

    public ModuleCatalog(string builtInDirectory, string userDirectory)
    {
        BuiltInDirectory = builtInDirectory;
        UserDirectory = userDirectory;

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

    private static string ResolveDefaultUserDirectory()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        return Path.Combine(localApplicationData, "Zitie", "modules");
    }

    private static void LoadDirectory(string directory, ICollection<ModuleDefinition> modules)
    {
        if (!Directory.Exists(directory)) return;

        try
        {
            foreach (var package in Directory.EnumerateFiles(directory, "*.zi", SearchOption.TopDirectoryOnly))
                LoadModulePackage(package, modules);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法扫描模板目录，已跳过：{directory}", exception);
        }
    }

    private static void LoadModulePackage(string packagePath, ICollection<ModuleDefinition> modules)
    {
        try
        {
            using var stream = File.OpenRead(packagePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var moduleEntry = archive.Entries.FirstOrDefault(static entry =>
                string.Equals(
                    ModuleDefinition.NormalizePackagePath(entry.FullName),
                    ModuleFileName,
                    StringComparison.OrdinalIgnoreCase));
            if (moduleEntry is null)
            {
                ZitieLogging.Warn($"模板包缺少 {ModuleFileName}，已跳过：{packagePath}");
                return;
            }

            var module = YamlResourceSerializer.DeserializeText<ModuleDefinition>(ReadEntryText(moduleEntry));
            if (module is null || string.IsNullOrWhiteSpace(module.Id))
            {
                ZitieLogging.Warn($"模板包模块缺少 Id，已跳过：{packagePath}");
                return;
            }

            module.SourcePath = packagePath;
            module.Assets = ReadAssets(archive);
            modules.Add(module);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"模板包解析失败，已跳过：{packagePath}", exception);
        }
    }

    private static IReadOnlyDictionary<string, byte[]> ReadAssets(ZipArchive archive)
    {
        var assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            var key = ModuleDefinition.NormalizePackagePath(entry.FullName);
            if (string.IsNullOrWhiteSpace(entry.Name) ||
                key is null ||
                string.Equals(key, ModuleFileName, StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            using var entryStream = entry.Open();
            using var memory = new MemoryStream();
            entryStream.CopyTo(memory);
            assets[key] = memory.ToArray();
        }

        return assets;
    }

    private static string ReadEntryText(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
