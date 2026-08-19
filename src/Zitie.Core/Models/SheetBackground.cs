namespace Zitie.Core.Models;

/// <summary>
///     字帖页面背景样式：白底、练习纸、信纸与宣纸风格。
/// </summary>
public enum SheetBackground
{
    /// <summary>纯白底，无底纹（默认）。</summary>
    Plain,

    /// <summary>红格纸：淡红横线底纹，经典书法练习纸。</summary>
    RedGrid,

    /// <summary>信纸：淡蓝横线底纹。</summary>
    Letter,

    /// <summary>宣纸：暖白底色与低对比米色横线。</summary>
    RicePaper
}
