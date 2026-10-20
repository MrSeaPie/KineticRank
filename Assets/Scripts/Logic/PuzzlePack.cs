using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    public static class PuzzlePack
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];
        static GameState StandardStart(int[,] grid, bool redTurn) => GameState.FromGrid(grid, redTurn, false, false, false, false);

        public static readonly IReadOnlyList<PuzzleDef> Puzzles = new List<PuzzleDef>
        {
            new PuzzleDef(
                "puz_01_horizontal_line",
                "Direct Horizontal Four",
                "Complete the open horizontal line in one move.",
                CreatePuzzle01(),
                1),
            new PuzzleDef(
                "puz_02_vertical_strike",
                "Vertical Ascent",
                "Stack the fourth token vertically to seal the win.",
                CreatePuzzle02(),
                1),
            new PuzzleDef(
                "puz_03_diagonal_climb",
                "Ascending Diagonal",
                "Find the climbing diagonal four-in-a-row.",
                CreatePuzzle03(),
                1),
            new PuzzleDef(
                "puz_04_split_threat",
                "Split Threat",
                "Fill the gap between tokens to complete the line.",
                CreatePuzzle04(),
                1),
            new PuzzleDef(
                "puz_05_shift_drop",
                "Kinetic Downshift",
                "Drop a token into a full column to shift and align four.",
                CreatePuzzle05(),
                1),
            new PuzzleDef(
                "puz_06_double_threat_setup",
                "Double Threat Setup",
                "Create an unblockable dual-threat fork.",
                CreatePuzzle06(),
                2),
            new PuzzleDef(
                "puz_07_block_and_counter",
                "Counter-Attack Fork",
                "Set up an inescapable two-way trap.",
                CreatePuzzle07(),
                2),
            new PuzzleDef(
                "puz_08_flank_fork",
                "Flank Ambush",
                "Extend along the row to force an unpreventable win.",
                CreatePuzzle08(),
                2),
            new PuzzleDef(
                "puz_09_high_diagonal",
                "High Diagonal",
                "Spot the elevated diagonal finish.",
                CreatePuzzle09(),
                1),
            new PuzzleDef(
                "puz_10_central_pillar",
                "Central Pillar",
                "Complete the center vertical column for the win.",
                CreatePuzzle10(),
                1)
        }.AsReadOnly();

        public static IReadOnlyList<PuzzleDef> All => Puzzles;

        public static PuzzleDef GetById(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            for (int i = 0; i < Puzzles.Count; i++)
            {
                if (Puzzles[i].Id == id) return Puzzles[i];
            }
            return null;
        }

        static GameState CreatePuzzle01()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[4, 0] = B; grid[5, 0] = B; grid[6, 0] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle02()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[0, 1] = R; grid[0, 2] = R;
            grid[1, 0] = B; grid[2, 0] = B; grid[3, 0] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle03()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R;
            grid[1, 0] = B; grid[1, 1] = R;
            grid[2, 0] = B; grid[2, 1] = R; grid[2, 2] = R;
            grid[3, 0] = R; grid[3, 1] = B; grid[3, 2] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle04()
        {
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[2, 0] = R; grid[4, 0] = R;
            grid[5, 0] = B; grid[5, 1] = B;
            grid[6, 0] = B; grid[6, 1] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle05()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[0, 1] = B; grid[0, 2] = B; grid[0, 3] = R;
            grid[1, 0] = B; grid[1, 1] = R; grid[1, 2] = R;
            grid[2, 0] = B; grid[2, 1] = R;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle06()
        {
            var grid = EmptyGrid();
            grid[2, 0] = R; grid[3, 0] = R;
            grid[5, 0] = B; grid[5, 1] = B;
            grid[6, 0] = B; grid[6, 1] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle07()
        {
            var grid = EmptyGrid();
            grid[1, 0] = R; grid[3, 0] = R;
            grid[5, 0] = B; grid[6, 0] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle08()
        {
            var grid = EmptyGrid();
            grid[2, 0] = R; grid[4, 0] = R;
            grid[0, 0] = B; grid[6, 0] = B;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle09()
        {
            var grid = EmptyGrid();
            grid[1, 0] = B;
            grid[2, 0] = R; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = B; grid[3, 2] = R;
            grid[4, 0] = R; grid[4, 1] = R; grid[4, 2] = B; grid[4, 3] = R;
            grid[5, 0] = B; grid[5, 1] = B; grid[5, 2] = R; grid[5, 3] = R;
            return StandardStart(grid, true);
        }

        static GameState CreatePuzzle10()
        {
            var grid = EmptyGrid();
            grid[3, 0] = R; grid[3, 1] = R; grid[3, 2] = R;
            grid[2, 0] = B; grid[2, 1] = B;
            grid[4, 0] = B; grid[4, 1] = B;
            return StandardStart(grid, true);
        }
    }
}
