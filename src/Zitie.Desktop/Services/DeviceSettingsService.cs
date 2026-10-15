using System.Globalization;
using Avalonia;
using YamlDotNet.RepresentationModel;

namespace Zitie.Desktop.Services;

/// <summary>保存设备界面偏好；保留不认识的 YAML 字段，更新字号时不会丢失主题或语言。</summary>
public sealed class DeviceSettingsService
{
    public const double MinimumUiFontSize = 12;
    public const double MaximumUiFontSize = 30;
    public const double DefaultUiFontSize = 13;
    private YamlMappingNode _settings = new();

    public DeviceSettingsService() : this(CreateDefaultSettingsPath()) { }

    public DeviceSettingsService(string settingsPath)
    {
        SettingsPath = settingsPath;
        Load();
        UiFontSize = ParseFontSize(Read("fontSize", "13"));
        ApplyFontSizeResources();
    }

    public string SettingsPath { get; }
    public double UiFontSize { get; private set; }
    public string ThemeMode { get => Read("themeMode", "System"); set => Write("themeMode", value); }
    public string Palette { get => Read("palette", "DotNet"); set => Write("palette", value); }
    public string Language { get => Read("language", "zh-CN") == "en-US" ? "en-US" : "zh-CN"; set => Write("language", value); }

    public double FontSize
    {
        get => UiFontSize;
        set
        {
            var fontSize = NormalizeFontSize(value);
            if (Math.Abs(UiFontSize - fontSize) < 0.01) return;
            UiFontSize = fontSize;
            ApplyFontSizeResources();
            Write("fontSize", fontSize.ToString("0.#", CultureInfo.InvariantCulture));
        }
    }

    private static string CreateDefaultSettingsPath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(string.IsNullOrWhiteSpace(local) ? AppContext.BaseDirectory : local, "Zitie", "settings.yml");
    }

    private string Read(string key, string fallback)
        => _settings.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlScalarNode { Value: { } text } ? text : fallback;

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            using var reader = File.OpenText(SettingsPath);
            var yaml = new YamlStream(); yaml.Load(reader);
            if (yaml.Documents.FirstOrDefault()?.RootNode is YamlMappingNode map) _settings = map;
        }
        catch (Exception exception) { ZitieLogging.Warn(AppText.Format(I18n.Language.SettingsLoadFailed, SettingsPath), exception); }
    }

    private void Write(string key, string value)
    {
        _settings.Children[new YamlScalarNode(key)] = new YamlScalarNode(value);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(SettingsPath))!);
            using var writer = File.CreateText(SettingsPath);
            new YamlStream(new YamlDocument(_settings)).Save(writer, false);
        }
        catch (Exception exception) { ZitieLogging.Warn(AppText.Format(I18n.Language.SettingsSaveFailed, SettingsPath), exception); }
    }

    private void ApplyFontSizeResources()
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;
        foreach (var (suffix, size) in new[] { ("Micro",8d),("Tiny",11d),("Small",12d),("Base",13d),("Medium",14d),("Large",14.5d),("Title",26d),("Seal",22d) })
            resources["ZitieUiFontSize" + suffix] = Math.Round(size * UiFontSize / DefaultUiFontSize, 1, MidpointRounding.AwayFromZero);
    }

    private static double ParseFontSize(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? NormalizeFontSize(number) : DefaultUiFontSize;

    private static double NormalizeFontSize(double value)
        => double.IsFinite(value) ? Math.Round(Math.Clamp(value, MinimumUiFontSize, MaximumUiFontSize), 1, MidpointRounding.AwayFromZero) : DefaultUiFontSize;
}
