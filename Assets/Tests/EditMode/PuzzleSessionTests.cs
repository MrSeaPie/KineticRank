using System;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    // Positions mirror PuzzleLineTests and disable power-ups so each column has one legal move kind
    public class PuzzleSessionTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[7, 6];

        static GameState NoPowerUps(int[,] grid, bool redToMove) => GameState.FromGrid(grid, redToMove, false, false, false, false);

        static Move Drop(int column) => new Move(column, MoveKind.Normal);

        static GameState MateInOne()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[6, 0] = B; grid[6, 1] = B; grid[5, 0] = B;
            return NoPowerUps(grid, true);
        }

        static GameState MateInTwo()
        {
            var grid = EmptyGrid();
            grid[2, 0] = R; grid[3, 0] = R;
            return NoPowerUps(grid, true);
        }

        [Test]
        public void NewSession_StartsInProgressAtStart()
        {
            var start = MateInTwo();
            var session = new PuzzleSession(start, 2);

            Assert.AreSame(start, session.Current);
            Assert.AreEqual(2, session.MovesLeft);
            Assert.AreEqual(PuzzleStatus.InProgress, session.Status);
            Assert.IsTrue(session.AttackerIsRed);
            Assert.IsNull(session.LastDefenderReply);
        }

        [Test]
        public void MateInOne_CorrectMoveSolves()
        {
            var session = new PuzzleSession(MateInOne(), 1);

            Assert.AreEqual(PuzzleStatus.Solved, session.SubmitAttackerMove(Drop(3)));
            Assert.AreEqual(EvaluationResult.RedWin, session.Current.Result);
            Assert.AreEqual(0, session.MovesLeft);
            Assert.IsNull(session.LastDefenderReply);
        }

        [Test]
        public void WrongMove_FailsWithoutChangingPosition()
        {
            var start = MateInOne();
            var session = new PuzzleSession(start, 1);

            Assert.AreEqual(PuzzleStatus.Failed, session.SubmitAttackerMove(Drop(4)));
            Assert.AreSame(start, session.Current);
            Assert.AreEqual(1, session.MovesLeft);
            Assert.Throws<InvalidOperationException>(() => session.SubmitAttackerMove(Drop(3)));
        }

        [Test]
        public void Restart_AfterFailure_AllowsSolving()
        {
            var start = MateInOne();
            var session = new PuzzleSession(start, 1);
            session.SubmitAttackerMove(Drop(4));

            session.Restart();

            Assert.AreSame(start, session.Current);
            Assert.AreEqual(PuzzleStatus.InProgress, session.Status);
            Assert.AreEqual(PuzzleStatus.Solved, session.SubmitAttackerMove(Drop(3)));
        }

        [Test]
        public void MateInTwo_DefenderRepliesThenAttackerWins()
        {
            var session = new PuzzleSession(MateInTwo(), 2);

            Assert.AreEqual(PuzzleStatus.InProgress, session.SubmitAttackerMove(Drop(1)));
            Assert.AreEqual(1, session.MovesLeft);
            Assert.AreEqual(Drop(0), session.LastDefenderReply);
            Assert.IsTrue(session.Current.RedToMove);
            Assert.AreEqual(B, session.Current.Cell(0, 0));

            Assert.AreEqual(PuzzleStatus.Solved, session.SubmitAttackerMove(Drop(4)));
            Assert.AreEqual(EvaluationResult.RedWin, session.Current.Result);
        }

        [Test]
        public void MoveOutsideTheForcedLine_FailsMidPuzzle()
        {
            var session = new PuzzleSession(MateInTwo(), 2);
            session.SubmitAttackerMove(Drop(1));

            Assert.AreEqual(PuzzleStatus.Failed, session.SubmitAttackerMove(Drop(6)));
            Assert.AreEqual(1, session.MovesLeft);
        }

        [Test]
        public void IllegalMove_ThrowsAndDoesNotFail()
        {
            var session = new PuzzleSession(MateInTwo(), 2);

            Assert.Throws<ArgumentException>(() => session.SubmitAttackerMove(new Move(1, MoveKind.Queen)));
            Assert.Throws<ArgumentException>(() => session.SubmitAttackerMove(Drop(7)));
            Assert.AreEqual(PuzzleStatus.InProgress, session.Status);
        }

        [Test]
        public void Constructor_RejectsBadInput()
        {
            Assert.Throws<ArgumentNullException>(() => new PuzzleSession(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PuzzleSession(MateInOne(), 0));
            var finished = MateInOne().Apply(Drop(3));
            Assert.Throws<ArgumentException>(() => new PuzzleSession(finished, 1));
        }
    }
}
