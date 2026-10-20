using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class PuzzlePackTests
    {
        [Test]
        public void PuzzlePack_ContainsTenPuzzles()
        {
            Assert.AreEqual(10, PuzzlePack.Puzzles.Count);
            Assert.AreEqual(10, PuzzlePack.All.Count);
        }

        [TestCase("puz_08_flank_fork")]
        [TestCase("puz_09_high_diagonal")]
        [TestCase("puz_10_central_pillar")]
        public void GetById_ReturnsExpectedPuzzle(string id)
        {
            var p = PuzzlePack.GetById(id);
            Assert.IsNotNull(p);
            Assert.AreEqual(id, p.Id);
        }

        [Test]
        public void AllPuzzles_StartInProgress_AndHavePositiveMoveCount()
        {
            for (int i = 0; i < PuzzlePack.Puzzles.Count; i++)
            {
                var p = PuzzlePack.Puzzles[i];
                Assert.IsFalse(p.Start.IsOver, $"{p.Id} should not start in terminal state");
                Assert.IsTrue(p.Start.RedToMove, $"{p.Id} should be Red's turn to move");
                Assert.GreaterOrEqual(p.MovesN, 1, $"{p.Id} should have positive move count");
                Assert.LessOrEqual(p.MovesN, PuzzleVerifier.MaxSupportedMoves, $"{p.Id} moves exceeds verifier max");
            }
        }

        [Test]
        public void AllPuzzles_HaveUniqueForcedWinningMove()
        {
            for (int i = 0; i < PuzzlePack.Puzzles.Count; i++)
            {
                var p = PuzzlePack.Puzzles[i];
                Assert.IsTrue(PuzzleVerifier.IsValidPuzzle(p.Start, p.MovesN), $"{p.Id} must be a valid puzzle with unique winning first move");
                var winningFirstMoves = PuzzleVerifier.WinningFirstMoves(p.Start, p.MovesN);
                Assert.AreEqual(1, winningFirstMoves.Count, $"{p.Id} must have exactly one winning first move");
            }
        }
    }
}
