using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class BracketWordColumnsLayoutEngine : BracketPracticeLayoutEngineBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketWordColumns;

    protected override int DefaultColumns => 4;

    protected override double ItemHeightMm => 11;

    protected override double RowPitchMm => 20;
}
