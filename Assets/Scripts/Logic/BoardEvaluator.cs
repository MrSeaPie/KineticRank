namespace KineticRank.Logic
{
    public enum CellOwner
    {
        Empty = 0,
        Red = 1,
        Blue = 2
    }

    public enum EvaluationResult
    {
        InProgress,
        RedWin,
        BlueWin,
        Draw
    }

    public static class BoardEvaluator
    {
        public const int Columns = 7;
        public const int Rows = 6;
        public const int ConnectCount = 4;

        public static EvaluationResult Evaluate(int[,] grid)
        {
            if (grid == null) return EvaluationResult.InProgress;

            bool redHasLine = HasWinningLine(grid, 1);
            bool blueHasLine = HasWinningLine(grid, 2);

            if (redHasLine && blueHasLine)
            {
                return EvaluationResult.Draw;
            }
            if (redHasLine)
            {
                return EvaluationResult.RedWin;
            }
            if (blueHasLine)
            {
                return EvaluationResult.BlueWin;
            }

            return EvaluationResult.InProgress;
        }

        public static bool HasWinningLine(int[,] grid, int player)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c <= cols - ConnectCount; c++)
                {
                    bool match = true;
                    for (int i = 0; i < ConnectCount; i++)
                    {
                        if (grid[c + i, r] != player) { match = false; break; }
                    }
                    if (match) return true;
                }
            }

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r <= rows - ConnectCount; r++)
                {
                    bool match = true;
                    for (int i = 0; i < ConnectCount; i++)
                    {
                        if (grid[c, r + i] != player) { match = false; break; }
                    }
                    if (match) return true;
                }
            }

            for (int c = 0; c <= cols - ConnectCount; c++)
            {
                for (int r = 0; r <= rows - ConnectCount; r++)
                {
                    bool match = true;
                    for (int i = 0; i < ConnectCount; i++)
                    {
                        if (grid[c + i, r + i] != player) { match = false; break; }
                    }
                    if (match) return true;
                }
            }

            for (int c = 0; c <= cols - ConnectCount; c++)
            {
                for (int r = ConnectCount - 1; r < rows; r++)
                {
                    bool match = true;
                    for (int i = 0; i < ConnectCount; i++)
                    {
                        if (grid[c + i, r - i] != player) { match = false; break; }
                    }
                    if (match) return true;
                }
            }

            return false;
        }
    }
}