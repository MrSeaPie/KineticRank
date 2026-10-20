using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Four guided onboarding boards (drop, shift, Queen, Cleaner) on the puzzle engine.
    // Each step enables only the power-up it teaches and has exactly one winning first move,
    // so the highlighted hint is never contradicted by another solution.
    public static class TutorialPack
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        public static readonly IReadOnlyList<PuzzleDef> Steps = new List<PuzzleDef>
        {
            new PuzzleDef(
                "tut_01_drop",
                "Drop",
                "Drop a token to complete four in a row along the bottom.",
                CreateDrop(),
                1),
            new PuzzleDef(
                "tut_02_shift",
                "Shift",
                "The middle column is full: dropping into it pushes every token down one row. Shift your red token into the bottom row.",
                CreateShift(),
                1),
            new PuzzleDef(
                "tut_03_queen",
                "Queen",
                "Drop your Queen on the blue stack: it flips every token in that column to your colour.",
                CreateQueen(),
                1),
            new PuzzleDef(
                "tut_04_cleaner",
                "Cleaner",
                "Your Cleaner empties a whole column. Clear the blue blockers, then finish whichever line Blue leaves open.",
                CreateCleaner(),
                2)
        }.AsReadOnly();

        public static PuzzleDef GetById(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            for (int i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Id == id) return Steps[i];
            }
            return null;
        }

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];

        // Red completes the bottom row with a plain drop in column 3; no power-ups
        static GameState CreateDrop()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[0, 1] = B; grid[1, 1] = B; grid[6, 0] = B;
            return GameState.FromGrid(grid, true, false, false, false, false);
        }

        // Column 3 is full (B,R,B,R,B,R from the bottom); dropping there pops the blue bottom token
        // and slides red into row 0; no power-ups
        static GameState CreateShift()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[0, 1] = B; grid[1, 1] = B; grid[6, 0] = B;
            grid[3, 0] = B; grid[3, 1] = R; grid[3, 2] = B;
            grid[3, 3] = R; grid[3, 4] = B; grid[3, 5] = R;
            return GameState.FromGrid(grid, true, false, false, false, false);
        }

        // A Queen on the three-high blue stack in column 3 flips it red and lands a fourth red on top; Red Queen only
        static GameState CreateQueen()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[6, 0] = R;
            grid[3, 0] = B; grid[3, 1] = B; grid[3, 2] = B;
            return GameState.FromGrid(grid, true, true, false, false, false);
        }

        // Blue in column 3 blocks red rows 0 and 1; a Cleaner opens both, Blue can only block row 0,
        // and Red's next drop in column 3 completes row 1; Red Cleaner only
        static GameState CreateCleaner()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[0, 1] = R; grid[1, 1] = R; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = B;
            grid[6, 0] = B; grid[6, 1] = B;
            grid[0, 2] = B; grid[1, 2] = B;
            return GameState.FromGrid(grid, true, false, true, false, false);
        }
    }
}
