using System;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    // Positions use FromGrid with power-ups disabled so that each column has one legal move kind
    public class PuzzleLineTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[7, 6];

        static GameState NoPowerUps(int[,] grid, bool redToMove) => GameState.FromGrid(grid, redToMove, false, false, false, false);

        static Move Drop(int column) => new Move(column, MoveKind.Normal);

        [Test]
        public void MateInOne_WinningMoveEndsTheGameWithoutReply()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[6, 0] = B; grid[6, 1] = B; grid[5, 0] = B;
            var state = NoPowerUps(grid, true);

            Assert.AreEqual(1, PuzzleLine.MovesToForcedWin(state, 3));
            Assert.IsTrue(PuzzleLine.IsCorrectAttackerMove(state, Drop(3), 1));
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(4), 1));

            var solved = state.Apply(Drop(3));
            Assert.AreEqual(EvaluationResult.RedWin, solved.Result);
            Assert.Throws<InvalidOperationException>(() => PuzzleLine.DefenderReply(solved, true, 0));
        }

        [Test]
        public void MateInTwo_AcceptsEitherOpenThreeMove()
        {
            var grid = EmptyGrid();
            grid[2, 0] = R; grid[3, 0] = R;
            var state = NoPowerUps(grid, true);

            Assert.AreEqual(2, PuzzleLine.MovesToForcedWin(state, 3));
            Assert.IsNull(PuzzleLine.MovesToForcedWin(state, 1));
            Assert.IsTrue(PuzzleLine.IsCorrectAttackerMove(state, Drop(1), 2));
            Assert.IsTrue(PuzzleLine.IsCorrectAttackerMove(state, Drop(4), 2));
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(6), 2));
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(1), 1));
        }

        [Test]
        public void DefenderReply_PicksTheOnlyReplyThatDelaysTheWin()
        {
            // Red threatens column 6 vertically; blocking it still loses to the row-0 open two, but only in 2 moves
            var grid = EmptyGrid();
            grid[6, 0] = R; grid[6, 1] = R; grid[6, 2] = R;
            grid[2, 0] = R; grid[3, 0] = R;
            var state = NoPowerUps(grid, false);

            Assert.AreEqual(Drop(6), PuzzleLine.DefenderReply(state, true, 2));
            Assert.AreEqual(2, PuzzleLine.MovesToForcedWin(state.Apply(Drop(6)), 2));
            Assert.AreEqual(1, PuzzleLine.MovesToForcedWin(state.Apply(Drop(0)), 2));
        }

        [Test]
        public void DefenderReply_TieGoesToFirstLegalMove()
        {
            // After Red's open three every Blue reply loses in 1, so the first move in LegalMoves order is chosen
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[2, 0] = R; grid[3, 0] = R;
            var state = NoPowerUps(grid, false);

            Assert.AreEqual(Drop(0), PuzzleLine.DefenderReply(state, true, 1));
            Assert.AreEqual(Drop(0), PuzzleLine.DefenderReply(state, true, 1));
        }

        [Test]
        public void MoveThatOnlyDraws_IsRejected()
        {
            // The shift in column 3 completes Red's row 1 and Blue's row 0 at the same time
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[0, 1] = R;
            grid[1, 0] = B; grid[1, 1] = R;
            grid[2, 0] = B; grid[2, 1] = R;
            grid[3, 0] = R; grid[3, 1] = B; grid[3, 2] = R; grid[3, 3] = B; grid[3, 4] = R; grid[3, 5] = B;
            var state = NoPowerUps(grid, true);

            Assert.AreEqual(EvaluationResult.Draw, state.Apply(Drop(3)).Result);
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(3), 1));
            Assert.IsNull(PuzzleLine.MovesToForcedWin(state, 1));
        }

        [Test]
        public void IllegalOrOutOfBudgetMoves_AreRejected()
        {
            var state = NoPowerUps(EmptyGrid(), true);

            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, new Move(0, MoveKind.Queen), 1));
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(7), 1));
            Assert.IsFalse(PuzzleLine.IsCorrectAttackerMove(state, Drop(3), 0));
            Assert.IsNull(PuzzleLine.MovesToForcedWin(state, 2));
        }
    }
}
