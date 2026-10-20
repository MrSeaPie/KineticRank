using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    public class TranspositionTableTests
    {
        static GameState Play(params int[] columns)
        {
            var state = GameState.Initial();
            foreach (int column in columns)
            {
                if (state.IsOver) break;
                state = state.Apply(new Move(column, MoveKind.Normal));
            }
            return state;
        }

        static BotStyle[] Styles => new[] { BotStyle.Default, BotStyle.Positional, BotStyle.Hoarder, BotStyle.Spender };

        [Test]
        public void KeyIsEqualForEqualPositions()
        {
            var a = Play(3, 3, 2, 4);
            var b = Play(3, 3, 2, 4);
            Assert.AreEqual(PositionKey.From(a), PositionKey.From(b));
            Assert.AreEqual(PositionKey.From(a).GetHashCode(), PositionKey.From(b).GetHashCode());
        }

        [Test]
        public void KeyDiffersBySideToMoveAndPowerUps()
        {
            var grid = Play(3, 3, 2, 4).ToGrid();
            var red = GameState.FromGrid(grid, true);
            var blue = GameState.FromGrid(grid, false);
            var noRedQueen = GameState.FromGrid(grid, true, redQueen: false);
            var noBlueCleaner = GameState.FromGrid(grid, true, blueCleaner: false);

            Assert.AreNotEqual(PositionKey.From(red), PositionKey.From(blue));
            Assert.AreNotEqual(PositionKey.From(red), PositionKey.From(noRedQueen));
            Assert.AreNotEqual(PositionKey.From(red), PositionKey.From(noBlueCleaner));
            Assert.AreNotEqual(PositionKey.From(noRedQueen), PositionKey.From(noBlueCleaner));
        }

        [Test]
        public void KeyDiffersByCell()
        {
            Assert.AreNotEqual(PositionKey.From(Play(3, 2)), PositionKey.From(Play(2, 3)));
            // Last column, top rows land in High, not Low
            Assert.AreNotEqual(PositionKey.From(Play(6, 6, 6, 6)), PositionKey.From(Play(6, 6, 6, 5)));
        }

        [Test]
        public void CachedMatchesUncachedAcrossStylesAndDepths()
        {
            var positions = new[] { GameState.Initial(), Play(3), Play(3, 3, 2, 4), Play(3, 2, 3, 2, 4, 1) };
            foreach (var style in Styles)
            {
                foreach (var position in positions)
                {
                    Assume.That(!position.IsOver);
                    for (int depth = 1; depth <= 3; depth++)
                    {
                        var table = new TranspositionTable();
                        Assert.AreEqual(StyledBot.ChooseMove(position, depth, style),
                            CachedStyledBot.ChooseMove(position, depth, style, table),
                            $"depth {depth}");
                    }
                }
            }
        }

        [Test]
        public void CachedMatchesUncachedAtDepthFourAndCacheIsUsed()
        {
            var position = Play(3, 3, 2, 4);
            var table = new TranspositionTable();
            Assert.AreEqual(StyledBot.ChooseMove(position, 4, BotStyle.Default),
                CachedStyledBot.ChooseMove(position, 4, BotStyle.Default, table));
            Assert.Greater(table.Count, 0);
            Assert.Greater(table.Hits, 0);
        }

        [Test]
        public void ReusedTableGivesSameGameAsUncached()
        {
            foreach (var style in Styles)
            {
                var table = new TranspositionTable();
                var state = GameState.Initial();
                for (int ply = 0; ply < 30 && !state.IsOver; ply++)
                {
                    var expected = StyledBot.ChooseMove(state, 2, style);
                    Assert.AreEqual(expected, CachedStyledBot.ChooseMove(state, 2, style, table), $"ply {ply}");
                    state = state.Apply(expected);
                }
            }
        }

        [Test]
        public void SeededTieBreakMatchesUncached()
        {
            foreach (int seed in new[] { 1, 7, 42 })
            {
                var plain = new Random(seed);
                var cached = new Random(seed);
                var table = new TranspositionTable();
                var state = GameState.Initial();
                for (int ply = 0; ply < 20 && !state.IsOver; ply++)
                {
                    var expected = StyledBot.ChooseMove(state, 2, BotStyle.Positional, plain);
                    Assert.AreEqual(expected, CachedStyledBot.ChooseMove(state, 2, BotStyle.Positional, table, cached), $"seed {seed} ply {ply}");
                    state = state.Apply(expected);
                }
            }
        }

        [Test]
        public void SwitchingStyleClearsTable()
        {
            var table = new TranspositionTable();
            CachedStyledBot.ChooseMove(Play(3, 3), 3, BotStyle.Default, table);
            Assert.Greater(table.Count, 0);
            Assert.AreSame(BotStyle.Default, table.Style);

            var position = Play(3, 3, 2, 4);
            Assert.AreEqual(StyledBot.ChooseMove(position, 3, BotStyle.Hoarder),
                CachedStyledBot.ChooseMove(position, 3, BotStyle.Hoarder, table));
            Assert.AreSame(BotStyle.Hoarder, table.Style);
        }

        [Test]
        public void CapacityLimitDoesNotChangeMoves()
        {
            var position = Play(3, 2, 3, 2, 4, 1);
            var tiny = new TranspositionTable(16);
            Assert.AreEqual(StyledBot.ChooseMove(position, 3, BotStyle.Default),
                CachedStyledBot.ChooseMove(position, 3, BotStyle.Default, tiny));
            Assert.LessOrEqual(tiny.Count, 16);
        }

        [Test]
        public void RejectsBadArguments()
        {
            var table = new TranspositionTable();
            Assert.Throws<ArgumentNullException>(() => CachedStyledBot.ChooseMove(GameState.Initial(), 2, BotStyle.Default, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CachedStyledBot.ChooseMove(GameState.Initial(), 0, BotStyle.Default, table));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TranspositionTable(0));
        }
    }
}
