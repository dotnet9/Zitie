using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Zitie.Avalonia.Rendering;
using Zitie.Core.Layout;
using Zitie.Core.Models;

namespace Zitie.Avalonia.Controls;

/// <summary>
///     字帖页面预览控件：用与导出相同的 Skia 渲染器绘制当前页，保证预览与打印一致。
///     外观 token 见 Themes/Tokens/ZitieTokens.axaml。
/// </summary>
public class ZitiePreview : TemplatedControl
{
    private const double BaseDpi = 96.0;
    private const double RenderQualityScale = 2.0;

    private WriteableBitmap? _bitmap;
    private IImage? _pageImage;
    private bool _templateApplied;

    public static readonly StyledProperty<CharacterSheetSpec?> SpecProperty =
        AvaloniaProperty.Register<ZitiePreview, CharacterSheetSpec?>(nameof(Spec));

    public static readonly StyledProperty<IReadOnlyList<SheetPage>?> PagesProperty =
        AvaloniaProperty.Register<ZitiePreview, IReadOnlyList<SheetPage>?>(nameof(Pages));

    public static readonly StyledProperty<int> PageIndexProperty =
        AvaloniaProperty.Register<ZitiePreview, int>(nameof(PageIndex));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZitiePreview, double>(nameof(Zoom), 1.0);

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<ZitiePreview, HorizontalAlignment>(nameof(HorizontalContentAlignment),
            HorizontalAlignment.Center);

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<ZitiePreview, VerticalAlignment>(nameof(VerticalContentAlignment),
            VerticalAlignment.Top);

    public static readonly DirectProperty<ZitiePreview, IImage?> PageImageProperty =
        AvaloniaProperty.RegisterDirect<ZitiePreview, IImage?>(nameof(PageImage), preview => preview.PageImage);

    static ZitiePreview()
    {
        SpecProperty.Changed.AddClassHandler<ZitiePreview>((preview, _) => preview.InvalidatePreview());
        PagesProperty.Changed.AddClassHandler<ZitiePreview>((preview, _) => preview.InvalidatePreview());
        PageIndexProperty.Changed.AddClassHandler<ZitiePreview>((preview, _) => preview.InvalidatePreview());
        ZoomProperty.Changed.AddClassHandler<ZitiePreview>((preview, _) => preview.InvalidatePreview());
    }

    public CharacterSheetSpec? Spec
    {
        get => GetValue(SpecProperty);
        set => SetValue(SpecProperty, value);
    }

    public IReadOnlyList<SheetPage>? Pages
    {
        get => GetValue(PagesProperty);
        set => SetValue(PagesProperty, value);
    }

    public int PageIndex
    {
        get => GetValue(PageIndexProperty);
        set => SetValue(PageIndexProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public IImage? PageImage
    {
        get => _pageImage;
        private set => SetAndRaise(PageImageProperty, ref _pageImage, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _templateApplied = true;
        InvalidatePreview();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidatePreview();
    }

    private void InvalidatePreview()
    {
        if (!_templateApplied) return;

        var spec = Spec;
        var pages = Pages;
        var page = pages is not null && PageIndex >= 0 && PageIndex < pages.Count
            ? pages[PageIndex]
            : null;

        if (spec is null || page is null)
        {
            PageImage = null;
            _bitmap?.Dispose();
            _bitmap = null;
            return;
        }

        var theme = ResolveTheme();
        var pxPerMm = BaseDpi / 25.4 * Math.Max(0.1, Zoom) * RenderQualityScale;
        var width = Math.Max(1, (int)Math.Ceiling(spec.Page.WidthMm * pxPerMm));
        var height = Math.Max(1, (int)Math.Ceiling(spec.Page.HeightMm * pxPerMm));

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var surface = SKSurface.Create(info);
        if (surface is null) return;

        surface.Canvas.Scale((float)(pxPerMm / LayoutEngine.MmToPt));
        SheetRenderer.RenderPage(surface.Canvas, spec, page, pages!.Count, theme);
        surface.Flush();

        using var snapshot = surface.Snapshot();
        using var pixmap = snapshot.PeekPixels();
        if (pixmap is null) return;

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(BaseDpi, BaseDpi),
            PixelFormat.Bgra8888);

        using (var framebuffer = bitmap.Lock())
        unsafe
        {
            var destinationBytes = (nint)(framebuffer.RowBytes * framebuffer.Size.Height);
            var bytesToCopy = (nint)Math.Min((long)destinationBytes, pixmap.BytesSize);
            Buffer.MemoryCopy((void*)pixmap.GetPixels(), (void*)framebuffer.Address, destinationBytes, bytesToCopy);
        }

        PageImage = bitmap;
        _bitmap?.Dispose();
        _bitmap = bitmap;
    }

    /// <summary>从主题资源解析渲染外观，缺失的键回退到打印默认值。</summary>
    private SheetRenderTheme ResolveTheme()
    {
        var theme = SheetRenderTheme.Print;

        if (TryGetColor("ZitieGridSolidColor", out var color)) theme = theme with { GridSolidColor = color };
        if (TryGetColor("ZitieGridDashColor", out color)) theme = theme with { GridDashColor = color };
        if (TryGetColor("ZitieModelGlyphColor", out color)) theme = theme with { ModelGlyphColor = color };
        if (TryGetColor("ZitieTraceGlyphColor", out color)) theme = theme with { TraceGlyphColor = color };
        if (TryGetColor("ZitieTitleColor", out color)) theme = theme with { TitleColor = color };
        if (TryGetColor("ZitieFieldColor", out color)) theme = theme with { FieldColor = color };
        if (TryGetColor("ZitieFooterColor", out color)) theme = theme with { FooterColor = color };
        if (TryGetDouble("ZitieGridSolidStrokePt", out var stroke)) theme = theme with { GridSolidStrokePt = (float)stroke };
        if (TryGetDouble("ZitieGridDashStrokePt", out stroke)) theme = theme with { GridDashStrokePt = (float)stroke };

        return theme;
    }

    private bool TryGetColor(string key, out Color color)
    {
        if (TryGetResource(key, ActualThemeVariant, out var value) && value is Color result)
        {
            color = result;
            return true;
        }

        color = default;
        return false;
    }

    private bool TryGetDouble(string key, out double value)
    {
        if (TryGetResource(key, ActualThemeVariant, out var resource) && resource is double result)
        {
            value = result;
            return true;
        }

        value = default;
        return false;
    }
}
