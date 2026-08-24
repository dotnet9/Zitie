using Zitie.Core.Models;

namespace Zitie.Core.Layout;

/// <summary>固定题型模板的排版引擎。</summary>
public interface IPracticeLayoutEngine
{
    PracticeLayoutKind Kind { get; }

    IReadOnlyList<SheetPage> Paginate(CharacterSheetSpec spec);
}
