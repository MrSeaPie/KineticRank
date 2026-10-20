using System;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class StyledBotTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];

        static BotStyle[] Presets() => new[] { BotStyle.Default, BotStyle.Positional, BotStyle.Hoarder, BotStyle.Spender };

        static GameState[] SamplePositions()
        {
            var win = EmptyGrid();
            win[0, 0] = R; win[1, 0] = R; win[2, 0] = R;
            win[0, 1] = B; win[1, 1] = B; win[2, 1] = B;
            return new[]
            {
                GameState.Initial(),
                GameState.Initial().Apply(new Move(3, MoveKind.Normal)).Apply(new Move(2, MoveKind.Normal)),
                GameState.Initial().Apply(new Move(3, MoveKind.Normal)).Apply(new Move(3, MoveKind.Queen)).Apply(new Move(4, MoveKind.Normal)),
                GameState.FromGrid(win, true)
            };
        }

        [Test]
        public void DefaultStyle_WithoutRandom_MatchesBot()
        {
            foreach (var state in SamplePositions())
            {
                for (int depth = 1; depth <= 3; depth++)
                {
                    Assert.AreEqual(Bot.ChooseMove(state, depth), StyledBot.ChooseMove(state, depth, BotStyle.Default),
                        $"depth {depth}");
                }
            }
        }

        [Test]
        public void SameSeed_GivesSameMove()
        {
            var state = SamplePositions()[2];
            foreach (var style in Presets())
            {
                var first = StyledBot.ChooseMove(state, 2, style, new Random(1234));
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(first, StyledBot.ChooseMove(state, 2, style, new Random(1234)), style.Name);
                }
            }
        }

        [Test]
        public void SeededTieBreak_StillTakesAWinningMove()
        {
            // Red wins by dropping in column 0 or column 4, so seeds may pick either but never a non-winning move
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[2, 0] = R; grid[3, 0] = R;
            grid[1, 1] = B; grid[2, 1] = B; grid[3, 1] = B;
            var state = GameState.FromGrid(grid, true);
            for (int seed = 0; seed < 20; seed++)
            {
                var move = StyledBot.ChooseMove(state, 1, BotStyle.Default, new Random(seed));
                Assert.AreEqual(EvaluationResult.RedWin, state.Apply(move).Result, $"seed {seed} chose {move}");
            }
        }

        [Test]
        public void Presets_SelfPlayOnlyReturnsLegalMoves()
        {
            var styles = Presets();
            for (int a = 0; a < styles.Length; a++)
            {
                var state = GameState.Initial();
                var random = new Random(a);
                for (int ply = 0; ply < 60 && !state.IsOver; ply++)
                {
                    var style = styles[(a + ply) % styles.Length];
                    var move = StyledBot.ChooseMove(state, 2, style, random);
                    Assert.IsTrue(state.IsLegal(move), $"{style.Name} illegal move at ply {ply}");
                    state = state.Apply(move);
                }
            }
        }

        [Test]
        public void ChooseMove_RejectsBadArguments()
        {
            var state = GameState.Initial();
            Assert.Catch<Exception>(() => StyledBot.ChooseMove(null, 2, BotStyle.Default));
            Assert.Catch<Exception>(() => StyledBot.ChooseMove(state, 2, null));
            Assert.Catch<Exception>(() => StyledBot.ChooseMove(state, 0, BotStyle.Default));

            var grid = EmptyGrid();
            for (int c = 0; c < 4; c++) grid[c, 0] = R;
            var over = GameState.FromGrid(grid, false);
            Assert.Catch<Exception>(() => StyledBot.ChooseMove(over, 2, BotStyle.Default));
        }
    }
}
