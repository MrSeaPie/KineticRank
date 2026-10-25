using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class TutorialPackTests
    {
        [Test]
        public void TutorialPack_ContainsFourSteps()
        {
            Assert.AreEqual(4, TutorialPack.Steps.Count);
        }

        [Test]
        public void AllSteps_StartInProgress_WithRedToMove()
        {
            for (int i = 0; i < TutorialPack.Steps.Count; i++)
            {
                var step = TutorialPack.Steps[i];
                Assert.IsFalse(step.Start.IsOver, $"{step.Id} should not start in terminal state");
                Assert.IsTrue(step.Start.RedToMove, $"{step.Id} should be Red's turn to move");
                Assert.IsFalse(string.IsNullOrWhiteSpace(step.Hint), $"{step.Id} needs hint text");
            }
        }

        [TestCase("tut_01_drop", 3, MoveKind.Normal)]
        [TestCase("tut_02_shift", 3, MoveKind.Normal)]
        [TestCase("tut_03_queen", 3, MoveKind.Queen)]
        [TestCase("tut_04_cleaner", 3, MoveKind.Cleaner)]
        public void Step_HasOnlyTheTaughtWinningMove(string id, int column, MoveKind kind)
        {
            var step = TutorialPack.GetById(id);
            Assert.IsNotNull(step);
            var winners = PuzzleVerifier.WinningFirstMoves(step.Start, step.MovesN);
            Assert.AreEqual(1, winners.Count, $"{id} must have exactly one winning first move");
            Assert.AreEqual(new Move(column, kind), winners[0]);
        }

        [TestCase("tut_01_drop", false, false)]
        [TestCase("tut_02_shift", false, false)]
        [TestCase("tut_03_queen", true, false)]
        [TestCase("tut_04_cleaner", false, true)]
        public void Step_EnablesOnlyTheTaughtPowerUp(string id, bool redQueen, bool redCleaner)
        {
            var start = TutorialPack.GetById(id).Start;
            Assert.AreEqual(redQueen, start.QueenAvailable(true));
            Assert.AreEqual(redCleaner, start.CleanerAvailable(true));
            Assert.IsFalse(start.QueenAvailable(false));
            Assert.IsFalse(start.CleanerAvailable(false));
        }

        [Test]
        public void ShiftStep_SolutionColumnIsFull()
        {
            var start = TutorialPack.GetById("tut_02_shift").Start;
            Assert.AreNotEqual(0, start.Cell(3, GameState.Rows - 1));
        }

        [Test]
        public void CleanerStep_HasNoWinInOneMove()
        {
            var step = TutorialPack.GetById("tut_04_cleaner");
            Assert.AreEqual(2, step.MovesN);
            Assert.IsFalse(PuzzleVerifier.HasForcedWin(step.Start, 1));
        }

        [Test]
        public void GetById_UnknownId_ReturnsNull()
        {
            Assert.IsNull(TutorialPack.GetById("tut_99_missing"));
        }
    }
}
