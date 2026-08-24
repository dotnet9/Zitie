using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Zitie.Core.Layout;
using Zitie.Core.Models;
using Zitie.Desktop.Services;
using Prism.Mvvm;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     模块卡片展示项：包一层命令，避免视图里做 $parent 绑定。
/// </summary>
public sealed class ModuleCardViewModel : BindableBase, IDisposable
{
    public const double BaseCardWidth = 244;
    public const double CardHorizontalSpacing = 16;

    private const double BaseCardHeight = 334;
    private const double BaseThumbHeight = 226;
    private const double BasePreviewZoom = 0.18;
    internal const int ThumbnailDecodeHeight = 384;

    private static readonly SemaphoreSlim ThumbnailDecodeGate = new(4);

    private double _cardZoom;
    private readonly bool _hasPreviewImage;
    private readonly object _thumbnailLoadLock = new();
    private ModulePreview? _preview;
    private Bitmap? _previewImage;
    private Task? _previewImageLoadTask;
    private bool _isPreviewLoading;
    private bool _isDisposed;

    public ModuleCardViewModel(ModuleDefinition module, ICommand openCommand)
    {
        Module = module;
        OpenCommand = openCommand;
        _hasPreviewImage = !string.IsNullOrWhiteSpace(module.PreviewImagePath) &&
                           File.Exists(module.PreviewImagePath);
        _isPreviewLoading = _hasPreviewImage;
        _cardZoom = 1;
    }

    public ModuleDefinition Module { get; }

    public ICommand OpenCommand { get; }

    public CharacterSheetSpec? PreviewSpec => HasPreviewImage ? null : Preview.Spec;

    public IReadOnlyList<SheetPage>? PreviewPages => HasPreviewImage ? null : Preview.Pages;

    public Bitmap? PreviewImage
    {
        get
        {
            if (HasPreviewImage) _ = EnsurePreviewImageLoadedAsync();
            return _previewImage;
        }
    }

    public bool HasPreviewImage => _hasPreviewImage;

    public bool IsPreviewLoading
    {
        get => _isPreviewLoading;
        private set => SetProperty(ref _isPreviewLoading, value);
    }

    public double PreviewImageOpacity => _previewImage is null ? 0 : 1;

    public double CardZoom
    {
        get => _cardZoom;
        set
        {
            var zoom = Math.Clamp(value, ModuleGalleryViewModel.MinimumGalleryZoom, ModuleGalleryViewModel.MaximumGalleryZoom);
            if (!SetProperty(ref _cardZoom, zoom)) return;

            RaisePropertyChanged(nameof(CardWidth));
            RaisePropertyChanged(nameof(CardHeight));
            RaisePropertyChanged(nameof(ThumbHeight));
            RaisePropertyChanged(nameof(PreviewZoom));
        }
    }

    public double CardWidth => Math.Round(BaseCardWidth * CardZoom);

    public double CardHeight => Math.Round(BaseCardHeight * CardZoom);

    public double ThumbHeight => Math.Round(BaseThumbHeight * CardZoom);

    public double PreviewZoom => Math.Round(BasePreviewZoom * CardZoom, 3, MidpointRounding.AwayFromZero);

    public string CategoryText => Module.Categories.FirstOrDefault() ??
                                  Module.Category ??
                                  "自定义";

    public string StatusText => Module.Enabled
        ? $"示例模板 · {GridText}"
        : "即将上线";

    internal bool IsPreviewMaterialized => _preview is not null;

    internal bool IsThumbnailLoadStarted => _previewImageLoadTask is not null;

    internal Task EnsurePreviewImageLoadedAsync()
    {
        if (!HasPreviewImage || _isDisposed) return Task.CompletedTask;

        lock (_thumbnailLoadLock)
            return _previewImageLoadTask ??= LoadPreviewImageAsync(Module.PreviewImagePath);
    }

    private async Task LoadPreviewImageAsync(string path)
    {
        Bitmap? bitmap = null;
        try
        {
            await ThumbnailDecodeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                bitmap = await Task.Run(() => DecodeThumbnail(path)).ConfigureAwait(false);
            }
            finally
            {
                ThumbnailDecodeGate.Release();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var loadedBitmap = bitmap;
                bitmap = null;
                SetPreviewImage(loadedBitmap);
            });
        }
        catch (Exception exception)
        {
            if (!_isDisposed)
                ZitieLogging.Warn($"模板缩略图加载失败：{path}", exception);
        }
        finally
        {
            bitmap?.Dispose();
            if (!_isDisposed)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!_isDisposed) IsPreviewLoading = false;
                });
        }
    }

    internal static Bitmap DecodeThumbnail(string path)
    {
        using var stream = File.OpenRead(path);
        return Bitmap.DecodeToHeight(
            stream,
            ThumbnailDecodeHeight,
            BitmapInterpolationMode.MediumQuality);
    }

    private void SetPreviewImage(Bitmap? bitmap)
    {
        if (bitmap is null) return;

        if (_isDisposed)
        {
            bitmap.Dispose();
            return;
        }

        _previewImage?.Dispose();
        _previewImage = bitmap;
        RaisePropertyChanged(nameof(PreviewImage));
        RaisePropertyChanged(nameof(PreviewImageOpacity));
    }

    private string GridText => ModulePreviewFactory.ParseGrid(Module.Defaults.Grid) switch
    {
        GridKind.None => "无格线",
        GridKind.Tian => "田字格",
        GridKind.HuiGong => "回宫格",
        GridKind.Nine => "九宫格",
        GridKind.Pinyin => "拼音四线格",
        GridKind.English => "四线三格",
        GridKind.Plain => "方格",
        _ => "米字格"
    };

    private ModulePreview Preview => _preview ??= ModulePreviewFactory.Create(Module);

    public void Dispose()
    {
        _isDisposed = true;
        _isPreviewLoading = false;
        _previewImage?.Dispose();
        _previewImage = null;
    }
}
