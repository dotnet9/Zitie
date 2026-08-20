using System.Globalization;
using Avalonia;
using YamlDotNet.RepresentationModel;

namespace Zitie.Desktop.Services;

public sealed class DeviceSettingsService
{
    public const double MinimumUiFontSize = 12;
    public const double MaximumUiFontSize = 30;
    public const double DefaultUiFontSize = 13;

    public DeviceSettingsService()
        : this(CreateDefaultSettingsPath())
    {
    }

    public DeviceSettingsService(string settingsPath)
    {
        SettingsPath = settingsPath;
        UiFontSize = LoadUiFontSize();
        ApplyFontSizeResources();
    }

    public string SettingsPath { get; }

    public double UiFontSize { get; private set; }

    public double FontSize
    {
        get => UiFontSize;
        set
        {
            var fontSize = NormalizeFontSize(value);
            if (Math.Abs(UiFontSize - fontSize) < 0.01) return;

            UiFontSize = fontSize;
            ApplyFontSizeResources();
            Save();
        }
    }

    private static string CreateDefaultSettingsPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        return Path.Combine(localApplicationData, "Zitie", "settings.yml");
    }

    private double LoadUiFontSize()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return DefaultUiFontSize;

            using var reader = File.OpenText(SettingsPath);
            var yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.Count == 0 ||
                yaml.Documents[0].RootNode is not YamlMappingNode map)
                return DefaultUiFontSize;

            foreach (var (key, value) in map.Children)
                if (key is YamlScalarNode { Value: "fontSize" } &&
                    value is YamlScalarNode scalar)
                    return ParseFontSize(scalar.Value);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"设备设置读取失败，已使用默认设置：{SettingsPath}", exception);
        }

        return DefaultUiFontSize;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllLines(SettingsPath,
                [$"fontSize: {UiFontSize.ToString("0.#", CultureInfo.InvariantCulture)}"]);
        }
        catch (Exception exception)
        {
            ZitieLogging.Warn($"设备设置保存失败：{SettingsPath}", exception);
        }
    }

    private void ApplyFontSizeResources()
    {
        var scale = UiFontSize / DefaultUiFontSize;
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

    private static double ParseFontSize(string? value)
    {
        var normalized = value?.Trim();
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize) ||
            double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out fontSize))
            return NormalizeFontSize(fontSize);

        return normalized?.ToLowerInvariant() switch
        {
            "small" or "小" => 12,
            "large" or "大" => 15,
            _ => DefaultUiFontSize
        };
    }

    private static double NormalizeFontSize(double value)
    {
        return Math.Round(Math.Clamp(value, MinimumUiFontSize, MaximumUiFontSize), 1, MidpointRounding.AwayFromZero);
    }

    private static double Size(double value, double scale)
    {
        return Math.Round(value * scale, 1, MidpointRounding.AwayFromZero);
    }
}
