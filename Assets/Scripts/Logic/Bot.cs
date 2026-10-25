using System;

namespace KineticRank.Logic
{
    // Deterministic computer opponent: negamax with alpha-beta over GameState.
    // There is no randomness; the same position and depth always give the same move.
    public static class Bot
    {
        public const int WinScore = 1000000;
        const int Infinity = WinScore * 2;

        const int OpenTwoScore = 10;
        const int OpenThreeScore = 50;
        const int PowerUpScore = 15;

        static readonly int[] ColumnOrder = { 3, 2, 4, 1, 5, 0, 6 };
        static readonly MoveKind[] KindOrder = { MoveKind.Normal, MoveKind.Queen, MoveKind.Cleaner };

        // Kind-major, centre-first ordering. Ties keep the earliest move in this order.
        static readonly Move[] Order = BuildOrder();

        static Move[] BuildOrder()
        {
            var order = new Move[ColumnOrder.Length * KindOrder.Length];
            int i = 0;
            foreach (var kind in KindOrder)
            {
                foreach (int column in ColumnOrder)
                {
                    order[i++] = new Move(column, kind);
                }
            }
            return order;
        }

        public static Move ChooseMove(GameState state, int depth)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "depth must be at least 1");
            if (state.IsOver) throw new InvalidOperationException("The game is already over.");

            Move best = default(Move);
            bool found = false;
            int bestScore = -Infinity;

            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                int score = -Negamax(state.Apply(move), depth - 1, -Infinity, -bestScore, 1);
                if (!found || score > bestScore)
                {
                    best = move;
                    bestScore = score;
                    found = true;
                }
            }

            if (!found) throw new InvalidOperationException("No legal moves.");
            return best;
        }

        static int Negamax(GameState state, int depth, int alpha, int beta, int ply)
        {
            if (state.IsOver) return TerminalScore(state, ply);
            if (depth == 0) return Evaluate(state);

            int best = -Infinity;
            bool any = false;
            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                any = true;
                int score = -Negamax(state.Apply(move), depth - 1, -beta, -alpha, ply + 1);
                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            return any ? best : 0;
        }

        // Scored for the side to move. Faster wins and slower losses score better.
        static int TerminalScore(GameState state, int ply)
        {
            switch (state.Result)
            {
                case EvaluationResult.RedWin:
                    return state.RedToMove ? WinScore - ply : -(WinScore - ply);
                case EvaluationResult.BlueWin:
                    return state.RedToMove ? -(WinScore - ply) : WinScore - ply;
                default:
                    return 0;
            }
        }

        // Static evaluation for the side to move: open 2s and 3s per colour, plus unused power-ups.
        public static int Evaluate(GameState state)
        {
            int red = (int)CellOwner.Red;
            int blue = (int)CellOwner.Blue;
            int score = 0;

            score += ScanWindows(state, 1, 0, red, blue);
            score += ScanWindows(state, 0, 1, red, blue);
            score += ScanWindows(state, 1, 1, red, blue);
            score += ScanWindows(state, 1, -1, red, blue);

            if (state.QueenAvailable(true)) score += PowerUpScore;
            if (state.CleanerAvailable(true)) score += PowerUpScore;
            if (state.QueenAvailable(false)) score -= PowerUpScore;
            if (state.CleanerAvailable(false)) score -= PowerUpScore;

            return state.RedToMove ? score : -score;
        }

        static int ScanWindows(GameState state, int dc, int dr, int red, int blue)
        {
            int n = BoardEvaluator.ConnectCount;
            int score = 0;
            for (int c = 0; c < GameState.Columns; c++)
            {
                int endC = c + dc * (n - 1);
                if (endC < 0 || endC >= GameState.Columns) continue;
                for (int r = 0; r < GameState.Rows; r++)
                {
                    int endR = r + dr * (n - 1);
                    if (endR < 0 || endR >= GameState.Rows) continue;

                    int reds = 0, blues = 0;
                    for (int i = 0; i < n; i++)
                    {
                        int cell = state.Cell(c + dc * i, r + dr * i);
                        if (cell == red) reds++;
                        else if (cell == blue) blues++;
                    }
                    if (blues == 0) score += WindowScore(reds);
                    else if (reds == 0) score -= WindowScore(blues);
                }
            }
            return score;
        }

        static int WindowScore(int count)
        {
            if (count == 3) return OpenThreeScore;
            if (count == 2) return OpenTwoScore;
            return 0;
        }
    }
}
