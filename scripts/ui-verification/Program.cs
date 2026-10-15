using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Ursa.Controls;
using System.Text.Json;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Core.UpdateChecking;
using CodeWF.Toolkit.Lang;
using Prism.Ioc;
using Prism.Regions;
using Zitie.Desktop;
using Zitie.Desktop.I18n;
using Zitie.Desktop.Services;
using Zitie.Desktop.ViewModels;
using Zitie.Desktop.Views;

var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/ui-verification");
Directory.CreateDirectory(output);
var settings = new DeviceSettingsService(Path.Combine(output, "fixture", "settings.yml"));
var lifetime = new ClassicDesktopStyleApplicationLifetime();
AppBuilder.Configure(() => new App(settings)).UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithLifetime(lifetime);
int assertions = 0;
var application = (App)Application.Current!;
var window = (MainWindow)lifetime.MainWindow!;
window.Show();
var model = (MainWindowViewModel)window.DataContext!;
Until(() => window.GetVisualDescendants().OfType<ModuleGalleryView>().FirstOrDefault()?.DataContext is ModuleGalleryViewModel { IsLoading: false });
var gallery = (ModuleGalleryViewModel)window.GetVisualDescendants().OfType<ModuleGalleryView>().Single().DataContext!;
Check(gallery.Modules.Count >= 223, "Actual complete template catalog is loaded");
Check(ModuleSearch.ParseTerms("括号组词").SequenceEqual(new[] { "括号", "组词" }), "Published dictionary supports actual Chinese tokenization without fallback");
using (var stream = File.Create(Path.Combine(output, "catalog.json")))
using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
{
    writer.WriteStartObject(); writer.WriteStartArray("templates");
    foreach (var item in gallery.Modules)
    {
        var module = item.Module;
        writer.WriteStartObject(); writer.WriteString("id", module.Id); writer.WriteString("name", module.Name);
        writer.WriteString("description", module.Description); writer.WriteString("kind", module.Kind); writer.WriteBoolean("enabled", module.Enabled);
        writer.WriteString("image", File.Exists(module.PreviewImagePath) ? Path.GetFileName(module.PreviewImagePath) : "");
        writer.WriteString("text", module.Defaults.Text); writer.WriteString("grid", module.Defaults.Grid); writer.WriteString("mode", module.Defaults.Mode);
        writer.WriteStartArray("categories"); foreach (var category in module.Categories) writer.WriteStringValue(category); writer.WriteEndArray(); writer.WriteEndObject();
    }
    writer.WriteEndArray(); writer.WriteStartArray("contents");
    foreach (var entry in application.Container.Resolve<TextCatalog>().Entries)
    {
        writer.WriteStartObject(); writer.WriteString("title", entry.Title); writer.WriteString("body", entry.Body);
        writer.WriteString("subject", entry.Subject); writer.WriteString("edition", entry.Edition); writer.WriteString("grade", entry.Grade);
        writer.WriteString("semester", entry.Semester); writer.WriteString("unit", entry.Unit); writer.WriteString("path", entry.DisplayPath);
        writer.WriteString("dynasty", entry.Dynasty); writer.WriteString("author", entry.Author); writer.WriteEndObject();
    }
    writer.WriteEndArray(); writer.WriteStartObject("pinyin");
    var pinyin = application.Container.Resolve<PinyinCatalog>().PinyinByGlyph;
    var glyphs = gallery.Modules.SelectMany(item => item.Module.Defaults.Text ?? "").Concat(application.Container.Resolve<TextCatalog>().Entries.SelectMany(entry => entry.Body)).Distinct();
    foreach (var glyph in glyphs) if (pinyin.TryGetValue(glyph.ToString(), out var syllable)) writer.WriteString(glyph.ToString(), syllable);
    writer.WriteEndObject(); writer.WriteEndObject();
}
Check(model.Appearance.Palettes.Count == 8 && model.Appearance.Modes.Count == 3, "Eight palettes and three independent modes");
var paletteSelector=window.GetVisualDescendants().OfType<ListBox>().Single(control=>control.Classes.Contains("title-palettes"));
Check(paletteSelector.Items.Count==8,"Title bar exposes eight palette dots");
var themeToggle=window.GetVisualDescendants().OfType<ThemeToggleButton>().Single();
model.Appearance.ThemeMode=StudioThemeMode.System;Pump();
foreach(var expected in new[]{StudioThemeMode.Light,StudioThemeMode.Dark,StudioThemeMode.System})
{
    themeToggle.GetVisualDescendants().OfType<Button>().Single(button=>button.Name=="PART_ThemeButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
    Check(model.Appearance.ThemeMode==expected && new DeviceSettingsService(settings.SettingsPath).ThemeMode==expected.ToString(),"Actual Ursa theme click persists "+expected);
}
if (args.Contains("--export-previews"))
{
    var previews = Path.Combine(output, "previews"); Directory.CreateDirectory(previews);
    foreach (var item in gallery.Modules.Where(item => !item.HasPreviewImage))
    {
        var preview = new Zitie.Avalonia.Controls.ZitiePreview { Spec = item.PreviewSpec, Pages = item.PreviewPages, Zoom = item.PreviewZoom, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var previewWindow = new Window { Width = 222, Height = 226, WindowDecorations = WindowDecorations.None, Background = Brushes.White, Content = preview };
        previewWindow.Show(); Pump(); previewWindow.CaptureRenderedFrame()!.Save(Path.Combine(previews, item.Module.Id + ".png")); previewWindow.Close();
    }
}
foreach (var palette in Enum.GetValues<StudioPalette>())
{
    paletteSelector.SelectedItem = model.Appearance.Palettes.Single(option => option.Key == palette.ToString());
    foreach (var mode in new[] { "Light", "Dark" })
    {
        model.Appearance.SelectedMode = model.Appearance.Modes.Single(option => option.Key == mode); Pump();
        Check(application.ActualThemeVariant == StudioTheme.GetVariant(palette, mode == "Dark"), "Actual theme variant matches selected mode and palette");
        Capture(window, $"gallery-{palette}-{mode}.png");
    }
}
model.Appearance.SelectedPalette = model.Appearance.Palettes.Single(option => option.Key == "DotNet");
model.Appearance.SelectedMode = model.Appearance.Modes.Single(option => option.Key == "System");
var reload = new DeviceSettingsService(settings.SettingsPath);
Check(reload.ThemeMode == "System" && reload.Palette == settings.Palette, "Appearance persists without altering content");
foreach (var culture in new[] { "en-US", "zh-CN" })
{
    var palette = settings.Palette;
    var languageButton=window.GetVisualDescendants().OfType<Button>().Single(button=>button.Flyout is MenuFlyout);
    var languageMenu=(MenuFlyout)languageButton.Flyout!;languageMenu.ShowAt(languageButton);Pump();
    var languageItem=languageMenu.ItemsSource!.Cast<AppearanceChoice>().Single(option=>option.Key==culture);
    Check(languageMenu.IsOpen && languageItem.SelectCommand is not null,"Language popup opens with the actual language catalog");
    languageItem.SelectCommand!.Execute(null);languageMenu.Hide();Pump();
    foreach(var mode in new[]{StudioThemeMode.Light,StudioThemeMode.Dark,StudioThemeMode.System})
    {
        model.Appearance.ThemeMode=mode;Pump();
        var key=mode switch{StudioThemeMode.Light=>Language.LightMode,StudioThemeMode.Dark=>Language.DarkMode,_=>Language.SystemMode};
        var themeButton=themeToggle.GetVisualDescendants().OfType<Button>().Single(button=>button.Name=="PART_ThemeButton");
        Check(ToolTip.GetTip(themeButton) is TextBlock text && text.Text==AppText.Get(key),"Actual Ursa tooltip follows "+culture+" / "+mode);
    }
    Check(I18nManager.Instance.Culture.Name == culture && settings.Language == culture, "Requested language remains selected globally and in saved preferences");
    Check(window.Title == AppText.Get(Language.WindowTitle), "Existing main window changes language");
    Check(settings.Palette == palette, "Language change retains selected palette");
    Capture(window, $"gallery-{culture}.png");
}
var originalCount = gallery.Modules.Count;
var expectedMatches = gallery.Modules.Where(item => ModuleSearch.Matches(item.Module, "nqez-945")).Select(item => item.Module.Id).ToArray();
gallery.SearchText = "nqez-945"; Until(() => gallery.FilteredModules.Count == expectedMatches.Length);
Check(gallery.FilteredModules.Select(item => item.Module.Id).SequenceEqual(expectedMatches) && expectedMatches.Contains("nqez-945"), "Actual template search preserves matching template IDs");
gallery.SearchText = ""; Until(() => gallery.FilteredModules.Count == originalCount);
model.NavigateEditorCommand.Execute();
Until(() => window.GetVisualDescendants().OfType<SheetEditorView>().Any());
var editor = (SheetEditorViewModel)window.GetVisualDescendants().OfType<SheetEditorView>().Single().DataContext!;
Until(() => editor.PageCount > 0);
for (int tab = 0; tab < 5; tab++)
{
    editor.SettingsTabIndex = tab; Pump(); Capture(window, $"editor-tab-{tab}.png");
    Check(editor.PageCount > 0, "Every parameter tab keeps the rendered sheet");
}
editor.SettingsTabIndex = 0; editor.OpenContentPickerCommand.Execute(); Pump(); Capture(window, "content-library.png");
Check(editor.IsContentPickerOpen && editor.ContentSelection.FilteredEntries.Count > 0, "Content drawer uses actual library");
editor.CloseContentPickerCommand.Execute(); editor.OpenTemplatePickerCommand.Execute(); Pump(); Capture(window, "template-picker.png");
Check(editor.IsTemplatePickerOpen && editor.TemplateChoices.Count >= 223, "Template drawer keeps actual choices");
editor.CloseTemplatePickerCommand.Execute();
var document = Path.Combine(output, "fixture", "practice.zitie.json");
editor.InputText = "ABCD"; Pump(); editor.SaveDocumentTo(document); editor.InputText = "EFGH"; editor.LoadDocumentFrom(document); Pump();
Check(editor.InputText == "ABCD", "Document save and restore use isolated fixture");
var pdf = Path.Combine(output, "fixture", "practice.pdf");editor.ExportPdfTo(pdf);
Check(File.Exists(pdf) && new FileInfo(pdf).Length > 100, "Actual PDF export creates a printable document");

var service = new FakeUpdates(output);
int opened = 0, shutdown = 0;
var aboutModel = new AboutViewModel(service, _ => opened++, () => shutdown++);
var about = new AboutWindow { DataContext = aboutModel }; about.Show(); Pump();
editor.GridKindIndex = 2; editor.PracticeModeIndex = 1; editor.BackgroundIndex = 3;
editor.HeaderPresetIndex = 3; editor.PageSizeIndex = 1; editor.TraceIntensityIndex = 5;
foreach (var culture in new[] { "en-US", "zh-CN" })
{
    model.Appearance.SelectedLanguage = model.Appearance.Languages.Single(option => option.Key == culture); Pump();
    Check(I18nManager.Instance.Culture.Name == culture && settings.Language == culture, "Existing editor keeps the requested language without selection feedback");
    Check(about.Title == AppText.Get(Language.About), "Existing about window follows global language");
    Check(!about.GetVisualDescendants().OfType<Button>().Single(button=>button.Name=="PART_MinimizeButton").IsEnabled && !about.GetVisualDescendants().OfType<Button>().Single(button=>button.Name=="PART_MaximizeRestoreButton").IsEnabled,"About respects disabled minimize and maximize capabilities");
    Check(aboutModel.PlatformText.Contains(".NET 10") && aboutModel.VersionText == "v0.2.2", "About uses actual assembly metadata");
    Check(editor.GridKindIndex == 2 && editor.PracticeModeIndex == 1 && editor.BackgroundIndex == 3 && editor.HeaderPresetIndex == 3 && editor.PageSizeIndex == 1 && editor.TraceIntensityIndex == 5, "Existing localized choices retain every selected parameter");
    editor.SettingsTabIndex = 1; Pump();
    var gridChoice = window.GetVisualDescendants().OfType<ComboBox>().Single(control => AutomationProperties.GetName(control) == AppText.Get(Language.GridType));
    Check(gridChoice.SelectionBoxItem is LocalizedChoice choice && choice.Text == AppText.Get(Language.FieldGrid), "Closed grid selector displays the current language without cached old text");
    Check(editor.StatusMessage == AppText.Format(Language.ExportedPdf, "practice.pdf") && editor.InputTextSummary == AppText.Format(Language.CharacterCount, 4), "Existing status and character summary follow the requested language");
    var exportButton = window.GetVisualDescendants().OfType<Zitie.Desktop.Controls.ZitieToolButton>().Single(control => AutomationProperties.GetName(control) == AppText.Get(Language.ExportPdf));
    var exportPosition = exportButton.TranslatePoint(default, window);
    Check(exportPosition is { } position && position.X >= 0 && position.X + exportButton.Bounds.Width <= window.Bounds.Width, "Export command remains entirely inside the English and Chinese toolbars");
    window.Width = 1000; Pump();
    var narrowPosition = exportButton.TranslatePoint(default, window);
    Check(narrowPosition is { } narrow && narrow.X >= 0 && narrow.X + exportButton.Bounds.Width <= window.Bounds.Width, "Minimum-width toolbar keeps export commands visible and document commands scrollable");
    window.Width = 1240; Pump();
    Capture(window, $"editor-layout-{culture}.png");
    Capture(about, $"about-{culture}.png");
}
Complete(aboutModel.CheckAsync());Check(aboutModel.CanDownload, "New version exposes matching download");Capture(about, "update-available.png");
Complete(aboutModel.DownloadAsync());Check(aboutModel.IsReady, "Update facade returns verified package");
Complete(aboutModel.InstallAsync());Check(service.InstallCalls == 1 && shutdown == 0, "Controlled install obeys facade exit policy");
service.Mode = "unsupported";Complete(aboutModel.CheckAsync());Check(aboutModel.NeedsReleasePage && !aboutModel.CanDownload, "Unsupported platform offers manual release page");
aboutModel.OpenReleasePageCommand.Execute();Check(opened == 1, "Release-page fallback invokes injected navigation");
service.Mode = "failure";Complete(aboutModel.CheckAsync());Check(aboutModel.StatusText.Contains("controlled failure"), "Failed check remains distinct from current version");
service.Mode = "current";Complete(aboutModel.CheckAsync());Check(!aboutModel.IsReady && !aboutModel.CanDownload, "Current version clears stale update actions");
service.Mode = "available";Complete(aboutModel.CheckAsync());service.DelayDownload = true;
var download = aboutModel.DownloadAsync();Pump();Check(aboutModel.IsDownloading, "Download exposes cancellable state");
aboutModel.CancelCommand.Execute();Complete(download);Check(!aboutModel.IsDownloading && !aboutModel.IsReady, "Cancellation preserves retryable state");
about.Close(); window.Close();
Console.WriteLine($"PASS: {assertions} assertions; screenshots in {output}");

void Check(bool condition, string description) { if (!condition) throw new InvalidOperationException(description); assertions++; }
void Pump() { Dispatcher.UIThread.RunJobs(); Thread.Sleep(30); Dispatcher.UIThread.RunJobs(); }
void Until(Func<bool> ready) { var limit = DateTime.UtcNow.AddSeconds(30); while (!ready() && DateTime.UtcNow < limit) Pump(); Check(ready(), "Asynchronous UI operation completed"); Pump(); }
void Complete(Task task) { Until(() => task.IsCompleted); task.GetAwaiter().GetResult(); Pump(); }
void Capture(Window target, string name) { Pump(); target.CaptureRenderedFrame()!.Save(Path.Combine(output, name)); }

sealed class FakeUpdates(string root) : IUpdateService
{
    public UpdateServiceOptions Options { get; } = new() { Repository = "Zitie", ProductName = "Zitie" };
    public Version CurrentVersion => new(0, 2, 2);
    public string CurrentVersionText => "0.2.2";
    public int InstallCalls;
    public string Mode = "available";
    public bool DelayDownload;
    public Task<UpdateCheckResult> CheckAsync(Version? current = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Mode switch
        {
            "current" => UpdateCheckResult.Latest(), "failure" => UpdateCheckResult.Failed("controlled failure"),
            "unsupported" => new(new(new(9, 0, 0), "v9.0.0", "Zitie", null, "https://example.test/releases", null, null), true, null),
            _ => new(new(new(9, 0, 0), "v9.0.0", "Zitie", null, "https://example.test/releases", "https://example.test/setup.exe", "Zitie-win-x64-setup.exe", "https://example.test/setup.exe.sha256"), true, null)
        });
    public async Task<UpdateDownloadResult> DownloadAsync(UpdateInfo info, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (DelayDownload) await Task.Delay(Timeout.Infinite, cancellationToken);
        var path = Path.Combine(root, "fixture", info.AssetName!); File.WriteAllText(path, "controlled package");
        return new(path, info.AssetName!, 18);
    }
    public Task<UpdateInstallResult> InstallAsync(UpdateDownloadResult download, CancellationToken cancellationToken = default)
    { InstallCalls++; return Task.FromResult(new UpdateInstallResult(UpdateInstallKind.Manual, "Controlled installation") { ShouldExitApplication = false }); }
}
