using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Deterministic puzzle check: exhaustive AND/OR search for a forced win within N moves of the side to move.
    // A move is column plus token kind, so a Normal and a Queen drop in the same column are different solutions.
    public static class PuzzleVerifier
    {
        // N = 3 is 5 plies (about 21^5 positions at worst); deeper searches are too slow without pruning
        public const int MaxSupportedMoves = 3;

        public static List<Move> WinningFirstMoves(GameState position, int moves)
        {
            if (moves < 1 || moves > MaxSupportedMoves)
                throw new ArgumentOutOfRangeException(nameof(moves), $"moves must be 1..{MaxSupportedMoves}");

            var winners = new List<Move>();
            if (position.IsOver) return winners;

            bool attackerRed = position.RedToMove;
            foreach (var move in position.LegalMoves())
                if (MoveForcesWin(position, move, attackerRed, moves)) winners.Add(move);
            return winners;
        }

        public static bool HasForcedWin(GameState position, int moves) => WinningFirstMoves(position, moves).Count > 0;

        // A valid puzzle has exactly one winning first move
        public static bool IsValidPuzzle(GameState position, int moves) => WinningFirstMoves(position, moves).Count == 1;

        static bool MoveForcesWin(GameState state, Move move, bool attackerRed, int movesLeft)
        {
            var next = state.Apply(move);
            if (next.IsOver) return IsWinFor(next.Result, attackerRed);
            return movesLeft > 1 && DefenderLoses(next, attackerRed, movesLeft - 1);
        }

        static bool AttackerWins(GameState state, bool attackerRed, int movesLeft)
        {
            foreach (var move in state.LegalMoves())
                if (MoveForcesWin(state, move, attackerRed, movesLeft)) return true;
            return false;
        }

        // Every defender reply must lose: either it hands the attacker a line, or the attacker still forces a win
        static bool DefenderLoses(GameState state, bool attackerRed, int movesLeft)
        {
            var replies = state.LegalMoves();
            if (replies.Count == 0) return false;

            foreach (var reply in replies)
            {
                var next = state.Apply(reply);
                if (next.IsOver)
                {
                    if (!IsWinFor(next.Result, attackerRed)) return false;
                    continue;
                }
                if (!AttackerWins(next, attackerRed, movesLeft)) return false;
            }
            return true;
        }

        // A double-four Draw is not a win
        static bool IsWinFor(EvaluationResult result, bool red) =>
            result == (red ? EvaluationResult.RedWin : EvaluationResult.BlueWin);
    }
}
