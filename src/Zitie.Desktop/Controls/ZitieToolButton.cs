using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Zitie.Desktop.Controls;

public class ZitieToolButton : Button
{
    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<ZitieToolButton, Geometry?>(nameof(IconData));

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ZitieToolButton);
}
