using System;
using System.Collections.Generic;
using System.Globalization;

namespace KineticRank.Logic
{
    // Picks one puzzle per UTC day from PuzzleCatalog. The same date gives the same puzzle for every player:
    // a stable FNV-1a hash of yyyy-MM-dd, never System.Random or string.GetHashCode (randomized per process).
    public static class DailyPuzzle
    {
        const uint FnvOffset = 2166136261u;
        const uint FnvPrime = 16777619u;

        public static PuzzleEntry Today() => ForDate(DateTime.UtcNow);

        public static PuzzleEntry ForDate(DateTime utcDate) => ForDate(utcDate, PuzzleCatalog.All);

        // Returns null when the list is null or empty
        public static PuzzleEntry ForDate(DateTime utcDate, IReadOnlyList<PuzzleEntry> entries)
        {
            if (entries == null || entries.Count == 0) return null;
            return entries[Index(utcDate, entries.Count)];
        }

        public static int Index(DateTime utcDate, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            return (int)(Fnv1a(DateKey(utcDate)) % (uint)count);
        }

        // Local times are converted so a player's time zone never changes the day's puzzle
        public static string DateKey(DateTime date)
        {
            var utc = date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date;
            return utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static uint Fnv1a(string text)
        {
            var hash = FnvOffset;
            if (text == null) return hash;
            foreach (var c in text)
            {
                hash ^= (byte)c;
                hash *= FnvPrime;
            }
            return hash;
        }
    }
}
