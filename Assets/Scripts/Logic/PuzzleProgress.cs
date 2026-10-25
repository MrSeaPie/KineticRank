using System.Collections.Generic;
using UnityEngine;

namespace KineticRank.Logic
{
    // Solved flags per puzzle, one PlayerPrefs key each. Kept apart from rank, Elo and solo ladder keys
    // so puzzle results never affect competitive data.
    public static class PuzzleProgress
    {
        public const string KeyPrefix = "puzzle_solved_";

        public static string Key(string id) => KeyPrefix + id;

        public static bool IsSolved(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return PlayerPrefs.GetInt(Key(id), 0) == 1;
        }

        public static void MarkSolved(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
        }

        public static void ClearSolved(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.DeleteKey(Key(id));
            PlayerPrefs.Save();
        }

        // Duplicate ids count once so a repeated entry can't inflate the total
        public static int SolvedCount(IEnumerable<string> ids)
        {
            if (ids == null) return 0;
            var seen = new HashSet<string>();
            var count = 0;
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id) && seen.Add(id) && IsSolved(id)) count++;
            return count;
        }
    }
}
