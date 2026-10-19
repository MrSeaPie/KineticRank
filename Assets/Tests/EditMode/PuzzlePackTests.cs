using System;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class PuzzlePackTests
    {
        [Test]
        public void PuzzlePack_ContainsExactlyTenPuzzles()
        {
            Assert.AreEqual(10, PuzzlePack.Puzzles.Count);
        }

        [Test]
        public void PuzzlePack_AllPuzzlesHaveUniqueIdsAndTitles()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            var titles = new System.Collections.Generic.HashSet<string>();

            foreach (var puzzle in PuzzlePack.Puzzles)
            {
                Assert.IsNotNull(puzzle);
                Assert.IsFalse(string.IsNullOrWhiteSpace(puzzle.Id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(puzzle.Title));
                Assert.IsTrue(ids.Add(puzzle.Id), $"Duplicate ID: {puzzle.Id}");
                Assert.IsTrue(titles.Add(puzzle.Title), $"Duplicate Title: {puzzle.Title}");
                Assert.AreSame(puzzle, PuzzlePack.GetById(puzzle.Id));
            }
        }

        [Test]
        public void PuzzlePack_AllPuzzlesStartInProgress()
        {
            foreach (var puzzle in PuzzlePack.Puzzles)
            {
                Assert.IsNotNull(puzzle.Start);
                Assert.IsFalse(puzzle.Start.IsOver, $"Puzzle {puzzle.Id} starts already over.");
                Assert.IsTrue(puzzle.MovesN >= 1 && puzzle.MovesN <= PuzzleVerifier.MaxSupportedMoves);
            }
        }

        [Test]
        public void PuzzlePack_AllPuzzlesVerifiedByPuzzleVerifier()
        {
            foreach (var puzzle in PuzzlePack.Puzzles)
            {
                Assert.IsTrue(PuzzleVerifier.IsValidPuzzle(puzzle.Start, puzzle.MovesN),
                    $"Puzzle {puzzle.Id} ({puzzle.Title}) must be a valid puzzle with unique forced winning solution in {puzzle.MovesN} moves.");
                var winningMoves = PuzzleVerifier.WinningFirstMoves(puzzle.Start, puzzle.MovesN);
                Assert.AreEqual(1, winningMoves.Count,
                    $"Puzzle {puzzle.Id} ({puzzle.Title}) must have exactly 1 winning first move.");
            }
        }

        [Test]
        public void PuzzlePack_AllPuzzlesSolvableInPuzzleSession()
        {
            foreach (var puzzle in PuzzlePack.Puzzles)
            {
                var session = new PuzzleSession(puzzle.Start, puzzle.MovesN);
                Assert.AreEqual(PuzzleStatus.InProgress, session.Status);
                Assert.AreEqual(puzzle.MovesN, session.MovesLeft);
                Assert.IsTrue(session.AttackerIsRed);
            }
        }
    }
}
