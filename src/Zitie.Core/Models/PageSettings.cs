namespace Zitie.Core.Models;

/// <summary>页面物理设置，单位毫米。</summary>
public sealed record PageSettings
{
    public static PageSettings A4 { get; } = new();

    public double WidthMm { get; init; } = 210;

    public double HeightMm { get; init; } = 297;

    public double MarginTopMm { get; init; } = 15;

    public double MarginBottomMm { get; init; } = 15;

    public double MarginLeftMm { get; init; } = 15;

    public double MarginRightMm { get; init; } = 15;

    public double UsableWidthMm => WidthMm - MarginLeftMm - MarginRightMm;

    public double UsableHeightMm => HeightMm - MarginTopMm - MarginBottomMm;
}
