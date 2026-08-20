using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Zitie.Desktop.Controls;

[PseudoClasses(":normal", ":maximized", ":fullscreen", ":native-window-corners")]
[TemplatePart("PART_TitleBar", typeof(InputElement))]
[TemplatePart("PART_MinimizeButton", typeof(Button))]
[TemplatePart("PART_MaximizeButton", typeof(Button))]
[TemplatePart("PART_CloseButton", typeof(Button))]
public class ZitieWindow : Window
{
    public static readonly StyledProperty<bool> IsTitleBarVisibleProperty =
        AvaloniaProperty.Register<ZitieWindow, bool>(nameof(IsTitleBarVisible), true);

    public static readonly StyledProperty<bool> IsManagedResizerVisibleProperty =
        AvaloniaProperty.Register<ZitieWindow, bool>(nameof(IsManagedResizerVisible), true);

    public static readonly StyledProperty<bool> IsMinimizeButtonVisibleProperty =
        AvaloniaProperty.Register<ZitieWindow, bool>(nameof(IsMinimizeButtonVisible), true);

    public static readonly StyledProperty<bool> IsMaximizeButtonVisibleProperty =
        AvaloniaProperty.Register<ZitieWindow, bool>(nameof(IsMaximizeButtonVisible), true);

    public static readonly StyledProperty<bool> IsCloseButtonVisibleProperty =
        AvaloniaProperty.Register<ZitieWindow, bool>(nameof(IsCloseButtonVisible), true);

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<ZitieWindow, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> TitleBarContentProperty =
        AvaloniaProperty.Register<ZitieWindow, object?>(nameof(TitleBarContent));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<ZitieWindow, object?>(nameof(RightContent));

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<ZitieWindow, double>(nameof(TitleBarHeight), 32);

    public static readonly StyledProperty<IBrush?> TitleBarBackgroundProperty =
        AvaloniaProperty.Register<ZitieWindow, IBrush?>(nameof(TitleBarBackground));

    public static readonly StyledProperty<IBrush?> TitleBarForegroundProperty =
        AvaloniaProperty.Register<ZitieWindow, IBrush?>(nameof(TitleBarForeground));

    public static readonly StyledProperty<IBrush?> TitleBarBorderBrushProperty =
        AvaloniaProperty.Register<ZitieWindow, IBrush?>(nameof(TitleBarBorderBrush));

    public static readonly StyledProperty<Thickness> TitleBarBorderThicknessProperty =
        AvaloniaProperty.Register<ZitieWindow, Thickness>(nameof(TitleBarBorderThickness));

    public static readonly StyledProperty<CornerRadius> WindowCornerRadiusProperty =
        AvaloniaProperty.Register<ZitieWindow, CornerRadius>(nameof(WindowCornerRadius));

    private readonly Dictionary<Control, WindowEdge> _resizeGrips = new();
    private readonly bool _usesNativeWindowCorners;
    private InputElement? _titleBar;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    public ZitieWindow()
    {
        WindowDecorations = WindowDecorations.None;
        ExtendClientAreaToDecorationsHint = false;
        _usesNativeWindowCorners = WindowsWindowCornerHelper.IsSupported;
        TransparencyLevelHint =
            [_usesNativeWindowCorners ? WindowTransparencyLevel.None : WindowTransparencyLevel.Transparent];
        TransparencyBackgroundFallback = Brushes.Transparent;
        PseudoClasses.Set(":native-window-corners", _usesNativeWindowCorners);
    }

    public bool IsTitleBarVisible
    {
        get => GetValue(IsTitleBarVisibleProperty);
        set => SetValue(IsTitleBarVisibleProperty, value);
    }

    public bool IsManagedResizerVisible
    {
        get => GetValue(IsManagedResizerVisibleProperty);
        set => SetValue(IsManagedResizerVisibleProperty, value);
    }

    public bool IsMinimizeButtonVisible
    {
        get => GetValue(IsMinimizeButtonVisibleProperty);
        set => SetValue(IsMinimizeButtonVisibleProperty, value);
    }

    public bool IsMaximizeButtonVisible
    {
        get => GetValue(IsMaximizeButtonVisibleProperty);
        set => SetValue(IsMaximizeButtonVisibleProperty, value);
    }

    public bool IsCloseButtonVisible
    {
        get => GetValue(IsCloseButtonVisibleProperty);
        set => SetValue(IsCloseButtonVisibleProperty, value);
    }

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public IBrush? TitleBarBackground
    {
        get => GetValue(TitleBarBackgroundProperty);
        set => SetValue(TitleBarBackgroundProperty, value);
    }

    public IBrush? TitleBarForeground
    {
        get => GetValue(TitleBarForegroundProperty);
        set => SetValue(TitleBarForegroundProperty, value);
    }

    public IBrush? TitleBarBorderBrush
    {
        get => GetValue(TitleBarBorderBrushProperty);
        set => SetValue(TitleBarBorderBrushProperty, value);
    }

