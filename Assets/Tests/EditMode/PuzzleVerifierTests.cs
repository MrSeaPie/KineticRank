using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    // Positions use FromGrid with power-ups disabled so that each column has one legal move kind
    public class PuzzleVerifierTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[7, 6];

        static GameState RedToMoveNoPowerUps(int[,] grid) => GameState.FromGrid(grid, true, false, false, false, false);

        static bool Contains(System.Collections.Generic.List<Move> moves, int column, MoveKind kind) =>
            moves.Any(m => m.Column == column && m.Kind == kind);

        [Test]
        public void DropWinInOne_HasExactlyOneSolution()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[6, 0] = B; grid[6, 1] = B; grid[5, 0] = B;
            var state = RedToMoveNoPowerUps(grid);

            var winners = PuzzleVerifier.WinningFirstMoves(state, 1);

            Assert.AreEqual(1, winners.Count);
            Assert.IsTrue(Contains(winners, 3, MoveKind.Normal));
            Assert.IsTrue(PuzzleVerifier.IsValidPuzzle(state, 1));
        }

        [Test]
        public void ShiftOnlyWin_FullColumnPopCompletesTheLine()
        {
            // Row 1 is R R R _ ; column 3 is full, so dropping there pops the bottom and lifts its row-2 Red into row 1
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[0, 1] = R;
            grid[1, 0] = B; grid[1, 1] = R;
            grid[2, 0] = R; grid[2, 1] = R;
            grid[3, 0] = R; grid[3, 1] = B; grid[3, 2] = R; grid[3, 3] = B; grid[3, 4] = R; grid[3, 5] = B;
            var state = RedToMoveNoPowerUps(grid);
            Assert.IsFalse(state.IsOver);

            var winners = PuzzleVerifier.WinningFirstMoves(state, 1);

            Assert.AreEqual(1, winners.Count);
            Assert.IsTrue(Contains(winners, 3, MoveKind.Normal));
            Assert.AreEqual(EvaluationResult.RedWin, state.Apply(new Move(3, MoveKind.Normal)).Result);
        }

        [Test]
        public void TwoWinningMoves_IsRejectedAsNotUnique()
        {
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[2, 0] = R; grid[3, 0] = R;
            var state = RedToMoveNoPowerUps(grid);

            var winners = PuzzleVerifier.WinningFirstMoves(state, 1);

            Assert.AreEqual(2, winners.Count);
            Assert.IsTrue(Contains(winners, 0, MoveKind.Normal));
            Assert.IsTrue(Contains(winners, 4, MoveKind.Normal));
            Assert.IsTrue(PuzzleVerifier.HasForcedWin(state, 1));
            Assert.IsFalse(PuzzleVerifier.IsValidPuzzle(state, 1));
        }

        [Test]
        public void EmptyBoard_HasNoForcedWinInTwo()
        {
            var state = RedToMoveNoPowerUps(EmptyGrid());

            Assert.IsFalse(PuzzleVerifier.HasForcedWin(state, 2));
            Assert.IsFalse(PuzzleVerifier.IsValidPuzzle(state, 2));
        }

        [Test]
        public void DoubleFourDraw_IsNotCountedAsWin()
        {
            // The shift in column 3 completes Red's row 1 and Blue's row 0 at the same time
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[0, 1] = R;
            grid[1, 0] = B; grid[1, 1] = R;
            grid[2, 0] = B; grid[2, 1] = R;
            grid[3, 0] = R; grid[3, 1] = B; grid[3, 2] = R; grid[3, 3] = B; grid[3, 4] = R; grid[3, 5] = B;
            var state = RedToMoveNoPowerUps(grid);
            Assert.IsFalse(state.IsOver);

            Assert.AreEqual(EvaluationResult.Draw, state.Apply(new Move(3, MoveKind.Normal)).Result);
            Assert.AreEqual(0, PuzzleVerifier.WinningFirstMoves(state, 1).Count);
        }

        [Test]
        public void OpenThree_IsForcedWinInTwoButNotInOne()
        {
            var grid = EmptyGrid();
            grid[2, 0] = R; grid[3, 0] = R;
            var state = RedToMoveNoPowerUps(grid);

            Assert.IsFalse(PuzzleVerifier.HasForcedWin(state, 1));
            var winners = PuzzleVerifier.WinningFirstMoves(state, 2);
            Assert.AreEqual(2, winners.Count);
            Assert.IsTrue(Contains(winners, 1, MoveKind.Normal));
            Assert.IsTrue(Contains(winners, 4, MoveKind.Normal));
        }
    }
}
