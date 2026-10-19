using System;

namespace KineticRank.Logic
{
    // Puzzle chosen in MainMenu and read by the Match scene, mirroring SoloSettings. Holds only the PuzzleDef.Id
    // string so it has no dependency on the pack. Not persisted; defaults to off.
    public static class PuzzleSettings
    {
        public static bool Enabled { get; private set; }
        public static string CurrentId { get; private set; } = "";

        public static void StartPuzzle(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Puzzle id must be non-empty", nameof(id));
            CurrentId = id;
            Enabled = true;
        }

        public static void Clear()
        {
            Enabled = false;
            CurrentId = "";
        }
    }
}
