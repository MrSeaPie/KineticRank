using System;

namespace KineticRank.Logic
{
    /// <summary>
    /// Tracks match alternation across rematches for hot-seat and Solo modes.
    /// In accordance with Owner Directive 1:
    /// - Players retain their assigned colours and Elo ratings across rematches.
    /// - Initial first mover alternates each rematch (Match 0: Red, Match 1: Blue, Match 2: Red, ...).
    /// - Resetting returns to Match 0 (Red first).
    /// - Puzzle and tutorial modes specify their own side-to-move and bypass rematch alternation.
    /// </summary>
    public static class RematchTracker
    {
        private static int rematchIndex = 0;

        /// <summary>
        /// Total consecutive rematches played in the current session.
        /// </summary>
        public static int RematchIndex => rematchIndex;

        /// <summary>
        /// Returns true if Red moves first in the current match, false if Blue moves first.
        /// </summary>
        public static bool CurrentFirstMoverIsRed => FirstMoverIsRedForRematch(rematchIndex);

        /// <summary>
        /// Pure helper: returns true if Red moves first for a given rematch index.
        /// Match 0 (initial match) starts with Red. Odd rematches start with Blue.
        /// </summary>
        public static bool FirstMoverIsRedForRematch(int index)
        {
            if (index < 0) index = 0;
            return (index % 2) == 0;
        }

        /// <summary>
        /// Registers a rematch, incrementing the counter and flipping the first mover.
        /// </summary>
        public static void RegisterRematch()
        {
            rematchIndex++;
        }

        /// <summary>
        /// Explicitly sets the rematch counter (e.g. for testing or state restore).
        /// </summary>
        public static void SetRematchIndex(int index)
        {
            rematchIndex = Math.Max(0, index);
        }

        /// <summary>
        /// Resets rematch tracking back to initial state (Match 0, Red first).
        /// Called when returning to or launching from MainMenu.
        /// </summary>
        public static void Reset()
        {
            rematchIndex = 0;
        }
    }
}
