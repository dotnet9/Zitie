using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CodeWF.Toolkit.Core.Extensions;
using CodeWF.Toolkit.Core.UpdateChecking;
using Prism.Commands;
using Zitie.Desktop.Commands;

namespace Zitie.Desktop.ViewModels;

/// <summary>关于与更新状态；网络、校验和安装统一交给 Toolkit，视图只绑定状态与命令。</summary>
public sealed class AboutViewModel : LocalizedBindableBase
{
    private readonly IUpdateService _updates;
    private readonly Action<string> _openUrl;
    private readonly Action _shutdown;
    private UpdateInfo? _update;
    private UpdateDownloadResult? _download;
    private CancellationTokenSource? _downloadCancellation;
    private bool _isChecking;
    private bool _isDownloading;
    private double _progress;
    private string _statusKey = Language.NotChecked;
    private object?[] _statusArguments = [];

    public AboutViewModel(IUpdateService updates) : this(updates,
        url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }),
        () => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()) { }

    public AboutViewModel(IUpdateService updates, Action<string> openUrl, Action shutdown)
    {
        _updates = updates; _openUrl = openUrl; _shutdown = shutdown;
        CheckCommand = new AsyncDelegateCommand(CheckAsync, () => !IsDownloading);
        DownloadCommand = new AsyncDelegateCommand(DownloadAsync, () => CanDownload);
        InstallCommand = new AsyncDelegateCommand(InstallAsync, () => IsReady);
        CancelCommand = new DelegateCommand(() => _downloadCancellation?.Cancel());
        OpenReleasePageCommand = new DelegateCommand(() => _openUrl(_update?.PageUrl ?? "https://github.com/dotnet9/Zitie/releases/latest"));
    }

    public AsyncDelegateCommand CheckCommand { get; }
    public AsyncDelegateCommand DownloadCommand { get; }
    public AsyncDelegateCommand InstallCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand OpenReleasePageCommand { get; }
    public string VersionText => "v" + UpdateVersion.Display(typeof(App).Assembly);
    public string PlatformText => $".NET {Environment.Version.Major} · Avalonia {typeof(Application).Assembly.GetName().Version?.ToString(3)} · {RuntimeInformation.RuntimeIdentifier}";
    public string BuildTimeText => typeof(App).Assembly.CompileTime()?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string StatusText => AppText.Format(_statusKey, _statusArguments);
    public string ReleaseNotes => _update?.Notes ?? string.Empty;
    public bool HasReleaseNotes => !string.IsNullOrWhiteSpace(ReleaseNotes);
    public bool IsChecking { get => _isChecking; private set => SetProperty(ref _isChecking, value); }
    public bool IsDownloading { get => _isDownloading; private set => SetProperty(ref _isDownloading, value); }
    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }
    public bool CanDownload => _update?.AssetUrl is not null && _download is null && !IsDownloading;
    public bool IsReady => _download is not null;
    public bool NeedsReleasePage => _update is not null && _update.AssetUrl is null;

    public async Task CheckAsync()
    {
        if (IsChecking || IsDownloading) return;
        IsChecking = true; _update = null; _download = null; Progress = 0;
        SetStatus(Language.Checking); RefreshActions();
        try
        {
            var result = await _updates.CheckAsync(UpdateVersion.Current(typeof(App).Assembly));
            _update = result.Update;
            if (!result.Succeeded) SetStatus(Language.UpdateFailed, result.Error);
            else if (_update is null) SetStatus(Language.CurrentVersion, VersionText);
            else SetStatus(Language.UpdateAvailable, _update.Tag);
        }
        catch (Exception exception) { SetStatus(Language.UpdateFailed, exception.Message); }
        finally { IsChecking = false; RefreshActions(); }
    }

    public async Task DownloadAsync()
    {
        if (!CanDownload || _update is null) return;
        using var cancellation = new CancellationTokenSource();
        _downloadCancellation = cancellation; IsDownloading = true; Progress = 0; RefreshActions();
        try
        {
            var progress = new Progress<UpdateDownloadProgress>(value =>
            {
                Progress = value.Percentage ?? 0; SetStatus(Language.Downloading, Progress);
            });
            _download = await _updates.DownloadAsync(_update, progress, cancellation.Token);
            Progress = 100; SetStatus(Language.DownloadReady, _download.FileName);
        }
        catch (OperationCanceledException) { SetStatus(Language.DownloadCancelled); }
        catch (Exception exception) { SetStatus(Language.DownloadFailed, exception.Message); }
        finally { _downloadCancellation = null; IsDownloading = false; RefreshActions(); }
    }

    public async Task InstallAsync()
    {
        if (_download is null) return;
        try
        {
            var result = await _updates.InstallAsync(_download);
            SetStatus(Language.InstallStarted, result.Message);
            if (result.ShouldExitApplication) _shutdown();
        }
        catch (Exception exception) { SetStatus(Language.InstallFailed, exception.Message); }
    }

    private void SetStatus(string key, params object?[] arguments)
    {
        _statusKey = key; _statusArguments = arguments; RaisePropertyChanged(nameof(StatusText));
    }

    private void RefreshActions()
    {
        RaisePropertyChanged(nameof(CanDownload)); RaisePropertyChanged(nameof(IsReady));
        RaisePropertyChanged(nameof(NeedsReleasePage)); RaisePropertyChanged(nameof(ReleaseNotes)); RaisePropertyChanged(nameof(HasReleaseNotes));
        CheckCommand.RaiseCanExecuteChanged(); DownloadCommand.RaiseCanExecuteChanged(); InstallCommand.RaiseCanExecuteChanged();
    }
}