    public Thickness TitleBarBorderThickness
    {
        get => GetValue(TitleBarBorderThicknessProperty);
        set => SetValue(TitleBarBorderThicknessProperty, value);
    }

    public CornerRadius WindowCornerRadius
    {
        get => GetValue(WindowCornerRadiusProperty);
        set => SetValue(WindowCornerRadiusProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ZitieWindow);

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyNativeWindowCornerPreference();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachTemplateEvents();

        _titleBar = e.NameScope.Find<InputElement>("PART_TitleBar");
        _minimizeButton = e.NameScope.Find<Button>("PART_MinimizeButton");
        _maximizeButton = e.NameScope.Find<Button>("PART_MaximizeButton");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");

        if (_titleBar is not null) _titleBar.PointerPressed += TitleBar_OnPointerPressed;
        if (_minimizeButton is not null) _minimizeButton.Click += MinimizeButton_OnClick;
        if (_maximizeButton is not null) _maximizeButton.Click += MaximizeButton_OnClick;
        if (_closeButton is not null) _closeButton.Click += CloseButton_OnClick;

        AttachResizeGrip(e, "PART_ResizeTopLeft", WindowEdge.NorthWest);
        AttachResizeGrip(e, "PART_ResizeTop", WindowEdge.North);
        AttachResizeGrip(e, "PART_ResizeTopRight", WindowEdge.NorthEast);
        AttachResizeGrip(e, "PART_ResizeLeft", WindowEdge.West);
        AttachResizeGrip(e, "PART_ResizeRight", WindowEdge.East);
        AttachResizeGrip(e, "PART_ResizeBottomLeft", WindowEdge.SouthWest);
        AttachResizeGrip(e, "PART_ResizeBottom", WindowEdge.South);
        AttachResizeGrip(e, "PART_ResizeBottomRight", WindowEdge.SouthEast);

        UpdateWindowState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty
            || change.Property == CanResizeProperty
            || change.Property == CanMaximizeProperty
            || change.Property == IsManagedResizerVisibleProperty)
            UpdateWindowState();

        if (change.Property == WindowStateProperty)
            ApplyNativeWindowCornerPreference();
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachTemplateEvents();
        base.OnClosed(e);
    }

    private void AttachResizeGrip(TemplateAppliedEventArgs e, string name, WindowEdge edge)
    {
        var grip = e.NameScope.Find<Control>(name);
        if (grip is null) return;

        _resizeGrips.Add(grip, edge);
        grip.PointerPressed += ResizeGrip_OnPointerPressed;
    }

    private void DetachTemplateEvents()
    {
        if (_titleBar is not null) _titleBar.PointerPressed -= TitleBar_OnPointerPressed;
        if (_minimizeButton is not null) _minimizeButton.Click -= MinimizeButton_OnClick;
        if (_maximizeButton is not null) _maximizeButton.Click -= MaximizeButton_OnClick;
        if (_closeButton is not null) _closeButton.Click -= CloseButton_OnClick;

        foreach (var grip in _resizeGrips.Keys)
            grip.PointerPressed -= ResizeGrip_OnPointerPressed;

        _resizeGrips.Clear();
        _titleBar = null;
        _minimizeButton = null;
        _maximizeButton = null;
        _closeButton = null;
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsFromFocusableControl(e.Source))
            return;

        if (e.ClickCount == 2 && CanResize && CanMaximize && WindowState != WindowState.FullScreen)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void ResizeGrip_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsManagedResizerVisible
            || !CanResize
            || WindowState is WindowState.Maximized or WindowState.FullScreen
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || sender is not Control grip
            || !_resizeGrips.TryGetValue(grip, out var edge))
            return;

        BeginResizeDrag(edge, e);
        e.Handled = true;
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (CanResize && CanMaximize && WindowState != WindowState.FullScreen)
            ToggleMaximizeRestore();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateWindowState()
    {
        PseudoClasses.Set(":normal", WindowState == WindowState.Normal);
        PseudoClasses.Set(":maximized", WindowState == WindowState.Maximized);
        PseudoClasses.Set(":fullscreen", WindowState == WindowState.FullScreen);

        if (_maximizeButton is not null)
            _maximizeButton.IsEnabled = CanResize && CanMaximize && WindowState != WindowState.FullScreen;

        var showResizeGrips = IsManagedResizerVisible
                              && CanResize
                              && WindowState is not (WindowState.Maximized or WindowState.FullScreen);
        foreach (var grip in _resizeGrips.Keys)
            grip.IsVisible = showResizeGrips;
    }

    private void ApplyNativeWindowCornerPreference()
    {
        if (_usesNativeWindowCorners)
            WindowsWindowCornerHelper.TryApply(this, WindowState == WindowState.Normal);
    }

    private bool IsFromFocusableControl(object? source)
    {
        if (source is not Visual sourceVisual) return false;

        var visual = sourceVisual;
        while (visual is not null && visual != _titleBar)
        {
            if (visual is Button or ToggleButton || visual is Control { Focusable: true })
                return true;

            visual = visual.GetVisualParent();
        }

        return false;
    }
}
