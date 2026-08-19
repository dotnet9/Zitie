namespace Zitie.Core.Models;

/// <summary>描红字迹深浅预设。</summary>
public enum TraceIntensity
{
    /// <summary>非常深，接近范字。</summary>
    VeryDark,

    /// <summary>深。</summary>
    Dark,

    /// <summary>较深。</summary>
    MediumDark,

    /// <summary>适中，默认描红深浅。</summary>
    Medium,

    /// <summary>略浅。</summary>
    Light,

    /// <summary>非常浅。</summary>
    VeryLight,

    /// <summary>白色，仅保留格线。</summary>
    White,

    /// <summary>空心轮廓字。</summary>
    Hollow
}
