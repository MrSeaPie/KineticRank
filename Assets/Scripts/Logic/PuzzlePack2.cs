using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Second puzzle pack for daily rotation: puz_11..puz_20, each with exactly one winning first move.
    // These lean on the kinetic rules (full-column push, Queen flip, Cleaner sweep) as well as forks.
    public static class PuzzlePack2
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static int[,] EmptyGrid() => new int[GameState.Columns, GameState.Rows];
        static GameState StandardStart(int[,] grid, bool redTurn) => GameState.FromGrid(grid, redTurn, false, false, false, false);
        static GameState QueenStart(int[,] grid) => GameState.FromGrid(grid, true, true, false, false, false);
        static GameState CleanerStart(int[,] grid) => GameState.FromGrid(grid, true, false, true, false, false);

        public static readonly IReadOnlyList<PuzzleDef> Puzzles = new List<PuzzleDef>
        {
            new PuzzleDef(
                "puz_11_kinetic_push",
                "Kinetic Push",
                "A full column still takes a drop: the bottom token pops out and the rest slide down.",
                CreatePuzzle11(),
                1),
            new PuzzleDef(
                "puz_12_bottom_pop",
                "Bottom Pop",
                "Pop the blocker out from under your row.",
                CreatePuzzle12(),
                1),
            new PuzzleDef(
                "puz_13_royal_reversal",
                "Royal Reversal",
                "The Queen flips every token in her column.",
                CreatePuzzle13(),
                1),
            new PuzzleDef(
                "puz_14_clean_sweep",
                "Clean Sweep",
                "Clear the blockers to open two threats in one column.",
                CreatePuzzle14(),
                2),
            new PuzzleDef(
                "puz_15_flip_the_blocker",
                "Flip the Blocker",
                "Turn an enemy blocker into your own token.",
                CreatePuzzle15(),
                1),
            new PuzzleDef(
                "puz_16_corner_fork",
                "Corner Fork",
                "One drop builds two threats at once.",
                CreatePuzzle16(),
                2),
            new PuzzleDef(
                "puz_17_double_edged_push",
                "Double-Edged Push",
                "A push moves every token in the column. Check what it does for Blue too.",
                CreatePuzzle17(),
                1),
            new PuzzleDef(
                "puz_18_diagonal_turncoat",
                "Diagonal Turncoat",
                "Flip one enemy token to finish the diagonal.",
                CreatePuzzle18(),
                1),
            new PuzzleDef(
                "puz_19_block_and_fork",
                "Block and Fork",
                "Your only defence is also a double attack.",
                CreatePuzzle19(),
                2),
            new PuzzleDef(
                "puz_20_sliding_diagonal",
                "Sliding Diagonal",
                "Push a full column so a token slides into the diagonal.",
                CreatePuzzle20(),
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

        // Solution: Normal@3 (push gives a vertical four in rows 2-5)
        static GameState CreatePuzzle11()
        {
            var grid = EmptyGrid();
            grid[2, 0] = B; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = R; grid[3, 2] = B; grid[3, 3] = R; grid[3, 4] = R; grid[3, 5] = R;
            grid[4, 0] = B; grid[4, 1] = B;
            return StandardStart(grid, true);
        }

        // Solution: Normal@3 (push slides the Red token into row 0)
        static GameState CreatePuzzle12()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[2, 0] = R;
            grid[3, 0] = B; grid[3, 1] = R; grid[3, 2] = B; grid[3, 3] = B; grid[3, 4] = R; grid[3, 5] = B;
            grid[4, 0] = B;
            return StandardStart(grid, true);
        }

        // Solution: Queen@3
        static GameState CreatePuzzle13()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R; grid[6, 0] = R;
            grid[3, 0] = B; grid[3, 1] = B; grid[3, 2] = B;
            return QueenStart(grid);
        }

        // Solution: Cleaner@3, then Red wins at (3,0) or (3,1)
        static GameState CreatePuzzle14()
        {
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[0, 1] = R; grid[0, 2] = B;
            grid[1, 0] = B; grid[1, 1] = R; grid[1, 2] = B;
            grid[2, 0] = R; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = B;
            grid[4, 0] = R; grid[4, 1] = B;
            grid[5, 0] = R; grid[6, 0] = R;
            return CleanerStart(grid);
        }

        // Solution: Queen@3
        static GameState CreatePuzzle15()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[1, 0] = R;
            grid[2, 0] = R; grid[2, 1] = B;
            grid[3, 0] = B; grid[3, 1] = R;
            grid[4, 0] = B; grid[5, 0] = B;
            return QueenStart(grid);
        }

        // Solution: Normal@2 (threats at (2,3) and (3,2))
        static GameState CreatePuzzle16()
        {
            var grid = EmptyGrid();
            grid[0, 0] = B; grid[0, 1] = B; grid[0, 2] = R;
            grid[1, 0] = R; grid[1, 1] = B; grid[1, 2] = R;
            grid[2, 0] = R; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = B;
            grid[4, 0] = B;
            grid[6, 0] = R;
            return StandardStart(grid, true);
        }

        // Solution: Normal@6. Pushing column 3 also completes Blue's row 1, a double-four Draw.
        static GameState CreatePuzzle17()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[0, 1] = B;
            grid[1, 0] = B; grid[1, 1] = B;
            grid[2, 0] = R; grid[2, 1] = B;
            grid[3, 0] = B; grid[3, 1] = R; grid[3, 2] = B; grid[3, 3] = R; grid[3, 4] = R; grid[3, 5] = R;
            grid[4, 0] = B; grid[4, 1] = B;
            grid[5, 0] = B;
            grid[6, 0] = R; grid[6, 1] = R; grid[6, 2] = R;
            return StandardStart(grid, true);
        }

        // Solution: Queen@2 (flips (2,2) to Red for the (0,0)-(3,3) diagonal)
        static GameState CreatePuzzle18()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R;
            grid[1, 0] = B; grid[1, 1] = R;
            grid[2, 0] = R; grid[2, 1] = R; grid[2, 2] = B;
            grid[3, 0] = B; grid[3, 1] = B; grid[3, 2] = R; grid[3, 3] = R;
            grid[5, 0] = B; grid[6, 0] = B;
            return QueenStart(grid);
        }

        // Solution: Normal@2 (blocks Blue's row 2 and threatens (3,3) and (4,0))
        static GameState CreatePuzzle19()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[0, 1] = B; grid[0, 2] = B;
            grid[1, 0] = R; grid[1, 1] = R; grid[1, 2] = B; grid[1, 3] = R;
            grid[2, 0] = B; grid[2, 1] = R;
            grid[3, 0] = B; grid[3, 1] = R; grid[3, 2] = B;
            return StandardStart(grid, true);
        }

        // Solution: Normal@3 (push slides (3,5) into (3,4) for the (0,1)-(3,4) diagonal)
        static GameState CreatePuzzle20()
        {
            var grid = EmptyGrid();
            grid[0, 0] = R; grid[0, 1] = R;
            grid[1, 0] = R; grid[1, 1] = B; grid[1, 2] = R;
            grid[2, 0] = B; grid[2, 1] = R; grid[2, 2] = B; grid[2, 3] = R;
            grid[3, 0] = R; grid[3, 1] = B; grid[3, 2] = B; grid[3, 3] = R; grid[3, 4] = B; grid[3, 5] = R;
            grid[4, 0] = B; grid[4, 1] = B;
            grid[6, 0] = B;
            return StandardStart(grid, true);
        }
    }
}
