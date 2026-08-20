using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Zitie.Desktop.Controls;

/// <summary>
/// 字帖桌面端的无边框窗口基类。
/// 标题栏和按钮由宿主窗口提供，窗口行为统一在这里维护。
/// </summary>
public class ZitieWindow : Window
{
    private readonly Dictionary<Control, WindowEdge> _resizeGrips = new();
    private InputElement? _titleBar;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    public ZitieWindow()
    {
        WindowDecorations = WindowDecorations.None;
        ExtendClientAreaToDecorationsHint = false;
        TransparencyLevelHint = [WindowTransparencyLevel.None];
        TransparencyBackgroundFallback = global::Avalonia.Media.Brushes.Transparent;
    }

    /// <summary>
    /// 将宿主窗口的标题栏、标题栏按钮和缩放热区接入统一行为。
    /// </summary>
    protected void AttachWindowChrome(
        InputElement titleBar,
        Button minimizeButton,
        Button maximizeButton,
        Button closeButton,
        params (Control Grip, WindowEdge Edge)[] resizeGrips)
    {
        DetachWindowChrome();

        _titleBar = titleBar;
        _minimizeButton = minimizeButton;
        _maximizeButton = maximizeButton;
        _closeButton = closeButton;

        _titleBar.PointerPressed += TitleBar_OnPointerPressed;
        _minimizeButton.Click += MinimizeButton_OnClick;
        _maximizeButton.Click += MaximizeButton_OnClick;
        _closeButton.Click += CloseButton_OnClick;

        foreach (var (grip, edge) in resizeGrips)
        {
            _resizeGrips[grip] = edge;
            grip.PointerPressed += ResizeGrip_OnPointerPressed;
        }

        UpdateMaximizeButtonState();
        UpdateResizeGripState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty
            || change.Property == CanResizeProperty
            || change.Property == CanMaximizeProperty)
        {
            UpdateMaximizeButtonState();
            UpdateResizeGripState();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachWindowChrome();
        base.OnClosed(e);
    }

    private void DetachWindowChrome()
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
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed || IsFromFocusableControl(e.Source))
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
        if (!CanResize
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

    private void UpdateMaximizeButtonState()
    {
        if (_maximizeButton is not null)
        {
            _maximizeButton.IsEnabled = CanResize && CanMaximize && WindowState != WindowState.FullScreen;
            _maximizeButton.Classes.Set("maximized", WindowState == WindowState.Maximized);
        }
    }

    private void UpdateResizeGripState()
    {
        var visible = CanResize && WindowState is not (WindowState.Maximized or WindowState.FullScreen);
        foreach (var grip in _resizeGrips.Keys)
            grip.IsVisible = visible;
    }

    private bool IsFromFocusableControl(object? source)
    {
        if (source is not Visual sourceVisual)
            return false;

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
