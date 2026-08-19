using Zitie.Core.Models;

namespace Zitie.Core.Layout;

/// <summary>
///     排版后的一页。页眉页脚由渲染层按 <see cref="HasHeader" /> 绘制。
/// </summary>
public sealed record SheetPage(
    int Index,
    int Columns,
    int Rows,
    bool HasHeader,
    IReadOnlyList<CellSlot> Cells);
