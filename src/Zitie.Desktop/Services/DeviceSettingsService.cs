using System.Globalization;
using Avalonia;
using YamlDotNet.RepresentationModel;

namespace Zitie.Desktop.Services;

public enum UiFontSizeMode
{
    Small,
    Medium,
    Large
}

public sealed class DeviceSettingsService
{
    private static readonly IReadOnlyList<UiFontSizeOption> FontSizeOptions =
    [
        new("小", UiFontSizeMode.Small, 0.92),
        new("中", UiFontSizeMode.Medium, 1.0),
        new("大", UiFontSizeMode.Large, 1.16)
    ];

    public DeviceSettingsService()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        SettingsPath = Path.Combine(localApplicationData, "Zitie", "settings.yml");
        FontSizeMode = LoadFontSizeMode();
        ApplyFontSizeResources();
    }

    public string SettingsPath { get; }

    public UiFontSizeMode FontSizeMode { get; private set; }

    public IReadOnlyList<string> FontSizeLabels => FontSizeOptions.Select(static option => option.Label).ToArray();

    public int FontSizeIndex
    {
        get
        {
            var index = FontSizeOptions
                .Select(static (option, index) => (option, index))
                .FirstOrDefault(item => item.option.Mode == FontSizeMode).index;
            return Math.Clamp(index, 0, FontSizeOptions.Count - 1);
        }
        set
        {
            var index = Math.Clamp(value, 0, FontSizeOptions.Count - 1);
            var mode = FontSizeOptions[index].Mode;
            if (FontSizeMode == mode) return;

            FontSizeMode = mode;
            ApplyFontSizeResources();
            Save();
        }
    }

    private UiFontSizeMode LoadFontSizeMode()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return UiFontSizeMode.Medium;

            using var reader = File.OpenText(SettingsPath);
            var yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.Count == 0 ||
                yaml.Documents[0].RootNode is not YamlMappingNode map)
                return UiFontSizeMode.Medium;

            foreach (var (key, value) in map.Children)
                if (key is YamlScalarNode { Value: "fontSize" } &&
                    value is YamlScalarNode scalar)
                    return ParseMode(scalar.Value);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"设备设置读取失败，已使用默认设置：{SettingsPath}", exception);
        }

        return UiFontSizeMode.Medium;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllLines(SettingsPath, [$"fontSize: {ModeName(FontSizeMode)}"]);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"设备设置保存失败：{SettingsPath}", exception);
        }
    }

    private void ApplyFontSizeResources()
    {
        var scale = FontSizeOptions.First(option => option.Mode == FontSizeMode).Scale;
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        resources["ZitieUiFontSizeMicro"] = Size(8, scale);
        resources["ZitieUiFontSizeTiny"] = Size(11, scale);
        resources["ZitieUiFontSizeSmall"] = Size(12, scale);
        resources["ZitieUiFontSizeBase"] = Size(13, scale);
        resources["ZitieUiFontSizeMedium"] = Size(14, scale);
        resources["ZitieUiFontSizeLarge"] = Size(14.5, scale);
        resources["ZitieUiFontSizeTitle"] = Size(26, scale);
        resources["ZitieUiFontSizeSeal"] = Size(22, scale);
    }

    private static double Size(double value, double scale)
    {
        return Math.Round(value * scale, 1, MidpointRounding.AwayFromZero);
    }

    private static UiFontSizeMode ParseMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "small" or "小" => UiFontSizeMode.Small,
            "large" or "大" => UiFontSizeMode.Large,
            _ => UiFontSizeMode.Medium
        };
    }

    private static string ModeName(UiFontSizeMode mode)
    {
        return mode switch
        {
            UiFontSizeMode.Small => "small",
            UiFontSizeMode.Large => "large",
            _ => "medium"
        };
    }

    private sealed record UiFontSizeOption(string Label, UiFontSizeMode Mode, double Scale);
}
