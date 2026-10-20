using System;
using System.Diagnostics;
using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class BotTests
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];

        static void AssertSameState(GameState a, GameState b)
        {
            for (int c = 0; c < GameState.Columns; c++)
            {
                for (int r = 0; r < GameState.Rows; r++)
                {
                    Assert.AreEqual(a.Cell(c, r), b.Cell(c, r), $"column {c} row {r}");
                }
            }
            Assert.AreEqual(a.RedToMove, b.RedToMove);
            Assert.AreEqual(a.QueenAvailable(true), b.QueenAvailable(true));
            Assert.AreEqual(a.QueenAvailable(false), b.QueenAvailable(false));
            Assert.AreEqual(a.CleanerAvailable(true), b.CleanerAvailable(true));
            Assert.AreEqual(a.CleanerAvailable(false), b.CleanerAvailable(false));
        }

        [Test]
        public void ChooseMove_TakesImmediateWin()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[0, 1] = B; grid[1, 1] = B; grid[2, 1] = B;
            var state = GameState.FromGrid(grid, true);
            var move = Bot.ChooseMove(state, 1);
            Assert.AreEqual(EvaluationResult.RedWin, state.Apply(move).Result);
        }

        [Test]
        public void ChooseMove_BlocksImmediateLoss()
        {
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[1, 0] = B; grid[2, 0] = B;
            grid[0, 1] = R; grid[1, 1] = R;
            var state = GameState.FromGrid(grid, true);
            var after = state.Apply(Bot.ChooseMove(state, 2));
            foreach (var reply in after.LegalMoves())
            {
                Assert.AreNotEqual(EvaluationResult.BlueWin, after.Apply(reply).Result);
            }
        }

        [Test]
        public void ChooseMove_FindsWinThroughFullColumnPop()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            int[] column = { B, R, B, R, B, R };
            for (int r = 0; r < GameState.Rows; r++) grid[3, r] = column[r];
            var state = GameState.FromGrid(grid, true);
            var move = Bot.ChooseMove(state, 1);
            Assert.AreEqual(EvaluationResult.RedWin, state.Apply(move).Result);
        }

        [Test]
        public void ChooseMove_SamePositionGivesSameMove()
        {
            var state = GameState.Initial()
                .Apply(new Move(3, MoveKind.Normal))
                .Apply(new Move(2, MoveKind.Normal));
            var first = state.Apply(Bot.ChooseMove(state, 3));
            for (int i = 0; i < 3; i++)
            {
                AssertSameState(first, state.Apply(Bot.ChooseMove(state, 3)));
            }
        }

        [Test]
        public void ChooseMove_SelfPlayOnlyReturnsLegalMoves()
        {
            var state = GameState.Initial();
            for (int ply = 0; ply < 200 && !state.IsOver; ply++)
            {
                var move = Bot.ChooseMove(state, 2);
                Assert.IsTrue(state.IsLegal(move), $"illegal move at ply {ply}");
                state = state.Apply(move);
            }
        }

        [Test]
        public void ChooseMove_GameOver_Throws()
        {
            var grid = EmptyGrid();
            for (int c = 0; c < 4; c++) grid[c, 0] = R;
            var state = GameState.FromGrid(grid, false);
            Assert.Throws<InvalidOperationException>(() => Bot.ChooseMove(state, 2));
        }

        [Test]
        public void ChooseMove_DepthFour_FinishesQuickly()
        {
            var watch = Stopwatch.StartNew();
            Bot.ChooseMove(GameState.Initial(), 4);
            watch.Stop();
            Assert.Less(watch.ElapsedMilliseconds, 15000, "depth 4 search too slow for WebGL");
        }

        [Test]
        public void BotTiers_LabelsAreExplicit()
        {
            Assert.AreEqual("Pine (Beginner)", BotTiers.Title(BotTier.Pine));
            Assert.AreEqual("Ebony (Expert)", BotTiers.Title(BotTier.Ebony));
            Assert.AreEqual("Still learning the game", BotTiers.Blurb(BotTier.Pine));
            Assert.AreEqual("Very hard to beat", BotTiers.Blurb(BotTier.Ebony));
            foreach (BotTier tier in Enum.GetValues(typeof(BotTier)))
            {
                Assert.IsNotEmpty(BotTiers.Difficulty(tier));
                Assert.IsNotEmpty(BotTiers.Blurb(tier));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => BotTiers.Difficulty((BotTier)5));
        }
    }
}
