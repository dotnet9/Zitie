using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Zitie.Desktop.Services;

namespace Zitie.Desktop.ViewModels;

/// <summary>
///     主窗口 ViewModel：左侧导航、日志面板开关。
///     当前页面高亮通过监听 MainRegion 的导航完成事件同步，
///     无论是导航栏点击、模板卡片打开还是编辑页返回，都能保持一致。
/// </summary>
public class MainWindowViewModel : BindableBase
{
    private const string MainRegionName = "MainRegion";
    private const string GalleryViewName = "ModuleGallery";

    private readonly IRegionManager _regionManager;
    private readonly DeviceSettingsService _deviceSettings;
    private bool _isMainRegionHooked;
    private bool _isGalleryMode = true;
    private bool _isLogVisible;
    private object? _logView;

    public MainWindowViewModel(IRegionManager regionManager, DeviceSettingsService deviceSettings)
    {
        _regionManager = regionManager;
        _deviceSettings = deviceSettings;

        NavigateGalleryCommand = new DelegateCommand(
            () => _regionManager.RequestNavigate(MainRegionName, GalleryViewName));
        NavigateEditorCommand = new DelegateCommand(
            () => _regionManager.RequestNavigate(MainRegionName, "SheetEditor"));
        ToggleLogCommand = new DelegateCommand(() => IsLogVisible = !IsLogVisible);

        // 区域由 DelayedRegionCreationBehavior 在视图附加后才创建，先挂集合变化再补挂当前区域。
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;
        HookMainRegionNavigation();
        Dispatcher.UIThread.Post(HookMainRegionNavigation, DispatcherPriority.Loaded);
    }

    public DelegateCommand NavigateGalleryCommand { get; }

    public DelegateCommand NavigateEditorCommand { get; }

    public DelegateCommand ToggleLogCommand { get; }

    public double MinimumUiFontSize => DeviceSettingsService.MinimumUiFontSize;

    public double MaximumUiFontSize => DeviceSettingsService.MaximumUiFontSize;

    public double UiFontSize
    {
        get => _deviceSettings.FontSize;
        set
        {
            var previous = _deviceSettings.FontSize;
            _deviceSettings.FontSize = value;
            if (Math.Abs(previous - _deviceSettings.FontSize) >= 0.01)
                RaisePropertyChanged();
        }
    }

    /// <summary>当前是否处于模板库页面（控制导航栏高亮）。</summary>
    public bool IsGalleryMode
    {
        get => _isGalleryMode;
        private set => SetProperty(ref _isGalleryMode, value);
    }

    /// <summary>日志面板可见性；首次打开时才创建日志视图。</summary>
    public bool IsLogVisible
    {
        get => _isLogVisible;
        set
        {
            if (!SetProperty(ref _isLogVisible, value)) return;
            if (value && _logView is null)
            {
                _logView = ZitieLogging.CreateView();
                RaisePropertyChanged(nameof(LogView));
            }
        }
    }

    public object? LogView => _logView;

    private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HookMainRegionNavigation();
    }

    private void HookMainRegionNavigation()
    {
        if (_isMainRegionHooked) return;

        var region = _regionManager.Regions.FirstOrDefault(region => region.Name == MainRegionName);
        if (region is null) return;

        region.NavigationService.Navigated += OnMainRegionNavigated;
        _isMainRegionHooked = true;

        if (region.ActiveViews.Count() == 0)
            _regionManager.RequestNavigate(MainRegionName, GalleryViewName);
    }

    private void OnMainRegionNavigated(object? sender, RegionNavigationEventArgs e)
    {
        var target = e.NavigationContext.Uri.OriginalString;
        IsGalleryMode = target.Contains(GalleryViewName, StringComparison.OrdinalIgnoreCase);
    }
}
