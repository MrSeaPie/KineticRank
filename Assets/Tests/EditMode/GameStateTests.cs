using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class GameStateTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];

        static void AssertColumn(GameState state, int column, params int[] expected)
        {
            for (int r = 0; r < GameState.Rows; r++)
            {
                int want = r < expected.Length ? expected[r] : 0;
                Assert.AreEqual(want, state.Cell(column, r), $"column {column} row {r}");
            }
        }

        [Test]
        public void Initial_EmptyRedToMoveWithAllPowerUps()
        {
            var state = GameState.Initial();
            Assert.IsTrue(state.RedToMove);
            Assert.AreEqual(EvaluationResult.InProgress, state.Result);
            for (int c = 0; c < GameState.Columns; c++) AssertColumn(state, c);
            Assert.AreEqual(GameState.Columns * 3, state.LegalMoves().Count);
        }

        [Test]
        public void Apply_NormalDrops_StackFromBottomAndAlternateSides()
        {
            var state = GameState.Initial()
                .Apply(new Move(3, MoveKind.Normal))
                .Apply(new Move(3, MoveKind.Normal));
            AssertColumn(state, 3, R, B);
            Assert.IsTrue(state.RedToMove);
        }

        [Test]
        public void Apply_DoesNotMutateOriginalState()
        {
            var start = GameState.Initial();
            start.Apply(new Move(0, MoveKind.Normal));
            AssertColumn(start, 0);
            Assert.IsTrue(start.RedToMove);
        }

        [Test]
        public void Apply_FullColumn_PopsBottomAndShiftsDown()
        {
            var grid = EmptyGrid();
            int[] column = { R, B, R, B, R, B };
            for (int r = 0; r < GameState.Rows; r++) grid[2, r] = column[r];
            var state = GameState.FromGrid(grid, true).Apply(new Move(2, MoveKind.Normal));
            AssertColumn(state, 2, B, R, B, R, B, R);
            Assert.IsFalse(state.RedToMove);
        }

        [Test]
        public void Apply_Queen_FlipsColumnAndKeepsMoverOnLandingCell()
        {
            var grid = EmptyGrid();
            grid[4, 0] = R; grid[4, 1] = B; grid[4, 2] = B;
            var state = GameState.FromGrid(grid, false).Apply(new Move(4, MoveKind.Queen));
            AssertColumn(state, 4, B, R, R, B);
            Assert.IsFalse(state.QueenAvailable(false));
            Assert.IsTrue(state.CleanerAvailable(false));
            Assert.IsTrue(state.QueenAvailable(true));
            Assert.AreEqual(GameState.Columns * 3, state.LegalMoves().Count);
        }

        [Test]
        public void Apply_Cleaner_ClearsWholeColumnIncludingItself()
        {
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[1, 1] = B; grid[1, 2] = R;
            var state = GameState.FromGrid(grid, true).Apply(new Move(1, MoveKind.Cleaner));
            AssertColumn(state, 1);
            Assert.IsFalse(state.CleanerAvailable(true));
            Assert.IsTrue(state.QueenAvailable(true));
            Assert.IsFalse(state.RedToMove);
        }

        [Test]
        public void Apply_CleanerOnFullColumn_LeavesItEmpty()
        {
            var grid = EmptyGrid();
            for (int r = 0; r < GameState.Rows; r++) grid[5, r] = r % 2 == 0 ? R : B;
            var state = GameState.FromGrid(grid, true).Apply(new Move(5, MoveKind.Cleaner));
            AssertColumn(state, 5);
        }

        [Test]
        public void Apply_UsedPowerUp_IsNoLongerLegal()
        {
            var state = GameState.Initial()
                .Apply(new Move(0, MoveKind.Queen))
                .Apply(new Move(6, MoveKind.Normal));
            Assert.IsTrue(state.RedToMove);
            Assert.AreEqual(GameState.Columns * 2, state.LegalMoves().Count);
            Assert.IsFalse(state.IsLegal(new Move(1, MoveKind.Queen)));
            Assert.IsTrue(state.IsLegal(new Move(1, MoveKind.Cleaner)));
            Assert.IsTrue(state.IsLegal(new Move(1, MoveKind.Normal)));
        }

        [Test]
        public void IsLegal_RejectsOutOfRangeColumns()
        {
            var state = GameState.Initial();
            Assert.IsFalse(state.IsLegal(new Move(-1, MoveKind.Normal)));
            Assert.IsFalse(state.IsLegal(new Move(GameState.Columns, MoveKind.Normal)));
        }

        [Test]
        public void Apply_CompletingALine_EndsGameWithNoLegalMoves()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[0, 1] = B; grid[1, 1] = B; grid[2, 1] = B;
            var start = GameState.FromGrid(grid, true);
            Assert.AreEqual(EvaluationResult.InProgress, start.Result);
            var state = start.Apply(new Move(3, MoveKind.Normal));
            Assert.IsTrue(state.IsOver);
            Assert.AreNotEqual(EvaluationResult.InProgress, state.Result);
            Assert.AreEqual(BoardEvaluator.Evaluate(state.ToGrid()), state.Result);
            Assert.AreEqual(0, state.LegalMoves().Count);
            Assert.IsFalse(state.IsLegal(new Move(4, MoveKind.Normal)));
        }

        [Test]
        public void FromGrid_CopiesInputGrid()
        {
            var grid = EmptyGrid();
            var state = GameState.FromGrid(grid, true);
            grid[0, 0] = R;
            AssertColumn(state, 0);
        }

        [Test]
        public void FromGrid_RejectsWrongSize()
        {
            Assert.Throws<ArgumentException>(() => GameState.FromGrid(new int[3, 3], true));
            Assert.Throws<ArgumentNullException>(() => GameState.FromGrid(null, true));
        }
    }
}
