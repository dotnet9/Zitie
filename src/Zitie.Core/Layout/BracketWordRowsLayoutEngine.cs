using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class BracketWordRowsLayoutEngine : BracketPracticeLayoutEngineBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketWordRows;

    protected override int DefaultColumns => 1;

    protected override double ItemHeightMm => 11;

    protected override double RowPitchMm => 18;
}
