using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class StyleRoundRobinTests
    {
        static readonly BotStyle[] TwoStyles = { BotStyle.Default, BotStyle.Spender };

        static List<IReadOnlyList<Move>> FewOpenings() => StyledSelfPlay.TwoPlyOpenings().Take(3).ToList();

        // The 49 Normal two-move openings from round 1, kept small so depth 4 stays affordable
        static List<IReadOnlyList<Move>> TwoPlyNormalOpenings() => TwoPlyNormalOpenings(GameState.Initial());

        // Same opening set built from a given start, e.g. the Traditional (no power-up) start
        static List<IReadOnlyList<Move>> TwoPlyNormalOpenings(GameState start)
        {
            var openings = new List<IReadOnlyList<Move>>();
            for (int a = 0; a < GameState.Columns; a++)
            {
                var first = new Move(a, MoveKind.Normal);
                var afterFirst = start.Apply(first);
                if (afterFirst.IsOver) continue;
                for (int b = 0; b < GameState.Columns; b++)
                {
                    var second = new Move(b, MoveKind.Normal);
                    if (afterFirst.Apply(second).IsOver) continue;
                    openings.Add(new[] { first, second });
                }
            }
            return openings;
        }

        [Test]
        public void TwoPlyNormalOpenings_HasAll49()
        {
            var openings = TwoPlyNormalOpenings();
            Assert.AreEqual(GameState.Columns * GameState.Columns, openings.Count);
            Assert.IsTrue(openings.All(o => o.Count == 2));
        }

        [Test]
        public void TraditionalOpenings_MatchStandardNormalOpenings()
        {
            var standard = TwoPlyNormalOpenings();
            var traditional = TwoPlyNormalOpenings(StyledSelfPlay.TraditionalStart());
            Assert.AreEqual(standard.Count, traditional.Count);
            for (int i = 0; i < standard.Count; i++)
            {
                Assert.AreEqual(standard[i][0].Column, traditional[i][0].Column);
                Assert.AreEqual(standard[i][1].Column, traditional[i][1].Column);
                Assert.AreEqual(MoveKind.Normal, traditional[i][0].Kind);
                Assert.AreEqual(MoveKind.Normal, traditional[i][1].Kind);
            }
        }

        [Test]
        public void Run_FromTraditionalStart_PlaysEveryPairing()
        {
            var openings = TwoPlyNormalOpenings(StyledSelfPlay.TraditionalStart()).Take(3).ToList();
            var report = StyleRoundRobin.Run(StyledSelfPlay.TraditionalStart(), TwoStyles, openings, 1, 0, 60, true);
            Assert.AreEqual(3, report.Pairings.Count);
            Assert.AreEqual(3 + 6 + 3, report.Games);
            Assert.AreEqual(report.Games, report.FirstMoverWins + report.SecondMoverWins + report.Draws);
            Assert.AreEqual(3, report.Openings);
        }

        [Test]
        public void Run_DefaultStartMatchesExplicitInitialStart()
        {
            var a = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 5, 60, true);
            var b = StyleRoundRobin.Run(GameState.Initial(), TwoStyles, FewOpenings(), 1, 5, 60, true);
            Assert.AreEqual(a.ToText(), b.ToText());
        }

        [Test]
        public void Run_PlaysEveryPairingInBothColours()
        {
            var report = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 0, 60, true);
            Assert.AreEqual(3, report.Pairings.Count);
            foreach (var p in report.Pairings)
            {
                Assert.AreEqual(p.Mirror ? 3 : 6, p.Games);
                Assert.AreEqual(p.Games, p.AWins + p.BWins + p.Draws);
                Assert.AreEqual(p.Games, p.FirstMoverWins + p.SecondMoverWins + p.Draws);
                Assert.That(p.CappedGames, Is.LessThanOrEqualTo(p.Draws));
            }

            var cross = report.Pairings.Single(p => !p.Mirror);
            Assert.AreEqual("Default", cross.StyleA);
            Assert.AreEqual("Spender", cross.StyleB);
            Assert.IsTrue(cross.AAsRed.Games.All(g => g.RedStyle == "Default" && g.BlueStyle == "Spender"));
            Assert.IsTrue(cross.BAsRed.Games.All(g => g.RedStyle == "Spender" && g.BlueStyle == "Default"));

            Assert.AreEqual(report.Pairings.Sum(p => p.Games), report.Games);
            Assert.AreEqual(report.Games, report.FirstMoverWins + report.SecondMoverWins + report.Draws);
            Assert.AreEqual(3, report.Openings);
        }

        [Test]
        public void Run_WithoutMirrorsSkipsSelfPairings()
        {
            var report = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, null, 60, false);
            Assert.AreEqual(1, report.Pairings.Count);
            Assert.IsFalse(report.Pairings[0].Mirror);
            Assert.AreEqual(6, report.Games);
        }

        [Test]
        public void Run_SameSeedGivesSameResults()
        {
            var a = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 5, 60, true);
            var b = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 5, 60, true);
            Assert.AreEqual(a.ToText(), b.ToText());
            for (int i = 0; i < a.Pairings.Count; i++)
            {
                var plies = a.Pairings[i].AAsRed.Games.Select(g => g.Plies).ToList();
                Assert.AreEqual(plies, b.Pairings[i].AAsRed.Games.Select(g => g.Plies).ToList());
            }
        }

        [Test]
        public void ToText_ListsEveryPairing()
        {
            var text = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, null, 60, true).ToText();
            StringAssert.Contains("depth 1, 3 openings, seedBase none", text);
            StringAssert.Contains("| Default | Default |", text);
            StringAssert.Contains("| Default | Spender |", text);
            StringAssert.Contains("| Spender | Spender |", text);
            StringAssert.Contains("| All | All |", text);
        }

        [Test]
        public void Run_RejectsBadArguments()
        {
            var openings = FewOpenings();
            Assert.Throws<ArgumentNullException>(() => StyleRoundRobin.Run(null, openings, 1, null, 60, true));
            Assert.Throws<ArgumentNullException>(() => StyleRoundRobin.Run(TwoStyles, null, 1, null, 60, true));
            Assert.Throws<ArgumentException>(() => StyleRoundRobin.Run(new BotStyle[0], openings, 1, null, 60, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => StyleRoundRobin.Run(TwoStyles, openings, 0, null, 60, true));
            Assert.Throws<ArgumentException>(() => StyleRoundRobin.Run(new[] { BotStyle.Default, BotStyle.Default }, openings, 1, null, 60, true));
        }

        // Long runs: see Docs/FAIRNESS_REPORT_ROUND2.md for the command. Output goes to TestResults/.
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth2_TwoPlyOpenings()
        {
            RunAndWrite(2, StyledSelfPlay.TwoPlyOpenings(), "fairness_round2_depth2.txt");
        }

        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth3_ThreePlyNormalOpenings()
        {
            RunAndWrite(3, StyledSelfPlay.ThreePlyNormalOpenings(), "fairness_round2_depth3.txt");
        }

        // Same opening set as the depth-2 run, so depth 2 vs depth 3 can be compared without the opening set changing
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth3_TwoPlyOpenings()
        {
            RunAndWrite(3, StyledSelfPlay.TwoPlyOpenings(), "fairness_round2_depth3_twoply.txt");
        }

        // Brief item 4 (4v4): the 49 Normal two-move openings keep the run bounded (1225 games)
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth4_TwoPlyNormalOpenings()
        {
            RunAndWrite(4, TwoPlyNormalOpenings(), "fairness_round2_depth4.txt");
        }

        // Same 49 openings as the depth-4 run, so depth 2/3/4 compare directly without opening-forced power-ups
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth2_TwoPlyNormalOpenings()
        {
            RunAndWrite(2, TwoPlyNormalOpenings(), "fairness_round2_depth2_normal49.txt");
        }

        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Depth3_TwoPlyNormalOpenings()
        {
            RunAndWrite(3, TwoPlyNormalOpenings(), "fairness_round2_depth3_normal49.txt");
        }

        // Phase 1 brief section 6: Traditional mode (all four power-up flags unavailable) on the same 49 openings,
        // so first-mover share compares directly with the normal49 runs above
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Traditional_Depth2_TwoPlyNormalOpenings()
        {
            var start = StyledSelfPlay.TraditionalStart();
            RunAndWrite(start, 2, TwoPlyNormalOpenings(start), "fairness_traditional_depth2_normal49.txt");
        }

        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void RoundRobin_Traditional_Depth3_TwoPlyNormalOpenings()
        {
            var start = StyledSelfPlay.TraditionalStart();
            RunAndWrite(start, 3, TwoPlyNormalOpenings(start), "fairness_traditional_depth3_normal49.txt");
        }

        static void RunAndWrite(int depth, List<IReadOnlyList<Move>> openings, string fileName) =>
            RunAndWrite(GameState.Initial(), depth, openings, fileName);

        static void RunAndWrite(GameState start, int depth, List<IReadOnlyList<Move>> openings, string fileName)
        {
            var report = StyleRoundRobin.Run(start, BotStyle.All, openings, depth, 1000, 160, true);
            var text = report.ToText();
            Directory.CreateDirectory("TestResults");
            File.WriteAllText(Path.Combine("TestResults", fileName), text);
            TestContext.WriteLine(text);
            Assert.AreEqual(report.Games, report.FirstMoverWins + report.SecondMoverWins + report.Draws);
        }
    }
}
