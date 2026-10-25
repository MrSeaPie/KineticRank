using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class BoardSnapshotTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        // Answers like BoardManager.IsToken would for the given grid
        static Func<int, int, bool, bool> IsTokenOf(int[,] grid) =>
            (c, r, red) => grid[c, r] == (red ? R : B);

        static int[,] GridOf(GameState state)
        {
            var grid = new int[GameState.Columns, GameState.Rows];
            for (int c = 0; c < GameState.Columns; c++)
            for (int r = 0; r < GameState.Rows; r++)
                grid[c, r] = state.Cell(c, r);
            return grid;
        }

        static GameState CaptureOf(GameState state) =>
            BoardSnapshot.Capture(IsTokenOf(GridOf(state)), state.RedToMove,
                state.QueenAvailable(true), state.CleanerAvailable(true),
                state.QueenAvailable(false), state.CleanerAvailable(false));

        static void AssertSame(GameState expected, GameState actual)
        {
            for (int c = 0; c < GameState.Columns; c++)
            for (int r = 0; r < GameState.Rows; r++)
                Assert.AreEqual(expected.Cell(c, r), actual.Cell(c, r), $"column {c} row {r}");
            Assert.AreEqual(expected.RedToMove, actual.RedToMove, "side to move");
            Assert.AreEqual(expected.QueenAvailable(true), actual.QueenAvailable(true), "red queen");
            Assert.AreEqual(expected.CleanerAvailable(true), actual.CleanerAvailable(true), "red cleaner");
            Assert.AreEqual(expected.QueenAvailable(false), actual.QueenAvailable(false), "blue queen");
            Assert.AreEqual(expected.CleanerAvailable(false), actual.CleanerAvailable(false), "blue cleaner");
            Assert.AreEqual(expected.Result, actual.Result, "result");
            Assert.AreEqual(expected.LegalMoves().Count, actual.LegalMoves().Count, "legal move count");
        }

        [Test]
        public void Capture_EmptyBoard_MatchesInitial()
        {
            var start = GameState.Initial();
            AssertSame(start, CaptureOf(start));
        }

        [Test]
        public void Capture_MidGameWithBottomPop_MatchesAppliedState()
        {
            var state = GameState.Initial();
            for (int i = 0; i < GameState.Rows + 1; i++) state = state.Apply(new Move(2, MoveKind.Normal));
            state = state
                .Apply(new Move(3, MoveKind.Normal))
                .Apply(new Move(4, MoveKind.Normal))
                .Apply(new Move(3, MoveKind.Normal));
            Assert.AreEqual(EvaluationResult.InProgress, state.Result);
            AssertSame(state, CaptureOf(state));
        }

        [Test]
        public void Capture_PowerUpsUsed_MatchesAppliedState()
        {
            var state = GameState.Initial()
                .Apply(new Move(0, MoveKind.Queen))
                .Apply(new Move(6, MoveKind.Normal))
                .Apply(new Move(1, MoveKind.Normal))
                .Apply(new Move(1, MoveKind.Cleaner));
            Assert.IsFalse(state.QueenAvailable(true));
            Assert.IsFalse(state.CleanerAvailable(false));
            AssertSame(state, CaptureOf(state));
        }

        [Test]
        public void Capture_PassesPowerUpFlagsInOrder()
        {
            var grid = new int[GameState.Columns, GameState.Rows];
            grid[0, 0] = R; grid[0, 1] = B; grid[5, 0] = B;
            var captured = BoardSnapshot.Capture(IsTokenOf(grid), false, true, false, false, true);
            AssertSame(GameState.FromGrid(grid, false, true, false, false, true), captured);
        }

        [Test]
        public void Capture_CellReportingBothColours_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BoardSnapshot.Capture((c, r, red) => c == 3 && r == 0, true, true, true, true, true));
        }

        [Test]
        public void Capture_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BoardSnapshot.Capture(null, true, true, true, true, true));
        }
    }
}
