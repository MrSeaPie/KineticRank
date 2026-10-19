using System;

namespace KineticRank.Logic
{
    // Runtime helpers for playing a verified puzzle: judge the player's move and pick the defender's reply.
    // Built on PuzzleVerifier, so depth is limited to PuzzleVerifier.MaxSupportedMoves.
    public static class PuzzleLine
    {
        // Smallest n in 1..maxMoves for which the side to move has a forced win, or null if there is none
        public static int? MovesToForcedWin(GameState position, int maxMoves)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            if (maxMoves > PuzzleVerifier.MaxSupportedMoves)
                throw new ArgumentOutOfRangeException(nameof(maxMoves), $"maxMoves must be at most {PuzzleVerifier.MaxSupportedMoves}");

            for (int n = 1; n <= maxMoves; n++)
                if (PuzzleVerifier.HasForcedWin(position, n)) return n;
            return null;
        }

        // Any move that still forces a win counts, not only the puzzle's canonical solution
        public static bool IsCorrectAttackerMove(GameState position, Move move, int movesLeft)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            if (movesLeft > PuzzleVerifier.MaxSupportedMoves)
                throw new ArgumentOutOfRangeException(nameof(movesLeft), $"movesLeft must be at most {PuzzleVerifier.MaxSupportedMoves}");
            if (movesLeft < 1 || !position.IsLegal(move)) return false;

            bool attackerRed = position.RedToMove;
            var next = position.Apply(move);
            if (next.IsOver) return IsWinFor(next.Result, attackerRed);
            if (movesLeft == 1) return false;

            foreach (var reply in next.LegalMoves())
            {
                var after = next.Apply(reply);
                if (after.IsOver)
                {
                    if (!IsWinFor(after.Result, attackerRed)) return false;
                    continue;
                }
                if (!PuzzleVerifier.HasForcedWin(after, movesLeft - 1)) return false;
            }
            return true;
        }

        // Longest resistance: the reply after which the attacker needs the most moves; ties go to the first in LegalMoves order.
        // A reply that escapes the forced win (draw, defender win, or no win within movesLeft) ranks above every other reply.
        public static Move DefenderReply(GameState afterAttackerMove, bool attackerRed, int movesLeft)
        {
            if (afterAttackerMove == null) throw new ArgumentNullException(nameof(afterAttackerMove));
            if (afterAttackerMove.IsOver) throw new InvalidOperationException("The game is over, so there is no reply to make.");
            if (afterAttackerMove.RedToMove == attackerRed) throw new ArgumentException("It must be the defender's turn.", nameof(attackerRed));

            var replies = afterAttackerMove.LegalMoves();
            Move best = replies[0];
            int bestScore = -1;
            foreach (var reply in replies)
            {
                int score = Resistance(afterAttackerMove.Apply(reply), attackerRed, movesLeft);
                if (score > bestScore)
                {
                    best = reply;
                    bestScore = score;
                }
            }
            return best;
        }

        static int Resistance(GameState afterReply, bool attackerRed, int movesLeft)
        {
            if (afterReply.IsOver) return IsWinFor(afterReply.Result, attackerRed) ? 0 : int.MaxValue;
            return MovesToForcedWin(afterReply, movesLeft) ?? int.MaxValue;
        }

        // A double-four Draw is not a win
        static bool IsWinFor(EvaluationResult result, bool red) =>
            result == (red ? EvaluationResult.RedWin : EvaluationResult.BlueWin);
    }
}
