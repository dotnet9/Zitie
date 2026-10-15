using CodeWF.Toolkit.Lang;
using Prism.Mvvm;

namespace Zitie.Desktop.I18n;

/// <summary>让已打开的视图模型跟随全局语言切换，避免关闭页面后被单例事件保留。</summary>
public abstract class LocalizedBindableBase : BindableBase
{
    protected LocalizedBindableBase()
    {
        var reference = new WeakReference<LocalizedBindableBase>(this);
        EventHandler<EventArgs>? handler = null;
        handler = (_, _) =>
        {
            if (reference.TryGetTarget(out var target)) target.OnCultureChanged();
            else I18nManager.Instance.CultureChanged -= handler;
        };
        I18nManager.Instance.CultureChanged += handler;
    }

    protected virtual void OnCultureChanged() => RaisePropertyChanged(string.Empty);
}
