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

        static void RunAndWrite(int depth, List<IReadOnlyList<Move>> openings, string fileName)
        {
            var report = StyleRoundRobin.Run(BotStyle.All, openings, depth, 1000, 160, true);
            var text = report.ToText();
            Directory.CreateDirectory("TestResults");
            File.WriteAllText(Path.Combine("TestResults", fileName), text);
            TestContext.WriteLine(text);
            Assert.AreEqual(report.Games, report.FirstMoverWins + report.SecondMoverWins + report.Draws);
        }
    }
}
