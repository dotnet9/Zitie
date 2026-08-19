namespace Zitie.Core.Models;

/// <summary>
///     字帖页面背景样式：白底之外的纸张模板（红格纸、信纸）。
/// </summary>
public enum SheetBackground
{
    /// <summary>纯白底，无底纹（默认）。</summary>
    Plain,

    /// <summary>红格纸：淡红横线底纹，经典书法练习纸。</summary>
    RedGrid,

    /// <summary>信纸：淡蓝横线底纹。</summary>
    Letter
}
