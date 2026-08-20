using System.IO;
using System.Text.Json;

namespace Zitie.Desktop.Services;

/// <summary>
///     模块定义：JSON 配方文件的数据形态。文件位于输出目录 modules/ 下，用户可自由增删替换。
/// </summary>
public sealed record ModuleDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>模块处理器类型，决定编辑器形态；首版仅 customText。</summary>
    public string Kind { get; init; } = "customText";

    /// <summary>是否可用；false 时卡片显示“即将上线”。</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>模块默认参数：grid / mode / repeats / traceCount / title / text。</summary>
    public JsonElement? Defaults { get; init; }
}

/// <summary>
///     扫描内置与用户模板目录构建模块目录；用户模板可用相同 Id 覆盖内置模板。
/// </summary>
public sealed class ModuleCatalog
{
    public ModuleCatalog()
    {
        BuiltInDirectory = Path.Combine(AppContext.BaseDirectory, "modules");
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
            files = Directory.EnumerateFiles(directory, "*.json");
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"无法扫描模板目录，已跳过：{directory}", exception);
            return;
        }

        foreach (var file in files)
            try
            {
                var module = JsonSerializer.Deserialize<ModuleDefinition>(
                    File.ReadAllText(file), ZitieJsonContext.Default.ModuleDefinition);
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
