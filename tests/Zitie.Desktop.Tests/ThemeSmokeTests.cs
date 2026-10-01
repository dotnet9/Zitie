using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;
using Zitie.Avalonia.Rendering;
using Zitie.Desktop.Controls;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Zitie.Desktop.Tests;

public sealed class ThemeSmokeTests
{
    [AvaloniaFact]
    public void ThemeResources_LoadWithZitieAccentOverrides()
    {
        var accent = Resource<SolidColorBrush>("ZitieAccentBrush");

        Assert.Equal(Color.Parse("#B04A3F"), accent.Color);
        Assert.Same(accent, Resource<SolidColorBrush>("CheckBoxCheckedDefaultBackground"));
        Assert.Same(accent, Resource<SolidColorBrush>("SliderTrackForeground"));
        Assert.IsType<ControlTheme>(Resource<object>("ZitieEditorTabItemTheme"));
        Assert.IsType<SheetRenderTheme>(Resource<object>("ZitieScreenRenderTheme"));
    }

    [AvaloniaFact]
    public void CustomControls_ApplyTemplatesAndAutomationNames()
    {
        var icon = new ZitieIcon
        {
            Data = Geometry.Parse("M5,12 H19"),
            IconSize = 18
        };
        var toolButton = new ZitieToolButton
        {
            Content = "保存",
            IconData = Geometry.Parse("M5,5 H19 V19 H5 Z")
        };
        var stepper = new ZitieStepper
        {
            ValueText = "1 / 3",
            DecreaseIconData = Geometry.Parse("M15,18 L9,12 L15,6"),
            IncreaseIconData = Geometry.Parse("M9,6 L15,12 L9,18"),
            DecreaseToolTip = "上一页",
            IncreaseToolTip = "下一页"
        };
        var window = new Window
        {
            Width = 360,
            Height = 180,
            Content = new StackPanel
            {
                Children = { icon, toolButton, stepper }
            }
        };

        window.Show();

        Assert.Contains(icon.GetVisualDescendants(), visual => visual is ShapePath);
        Assert.Contains(toolButton.GetVisualDescendants(), visual => visual is ZitieIcon);

        var stepperButtons = stepper.GetVisualDescendants().OfType<Button>().ToArray();
        Assert.Equal(2, stepperButtons.Length);
        Assert.Equal("上一页", AutomationProperties.GetName(stepperButtons[0]));
        Assert.Equal("下一页", AutomationProperties.GetName(stepperButtons[1]));

        window.Close();
    }

    private static T Resource<T>(string key)
    {
        var application = Assert.IsAssignableFrom<Application>(Application.Current);
        Assert.True(application.TryFindResource(key, out var value));
        return Assert.IsAssignableFrom<T>(value);
    }
}
