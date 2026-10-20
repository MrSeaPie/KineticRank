using System;
using System.Collections.Generic;
using System.Linq;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class StyledSelfPlayTests
    {
        static void AssertReplays(IReadOnlyList<Move> opening)
        {
            var state = GameState.Initial();
            foreach (var move in opening)
            {
                Assert.IsTrue(state.IsLegal(move), $"illegal {move}");
                state = state.Apply(move);
            }
            Assert.IsFalse(state.IsOver);
        }

        static string Text(IReadOnlyList<Move> opening) => string.Join(",", opening.Select(m => m.ToString()));

        [Test]
        public void TwoPlyOpenings_AreDistinctLegalAndAtMost441()
        {
            var openings = StyledSelfPlay.TwoPlyOpenings();
            Assert.That(openings.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(441));
            foreach (var opening in openings)
            {
                Assert.AreEqual(2, opening.Count);
                AssertReplays(opening);
            }
            Assert.AreEqual(openings.Count, openings.Select(Text).Distinct().Count());
        }

        [Test]
        public void ThreePlyNormalOpenings_AreNormalDistinctAndAtMost343()
        {
            var openings = StyledSelfPlay.ThreePlyNormalOpenings();
            Assert.That(openings.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(343));
            foreach (var opening in openings)
            {
                Assert.AreEqual(3, opening.Count);
                Assert.IsTrue(opening.All(m => m.Kind == MoveKind.Normal));
                AssertReplays(opening);
            }
            Assert.AreEqual(openings.Count, openings.Select(Text).Distinct().Count());
        }

        [Test]
        public void PlayGame_SameSeedGivesSameGame()
        {
            var opening = StyledSelfPlay.TwoPlyOpenings()[0];
            var a = StyledSelfPlay.PlayGame(opening, BotStyle.Positional, BotStyle.Spender, 2, 2, 7, 60);
            var b = StyledSelfPlay.PlayGame(opening, BotStyle.Positional, BotStyle.Spender, 2, 2, 7, 60);
            Assert.AreEqual(a.Result, b.Result);
            Assert.AreEqual(a.Plies, b.Plies);
            Assert.AreEqual(a.MaxRepeats, b.MaxRepeats);
            Assert.AreEqual(7, a.Seed);
            Assert.AreEqual("Positional", a.RedStyle);
            Assert.AreEqual("Spender", a.BlueStyle);
        }

        [Test]
        public void PlayGame_RespectsPlyCap()
        {
            var opening = StyledSelfPlay.ThreePlyNormalOpenings()[0];
            int cap = opening.Count + 4;
            var game = StyledSelfPlay.PlayGame(opening, BotStyle.Default, BotStyle.Hoarder, 1, 1, null, cap);
            Assert.That(game.Plies, Is.LessThanOrEqualTo(cap));
            Assert.That(game.MaxRepeats, Is.GreaterThanOrEqualTo(1));
            if (game.Capped)
            {
                Assert.AreEqual(cap, game.Plies);
                Assert.AreEqual(EvaluationResult.InProgress, game.Result);
                Assert.IsTrue(game.IsDraw);
                Assert.IsFalse(game.DecidedDraw);
            }
        }

        [Test]
        public void Run_TotalsAddUp()
        {
            var openings = StyledSelfPlay.TwoPlyOpenings().Take(5).ToList();
            var report = StyledSelfPlay.Run(openings, BotStyle.Hoarder, BotStyle.Spender, 1, 1, 0, 80);
            Assert.AreEqual(5, report.Games.Count);
            Assert.AreEqual(5, report.RedWins + report.BlueWins + report.Draws);
            Assert.AreEqual(report.Draws, report.CappedGames + report.DecidedDraws);
            for (int i = 0; i < 5; i++) Assert.AreEqual(i, report.Games[i].Seed);
            Assert.That(report.AveragePlies, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void PlayGame_RejectsBadArguments()
        {
            var twoQueens = new[] { new Move(0, MoveKind.Queen), new Move(1, MoveKind.Normal), new Move(2, MoveKind.Queen) };
            Assert.Throws<ArgumentException>(() => StyledSelfPlay.PlayGame(twoQueens, BotStyle.Default, BotStyle.Default, 1, 1, null, 20));
            var opening = new[] { new Move(3, MoveKind.Normal) };
            Assert.Throws<ArgumentNullException>(() => StyledSelfPlay.PlayGame(null, BotStyle.Default, BotStyle.Default, 1, 1, null, 20));
            Assert.Throws<ArgumentNullException>(() => StyledSelfPlay.PlayGame(opening, null, BotStyle.Default, 1, 1, null, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => StyledSelfPlay.PlayGame(opening, BotStyle.Default, BotStyle.Default, 1, 1, null, 0));
        }
    }
}
