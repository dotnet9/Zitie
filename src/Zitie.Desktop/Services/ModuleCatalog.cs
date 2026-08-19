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
///     扫描输出目录 modules/*.json 构建模块目录；无效文件跳过并记录日志。
/// </summary>
public sealed class ModuleCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ModuleCatalog()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "modules");
        var modules = new List<ModuleDefinition>();

        if (Directory.Exists(directory))
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    var module = JsonSerializer.Deserialize<ModuleDefinition>(
                        File.ReadAllText(file), JsonOptions);
                    if (module is not null && !string.IsNullOrWhiteSpace(module.Id))
                        modules.Add(module);
                    else
                        ZitieLogging.Warn($"模块文件缺少 Id，已跳过：{file}");
                }
                catch (Exception exception)
                {
                    ZitieLogging.Warn($"模块文件解析失败，已跳过：{file}", exception);
                }

        Modules = modules
            .OrderBy(module => module.Enabled ? 0 : 1)
            .ThenBy(module => module.Name, StringComparer.CurrentCulture)
            .ToList();

        ZitieLogging.Info($"模块目录加载完成：{Modules.Count} 个模块（{directory}）");
    }

    public IReadOnlyList<ModuleDefinition> Modules { get; }

    public ModuleDefinition? Find(string? id)
    {
        return string.IsNullOrEmpty(id)
            ? null
            : Modules.FirstOrDefault(module => module.Id == id);
    }
}
