using System;
using NUnit.Framework;
using UnityEngine;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class PuzzleProgressTests
    {
        // Test-only ids so real puzzle progress on this machine is never touched
        const string IdA = "test_puz_a";
        const string IdB = "test_puz_b";
        const string IdC = "test_puz_c";

        [SetUp]
        public void SetUp() { ClearKeys(); PuzzleSettings.Clear(); }

        [TearDown]
        public void TearDown() { ClearKeys(); PuzzleSettings.Clear(); }

        static void ClearKeys()
        {
            PuzzleProgress.ClearSolved(IdA);
            PuzzleProgress.ClearSolved(IdB);
            PuzzleProgress.ClearSolved(IdC);
        }

        [Test]
        public void Settings_DefaultToOff()
        {
            Assert.IsFalse(PuzzleSettings.Enabled);
            Assert.AreEqual("", PuzzleSettings.CurrentId);
        }

        [Test]
        public void Settings_StartPuzzleThenClear()
        {
            PuzzleSettings.StartPuzzle("puz_01_horizontal_win");
            Assert.IsTrue(PuzzleSettings.Enabled);
            Assert.AreEqual("puz_01_horizontal_win", PuzzleSettings.CurrentId);

            PuzzleSettings.Clear();
            Assert.IsFalse(PuzzleSettings.Enabled);
            Assert.AreEqual("", PuzzleSettings.CurrentId);
        }

        [Test]
        public void Settings_EmptyIdThrowsAndStaysOff()
        {
            Assert.Throws<ArgumentException>(() => PuzzleSettings.StartPuzzle(""));
            Assert.Throws<ArgumentException>(() => PuzzleSettings.StartPuzzle(null));
            Assert.IsFalse(PuzzleSettings.Enabled);
        }

        [Test]
        public void Progress_UsesPrefixedKey()
        {
            Assert.AreEqual("puzzle_solved_puz_01_horizontal_win", PuzzleProgress.Key("puz_01_horizontal_win"));
        }

        [Test]
        public void Progress_MarkSolvedRoundTrips()
        {
            Assert.IsFalse(PuzzleProgress.IsSolved(IdA));

            PuzzleProgress.MarkSolved(IdA);
            Assert.IsTrue(PuzzleProgress.IsSolved(IdA));
            Assert.AreEqual(1, PlayerPrefs.GetInt(PuzzleProgress.Key(IdA), 0));
            Assert.IsFalse(PuzzleProgress.IsSolved(IdB));

            PuzzleProgress.ClearSolved(IdA);
            Assert.IsFalse(PuzzleProgress.IsSolved(IdA));
            Assert.IsFalse(PlayerPrefs.HasKey(PuzzleProgress.Key(IdA)));
        }

        [Test]
        public void Progress_SolvedCountIgnoresUnsolvedDuplicatesAndEmpty()
        {
            PuzzleProgress.MarkSolved(IdA);
            PuzzleProgress.MarkSolved(IdC);

            Assert.AreEqual(2, PuzzleProgress.SolvedCount(new[] { IdA, IdB, IdC }));
            Assert.AreEqual(1, PuzzleProgress.SolvedCount(new[] { IdA, IdA, "", null }));
            Assert.AreEqual(0, PuzzleProgress.SolvedCount(null));
        }

        [Test]
        public void Progress_EmptyIdIsNeverSolvedAndNotWritten()
        {
            PuzzleProgress.MarkSolved("");
            PuzzleProgress.MarkSolved(null);

            Assert.IsFalse(PuzzleProgress.IsSolved(""));
            Assert.IsFalse(PuzzleProgress.IsSolved(null));
            Assert.IsFalse(PlayerPrefs.HasKey(PuzzleProgress.KeyPrefix));
        }
    }
}
