namespace Zitie.Core.Models;

/// <summary>练习格类型。</summary>
public enum GridKind
{
    /// <summary>米字格：外框 + 十字 + 对角虚线。</summary>
    Mi,

    /// <summary>田字格：外框 + 十字虚线。</summary>
    Tian,

    /// <summary>回宫格：外框 + 内框。</summary>
    HuiGong,

    /// <summary>方格：仅外框。</summary>
    Plain
}
