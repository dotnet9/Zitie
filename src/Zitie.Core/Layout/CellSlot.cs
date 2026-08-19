using Zitie.Core.Models;

namespace Zitie.Core.Layout;

/// <summary>
///     排版后的单个格子，坐标为相对页面左上角的毫米值。
/// </summary>
public sealed record CellSlot(
    double XMm,
    double YMm,
    double SizeMm,
    string Glyph,
    CellRole Role,
    int GroupIndex);
