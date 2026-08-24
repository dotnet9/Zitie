using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class BracketPinyinColumnsLayoutEngine : BracketPracticeLayoutEngineBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketPinyinColumns;

    protected override int DefaultColumns => 4;

    protected override double ItemHeightMm => 21;

    protected override double RowPitchMm => 24;
}
