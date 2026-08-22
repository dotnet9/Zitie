using Zitie.Core.Models;

namespace Zitie.Core.Layout;

public sealed class BracketGridWordsLayoutEngine : BracketPracticeLayoutEngineBase
{
    public override PracticeLayoutKind Kind => PracticeLayoutKind.BracketGridWords;

    protected override int DefaultColumns => 6;

    protected override double ItemHeightMm => 36;

    protected override double RowPitchMm => 54;
}
