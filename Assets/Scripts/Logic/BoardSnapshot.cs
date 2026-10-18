using System;

namespace KineticRank.Logic
{
    // Builds a GameState from the live board so the bot searches the same position the player sees.
    // isToken(column, row, red) mirrors BoardManager.IsToken; row 0 is the bottom, as in GameManager.EvaluateBoard.
    public static class BoardSnapshot
    {
        public static GameState Capture(Func<int, int, bool, bool> isToken, bool redToMove,
            bool redQueen, bool redCleaner, bool blueQueen, bool blueCleaner)
        {
            if (isToken == null) throw new ArgumentNullException(nameof(isToken));

            var grid = new int[GameState.Columns, GameState.Rows];
            for (int c = 0; c < GameState.Columns; c++)
            for (int r = 0; r < GameState.Rows; r++)
            {
                bool red = isToken(c, r, true);
                bool blue = isToken(c, r, false);
                if (red && blue) throw new InvalidOperationException($"Cell {c},{r} reports both colours.");
                grid[c, r] = red ? (int)CellOwner.Red : blue ? (int)CellOwner.Blue : 0;
            }
            return GameState.FromGrid(grid, redToMove, redQueen, redCleaner, blueQueen, blueCleaner);
        }
    }
}
