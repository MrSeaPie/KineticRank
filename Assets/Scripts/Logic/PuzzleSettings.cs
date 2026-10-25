using System;

namespace KineticRank.Logic
{
    // Puzzle chosen in MainMenu and read by the Match scene, mirroring SoloSettings. Holds only the PuzzleDef.Id
    // string so it has no dependency on the pack. Not persisted; defaults to off.
    public static class PuzzleSettings
    {
        public static bool Enabled { get; private set; }
        public static string CurrentId { get; private set; } = "";
        // Side the player solves for (the side to move in the puzzle's start position)
        public static bool AttackerIsRed { get; private set; } = true;

        public static void StartPuzzle(string id, bool attackerIsRed = true)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Puzzle id must be non-empty", nameof(id));
            CurrentId = id;
            AttackerIsRed = attackerIsRed;
            Enabled = true;
        }

        public static void Clear()
        {
            Enabled = false;
            CurrentId = "";
            AttackerIsRed = true;
        }
    }
}
