using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Zitie.Desktop.Controls;

public class ZitieIcon : TemplatedControl
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<ZitieIcon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<ZitieIcon, double>(nameof(IconSize), 16);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<ZitieIcon, double>(nameof(StrokeThickness), 1.8);

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ZitieIcon);
}
