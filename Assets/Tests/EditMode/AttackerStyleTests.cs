using System;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    // Attacker style: StyledBot.ThreatPoints scoring and its effect on StyledBot.Evaluate
    public class AttackerStyleTests
    {
        // Red on columns 0-2 of the bottom row: one open-3 window (4) and one open-2 window (1)
        static int[,] RedOpenThree()
        {
            var grid = new int[GameState.Columns, GameState.Rows];
            grid[0, 0] = 1;
            grid[1, 0] = 1;
            grid[2, 0] = 1;
            return grid;
        }

        static int[,] SwapColours(int[,] grid)
        {
            var swapped = (int[,])grid.Clone();
            for (int c = 0; c < GameState.Columns; c++)
            {
                for (int r = 0; r < GameState.Rows; r++)
                {
                    if (swapped[c, r] == 1) swapped[c, r] = 2;
                    else if (swapped[c, r] == 2) swapped[c, r] = 1;
                }
            }
            return swapped;
        }

        [Test]
        public void ThreatPoints_EmptyBoard_IsZero()
        {
            Assert.AreEqual(0, StyledBot.ThreatPoints(GameState.Initial()));
        }

        [Test]
        public void ThreatPoints_RedOpenThree_ScoresThreeWindowPlusTwoWindow()
        {
            var state = GameState.FromGrid(RedOpenThree(), false);
            Assert.AreEqual(5, StyledBot.ThreatPoints(state));
        }

        [Test]
        public void ThreatPoints_SwappedColours_Negates()
        {
            var grid = RedOpenThree();
            grid[4, 0] = 2;
            grid[4, 1] = 2;
            grid[3, 0] = 2;
            var red = GameState.FromGrid(grid, true);
            var blue = GameState.FromGrid(SwapColours(grid), true);
            Assert.AreEqual(-StyledBot.ThreatPoints(red), StyledBot.ThreatPoints(blue));
        }

        [Test]
        public void ThreatPoints_BlockedWindows_ScoreNothing()
        {
            var grid = RedOpenThree();
            grid[3, 0] = 2;
            Assert.AreEqual(0, StyledBot.ThreatPoints(GameState.FromGrid(grid, true)));
        }

        [Test]
        public void ThreatPoints_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StyledBot.ThreatPoints(null));
        }

        [Test]
        public void Evaluate_Attacker_AddsWeightedThreatsForSideToMove()
        {
            var redToMove = GameState.FromGrid(RedOpenThree(), true);
            var blueToMove = GameState.FromGrid(RedOpenThree(), false);
            int weight = BotStyle.Attacker.ThreatWeight;

            Assert.AreEqual(5 * weight,
                StyledBot.Evaluate(redToMove, BotStyle.Attacker) - StyledBot.Evaluate(redToMove, BotStyle.Default));
            Assert.AreEqual(-5 * weight,
                StyledBot.Evaluate(blueToMove, BotStyle.Attacker) - StyledBot.Evaluate(blueToMove, BotStyle.Default));
        }

        [Test]
        public void Styles_OnlyAttackerUsesThreatWeight()
        {
            Assert.Greater(BotStyle.Attacker.ThreatWeight, 0);
            Assert.Contains(BotStyle.Attacker, (System.Collections.ICollection)BotStyle.All);
            Assert.AreEqual(0, new BotStyle("Six", 1, 2, 3, 4, 5).ThreatWeight);
            foreach (var style in BotStyle.All)
            {
                if (style != BotStyle.Attacker) Assert.AreEqual(0, style.ThreatWeight, style.Name);
            }
        }
    }
}
