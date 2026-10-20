using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // One playable puzzle as the menu and runtime see it. Mirrors the agreed PuzzleDef shape so PuzzlePack can feed it.
    public sealed class PuzzleEntry
    {
        public string Id { get; }
        public string Title { get; }
        public string Hint { get; }
        public GameState Start { get; }
        public int MovesN { get; }

        public PuzzleEntry(string id, string title, string hint, GameState start, int movesN)
        {
            Id = id;
            Title = title ?? "";
            Hint = hint ?? "";
            Start = start;
            MovesN = movesN;
        }
    }

    // Registry of puzzles shown in the Puzzles menu. Ships with a few seed positions (proven unique in EditMode tests)
    // and accepts more through Register, so the menu does not depend on PuzzlePack being merged.
    public static class PuzzleCatalog
    {
        const int R = (int)CellOwner.Red;
        const int B = (int)CellOwner.Blue;

        static readonly List<PuzzleEntry> entries = new List<PuzzleEntry>();
        static readonly Dictionary<string, PuzzleEntry> byId = new Dictionary<string, PuzzleEntry>();

        static PuzzleCatalog() => Register(Seeds());

        public static IReadOnlyList<PuzzleEntry> All => entries;

        public static bool TryGet(string id, out PuzzleEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out entry);
        }

        // Adds entries in order; skips nulls, empty or repeated ids, missing or finished start positions and
        // out-of-range move counts. Returns how many were added.
        public static int Register(IEnumerable<PuzzleEntry> items)
        {
            if (items == null) return 0;
            var added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id) || byId.ContainsKey(item.Id)) continue;
                if (item.Start == null || item.Start.IsOver) continue;
                if (item.MovesN < 1 || item.MovesN > PuzzleVerifier.MaxSupportedMoves) continue;
                entries.Add(item);
                byId.Add(item.Id, item);
                added++;
            }
            return added;
        }

        // Drops anything registered later and restores the seeds; used by tests to keep the static state predictable
        public static void ResetToSeeds()
        {
            entries.Clear();
            byId.Clear();
            Register(Seeds());
        }

        public static IEnumerable<PuzzleEntry> Seeds()
        {
            // Same positions as PuzzleVerifierTests, where each has exactly one winning first move
            var finish = new int[GameState.Columns, GameState.Rows];
            finish[0, 0] = R; finish[1, 0] = R; finish[2, 0] = R;
            finish[6, 0] = B; finish[6, 1] = B; finish[5, 0] = B;
            yield return new PuzzleEntry("seed_finish_line", "Finish the line",
                "Red to move. Win in 1.", NoPowerUps(finish), 1);

            var shift = new int[GameState.Columns, GameState.Rows];
            shift[0, 0] = B; shift[0, 1] = R;
            shift[1, 0] = B; shift[1, 1] = R;
            shift[2, 0] = R; shift[2, 1] = R;
            shift[3, 0] = R; shift[3, 1] = B; shift[3, 2] = R; shift[3, 3] = B; shift[3, 4] = R; shift[3, 5] = B;
            yield return new PuzzleEntry("seed_shift_win", "Kinetic shift",
                "Red to move. Win in 1. Playing into a full column drops its bottom token.", NoPowerUps(shift), 1);
        }

        static GameState NoPowerUps(int[,] grid) => GameState.FromGrid(grid, true, false, false, false, false);
    }
}
