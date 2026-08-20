using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Zitie.Desktop.Controls;

public class ZitieStepper : TemplatedControl
{
    public static readonly StyledProperty<string?> ValueTextProperty =
        AvaloniaProperty.Register<ZitieStepper, string?>(nameof(ValueText));

    public static readonly StyledProperty<double> ValueWidthProperty =
        AvaloniaProperty.Register<ZitieStepper, double>(nameof(ValueWidth), 44);

    public static readonly StyledProperty<Geometry?> DecreaseIconDataProperty =
        AvaloniaProperty.Register<ZitieStepper, Geometry?>(nameof(DecreaseIconData));

    public static readonly StyledProperty<Geometry?> IncreaseIconDataProperty =
        AvaloniaProperty.Register<ZitieStepper, Geometry?>(nameof(IncreaseIconData));

    public static readonly StyledProperty<ICommand?> DecreaseCommandProperty =
        AvaloniaProperty.Register<ZitieStepper, ICommand?>(nameof(DecreaseCommand));

    public static readonly StyledProperty<ICommand?> IncreaseCommandProperty =
        AvaloniaProperty.Register<ZitieStepper, ICommand?>(nameof(IncreaseCommand));

    public static readonly StyledProperty<string?> DecreaseToolTipProperty =
        AvaloniaProperty.Register<ZitieStepper, string?>(nameof(DecreaseToolTip));

    public static readonly StyledProperty<string?> IncreaseToolTipProperty =
        AvaloniaProperty.Register<ZitieStepper, string?>(nameof(IncreaseToolTip));

    public string? ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public double ValueWidth
    {
        get => GetValue(ValueWidthProperty);
        set => SetValue(ValueWidthProperty, value);
    }

    public Geometry? DecreaseIconData
    {
        get => GetValue(DecreaseIconDataProperty);
        set => SetValue(DecreaseIconDataProperty, value);
    }

    public Geometry? IncreaseIconData
    {
        get => GetValue(IncreaseIconDataProperty);
        set => SetValue(IncreaseIconDataProperty, value);
    }

    public ICommand? DecreaseCommand
    {
        get => GetValue(DecreaseCommandProperty);
        set => SetValue(DecreaseCommandProperty, value);
    }

    public ICommand? IncreaseCommand
    {
        get => GetValue(IncreaseCommandProperty);
        set => SetValue(IncreaseCommandProperty, value);
    }

    public string? DecreaseToolTip
    {
        get => GetValue(DecreaseToolTipProperty);
        set => SetValue(DecreaseToolTipProperty, value);
    }

    public string? IncreaseToolTip
    {
        get => GetValue(IncreaseToolTipProperty);
        set => SetValue(IncreaseToolTipProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ZitieStepper);
}
