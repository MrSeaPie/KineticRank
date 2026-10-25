using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class SelfPlayTests
    {
        const int ReportCap = 120;

        static void AssertCountsAddUp(SelfPlayReport report)
        {
            Assert.AreEqual(report.GameCount, report.RedWins + report.BlueWins + report.Draws);
            Assert.LessOrEqual(report.CappedGames, report.Draws);
            Assert.AreEqual(report.TotalPlies, report.Games.Sum(g => g.Plies));
        }

        [Test]
        public void TwoPlyOpenings_Has49DistinctLegalOpenings()
        {
            var openings = SelfPlay.TwoPlyOpenings();
            Assert.AreEqual(49, openings.Count);
            var seen = new HashSet<string>();
            foreach (var opening in openings)
            {
                Assert.IsTrue(seen.Add(string.Join(" ", opening)), "duplicate opening");
                var state = GameState.Initial();
                foreach (var move in opening)
                {
                    Assert.IsTrue(state.IsLegal(move));
                    state = state.Apply(move);
                }
                Assert.IsFalse(state.IsOver);
            }
        }

        [Test]
        public void PlayGame_RespectsPlyCap()
        {
            var game = SelfPlay.PlayGame(SelfPlay.TwoPlyOpenings()[0], 1, 1, 4);
            Assert.LessOrEqual(game.Plies, 4);
            if (game.Capped)
            {
                Assert.AreEqual(4, game.Plies);
                Assert.IsTrue(game.Drawn);
            }
        }

        [Test]
        public void PlayGame_IllegalOpening_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SelfPlay.PlayGame(new[] { new Move(GameState.Columns, MoveKind.Normal) }, 1, 1, 10));
            Assert.Throws<ArgumentException>(() =>
                SelfPlay.PlayGame(new[] { new Move(0, MoveKind.Queen), new Move(1, MoveKind.Normal), new Move(2, MoveKind.Queen) }, 1, 1, 10));
        }

        [Test]
        public void BadDepth_Throws()
        {
            var opening = SelfPlay.TwoPlyOpenings()[0];
            Assert.Throws<ArgumentOutOfRangeException>(() => SelfPlay.PlayGame(opening, 0, 1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => SelfPlay.PlayGame(opening, 1, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => SelfPlay.Run(new[] { opening }, 0, 1, 10));
        }

        [Test]
        public void Run_CountsAddUp()
        {
            var report = SelfPlay.Run(SelfPlay.TwoPlyOpenings().Take(7), 1, 1, 60);
            Assert.AreEqual(7, report.GameCount);
            AssertCountsAddUp(report);
        }

        [Test]
        public void Run_IsDeterministic()
        {
            var openings = SelfPlay.TwoPlyOpenings().Take(7).ToList();
            var first = SelfPlay.Run(openings, 2, 2, 60);
            var second = SelfPlay.Run(openings, 2, 2, 60);
            Assert.AreEqual(first.ToString(), second.ToString());
            for (int i = 0; i < first.GameCount; i++)
            {
                Assert.AreEqual(first.Games[i].Result, second.Games[i].Result, $"game {i} result");
                Assert.AreEqual(first.Games[i].Plies, second.Games[i].Plies, $"game {i} length");
            }
        }

        // Measurement, not a balance assertion: writes the rates for the fairness decision
        [Test]
        public void FairnessReport_AllTwoPlyOpenings()
        {
            int[][] pairings = { new[] { 1, 1 }, new[] { 2, 2 }, new[] { 1, 2 }, new[] { 2, 1 } };
            var openings = SelfPlay.TwoPlyOpenings();
            var text = new StringBuilder();
            foreach (var pairing in pairings)
            {
                var report = SelfPlay.Run(openings, pairing[0], pairing[1], ReportCap);
                AssertCountsAddUp(report);
                text.AppendLine(report.ToString());
            }

            TestContext.WriteLine(text.ToString());
            Directory.CreateDirectory("TestResults");
            File.WriteAllText(Path.Combine("TestResults", "fairness_report.txt"), text.ToString());
        }
    }
}
