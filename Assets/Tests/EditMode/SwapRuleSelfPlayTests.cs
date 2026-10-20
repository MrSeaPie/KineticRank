using System;
using System.Collections.Generic;
using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class SwapRuleSelfPlayTests
    {
        const int MaxPlies = 120;

        static readonly BotStyle[] Styles = { BotStyle.Default, BotStyle.Hoarder, BotStyle.Spender, BotStyle.Positional, BotStyle.Attacker };

        static List<IReadOnlyList<Move>> NormalOpenings() =>
            StyledSelfPlay.TwoPlyOpenings().Where(o => o.All(m => m.Kind == MoveKind.Normal)).Select(o => (IReadOnlyList<Move>)o.ToList()).ToList();

        [Test]
        public void Score_AtDepth1_IsBestNegatedChildEvaluation()
        {
            var state = GameState.Initial().Apply(new Move(3, MoveKind.Normal));
            var kinds = new[] { MoveKind.Normal, MoveKind.Queen, MoveKind.Cleaner };
            foreach (var style in new[] { BotStyle.Default, BotStyle.Hoarder })
            {
                int expected = int.MinValue;
                for (int column = 0; column < 7; column++)
                {
                    foreach (var kind in kinds)
                    {
                        var move = new Move(column, kind);
                        if (!state.IsLegal(move)) continue;
                        var child = state.Apply(move);
                        Assert.IsFalse(child.IsOver);
                        expected = Math.Max(expected, -StyledBot.Evaluate(child, style));
                    }
                }
                Assert.AreEqual(expected, StyledBot.Score(state, 1, style));
            }
        }

        [Test]
        public void PlayGame_IsDeterministicAndSwapsOnNegativeScore()
        {
            var opening = NormalOpenings()[0];
            var a = SwapRuleSelfPlay.PlayGame(opening, BotStyle.Hoarder, BotStyle.Spender, 1, null, 60);
            var b = SwapRuleSelfPlay.PlayGame(opening, BotStyle.Hoarder, BotStyle.Spender, 1, null, 60);
            Assert.AreEqual(a.Swapped, b.Swapped);
            Assert.AreEqual(a.SwapScore, b.SwapScore);
            Assert.AreEqual(a.Game.Result, b.Game.Result);
            Assert.AreEqual(a.Game.Plies, b.Game.Plies);

            Assert.AreEqual(StyledBot.Score(GameState.Initial().Apply(opening[0]), 1, BotStyle.Spender), a.SwapScore);
            Assert.AreEqual(a.SwapScore < 0, a.Swapped);
            Assert.AreEqual("Hoarder", a.FirstStyle);
            Assert.AreEqual("Spender", a.SecondStyle);
            Assert.AreEqual(a.Swapped ? "Spender" : "Hoarder", a.Game.RedStyle);
        }

        [Test]
        public void Run_TotalsAddUpAndSwapsFollowTheRule()
        {
            var openings = NormalOpenings().Take(10).ToList();
            var report = SwapRuleSelfPlay.Run(openings, BotStyle.Default, BotStyle.Positional, 1, 0, 80);
            Assert.AreEqual(10, report.Games.Count);
            Assert.AreEqual(10, report.FirstPlayerWins + report.SecondPlayerWins + report.Draws);
            Assert.AreEqual(10, report.RedWins + report.BlueWins + report.Draws);
            Assert.AreEqual(report.Games.Count(g => g.Swapped), report.Swaps);
            for (int i = 0; i < 10; i++)
            {
                var game = report.Games[i];
                Assert.AreEqual(i, game.Game.Seed);
                Assert.AreEqual(game.SwapScore < 0, game.Swapped);
                Assert.AreEqual("Default", game.FirstStyle);
                Assert.AreEqual("Positional", game.SecondStyle);
            }
        }

        [Test]
        public void PlayGame_RejectsBadArguments()
        {
            var opening = new[] { new Move(3, MoveKind.Normal) };
            Assert.Throws<ArgumentNullException>(() => SwapRuleSelfPlay.PlayGame(null, BotStyle.Default, BotStyle.Default, 1, null, 20));
            Assert.Throws<ArgumentNullException>(() => SwapRuleSelfPlay.PlayGame(opening, null, BotStyle.Default, 1, null, 20));
            Assert.Throws<ArgumentNullException>(() => SwapRuleSelfPlay.PlayGame(opening, BotStyle.Default, null, 1, null, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => SwapRuleSelfPlay.PlayGame(opening, BotStyle.Default, BotStyle.Default, 0, null, 20));
            Assert.Throws<ArgumentException>(() => SwapRuleSelfPlay.PlayGame(new Move[0], BotStyle.Default, BotStyle.Default, 1, null, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => StyledBot.Score(GameState.Initial(), 0, BotStyle.Default));
        }

        // Every ordered style pair on the 49 two-ply Normal openings, played once without and once with the swap rule.
        static void CompareWithAndWithoutSwap(int depth)
        {
            var openings = NormalOpenings();
            Assert.AreEqual(49, openings.Count);

            int plainFirst = 0, plainSecond = 0, plainDraws = 0;
            int swapFirst = 0, swapSecond = 0, swapDraws = 0, swaps = 0;
            var swapsByColumn = new int[7];
            var gamesByColumn = new int[7];

            foreach (var first in Styles)
            {
                foreach (var second in Styles)
                {
                    foreach (var opening in openings)
                    {
                        var plain = StyledSelfPlay.PlayGame(opening, first, second, depth, depth, null, MaxPlies);
                        if (plain.Result == EvaluationResult.RedWin) plainFirst++;
                        else if (plain.Result == EvaluationResult.BlueWin) plainSecond++;
                        else plainDraws++;

                        var swapped = SwapRuleSelfPlay.PlayGame(opening, first, second, depth, null, MaxPlies);
                        if (swapped.FirstPlayerWon) swapFirst++;
                        else if (swapped.SecondPlayerWon) swapSecond++;
                        else swapDraws++;
                        gamesByColumn[opening[0].Column]++;
                        if (swapped.Swapped)
                        {
                            swaps++;
                            swapsByColumn[opening[0].Column]++;
                        }
                    }
                }
            }

            int games = Styles.Length * Styles.Length * openings.Count;
            Assert.AreEqual(games, plainFirst + plainSecond + plainDraws);
            Assert.AreEqual(games, swapFirst + swapSecond + swapDraws);

            double Share(int a, int b) => a + b == 0 ? 0.0 : 100.0 * a / (a + b);
            TestContext.WriteLine($"Swap rule, depth {depth}, {games} games per mode, cap {MaxPlies} plies");
            TestContext.WriteLine($"No swap:   first {plainFirst}, second {plainSecond}, draws {plainDraws}, first share of decisive {Share(plainFirst, plainSecond):F1}%");
            TestContext.WriteLine($"With swap: first {swapFirst}, second {swapSecond}, draws {swapDraws}, first share of decisive {Share(swapFirst, swapSecond):F1}%, swaps {swaps}");
            for (int column = 0; column < 7; column++)
            {
                TestContext.WriteLine($"First move column {column}: swapped {swapsByColumn[column]} of {gamesByColumn[column]}");
            }
        }

        [Test, Explicit("Long fairness run; see Docs/FAIRNESS_REPORT.md")]
        public void SwapRule_Depth2_TwoPlyNormalOpenings() => CompareWithAndWithoutSwap(2);

        [Test, Explicit("Long fairness run; see Docs/FAIRNESS_REPORT.md")]
        public void SwapRule_Depth3_TwoPlyNormalOpenings() => CompareWithAndWithoutSwap(3);
    }
}
