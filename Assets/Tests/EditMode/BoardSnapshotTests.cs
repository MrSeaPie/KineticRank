using System;
using NUnit.Framework;
using KineticRank.Logic;

public class BoardSnapshotTests
{
    static GameState Play(params Move[] moves)
    {
        var state = GameState.Initial();
        foreach (var move in moves) state = state.Apply(move);
        return state;
    }

    // Reads the source through an IsToken-style callback, the same way the scene adapter reads BoardManager
    static GameState Capture(GameState source) =>
        BoardSnapshot.Capture((c, r, red) => source.Cell(c, r) == (red ? (int)CellOwner.Red : (int)CellOwner.Blue),
            source.RedToMove,
            source.QueenAvailable(true), source.CleanerAvailable(true),
            source.QueenAvailable(false), source.CleanerAvailable(false));

    static void AssertSame(GameState expected, GameState actual)
    {
        for (int c = 0; c < GameState.Columns; c++)
        for (int r = 0; r < GameState.Rows; r++)
            Assert.AreEqual(expected.Cell(c, r), actual.Cell(c, r), $"cell {c},{r}");
        Assert.AreEqual(expected.RedToMove, actual.RedToMove);
        Assert.AreEqual(expected.Result, actual.Result);
        Assert.AreEqual(expected.QueenAvailable(true), actual.QueenAvailable(true));
        Assert.AreEqual(expected.CleanerAvailable(true), actual.CleanerAvailable(true));
        Assert.AreEqual(expected.QueenAvailable(false), actual.QueenAvailable(false));
        Assert.AreEqual(expected.CleanerAvailable(false), actual.CleanerAvailable(false));
    }

    [Test]
    public void EmptyBoard_MatchesInitialState()
    {
        var initial = GameState.Initial();
        AssertSame(initial, Capture(initial));
    }

    [Test]
    public void MixedPosition_MatchesCellsSideAndPowerUps()
    {
        var state = Play(
            new Move(3, MoveKind.Normal),
            new Move(3, MoveKind.Normal),
            new Move(2, MoveKind.Queen),
            new Move(4, MoveKind.Normal),
            new Move(3, MoveKind.Cleaner));
        AssertSame(state, Capture(state));
    }

    [Test]
    public void FullColumnShift_MatchesAfterBottomTokenPops()
    {
        var moves = new Move[GameState.Rows + 1];
        for (int i = 0; i < moves.Length; i++) moves[i] = new Move(0, MoveKind.Normal);
        var state = Play(moves);
        AssertSame(state, Capture(state));
    }

    [Test]
    public void Bot_ChoosesSameMoveFromSnapshot()
    {
        var state = Play(
            new Move(3, MoveKind.Normal),
            new Move(2, MoveKind.Normal),
            new Move(3, MoveKind.Normal),
            new Move(4, MoveKind.Queen));
        Assert.AreEqual(Bot.ChooseMove(state, 2), Bot.ChooseMove(Capture(state), 2));
    }

    [Test]
    public void CellReportingBothColours_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BoardSnapshot.Capture((c, r, red) => c == 0 && r == 0, true, true, true, true, true));
    }

    [Test]
    public void NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BoardSnapshot.Capture(null, true, true, true, true, true));
    }

    [Test]
    public void Tiers_MapToPlannedDepths()
    {
        Assert.AreEqual(1, BotTiers.Depth(BotTier.Pine));
        Assert.AreEqual(2, BotTiers.Depth(BotTier.Birch));
        Assert.AreEqual(4, BotTiers.Depth(BotTier.Oak));
        Assert.AreEqual(6, BotTiers.Depth(BotTier.Walnut));
        Assert.AreEqual(8, BotTiers.Depth(BotTier.Ebony));
        Assert.Throws<ArgumentOutOfRangeException>(() => BotTiers.Depth((BotTier)5));
    }
}
