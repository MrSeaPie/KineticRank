using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Bot variant for the fairness tester: the same negamax and move order as Bot, with BotStyle terms
    // added to the leaf score and an optional seeded tie-break at the root.
    // With BotStyle.Default and no Random it picks exactly the move Bot.ChooseMove picks.
    public static class StyledBot
    {
        const int Infinity = Bot.WinScore * 2;
        const int CentreColumn = 3;
        const int Rows = 6;

        static readonly int[] ColumnOrder = { 3, 2, 4, 1, 5, 0, 6 };
        static readonly MoveKind[] KindOrder = { MoveKind.Normal, MoveKind.Queen, MoveKind.Cleaner };
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

        // tieBreak null keeps the earliest best move (Bot's rule); otherwise a uniform pick among equal best moves.
        public static Move ChooseMove(GameState state, int depth, BotStyle style, Random tieBreak = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "depth must be at least 1");
            if (state.IsOver) throw new InvalidOperationException("The game is already over.");

            var best = new List<Move>();
            int bestScore = -Infinity;

            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                int bias = style.Bias(move.Kind);
                // Widen the window by one when collecting ties, so equal scores come back exact
                int floor = tieBreak == null ? bestScore : bestScore - 1;
                int score = -Negamax(state.Apply(move), depth - 1, -Infinity, -(floor - bias), 1, style) + bias;
                if (best.Count == 0 || score > bestScore)
                {
                    best.Clear();
                    best.Add(move);
                    bestScore = score;
                }
                else if (tieBreak != null && score == bestScore)
                {
                    best.Add(move);
                }
            }

            if (best.Count == 0) throw new InvalidOperationException("No legal moves.");
            return tieBreak == null ? best[0] : best[tieBreak.Next(best.Count)];
        }

        static int Negamax(GameState state, int depth, int alpha, int beta, int ply, BotStyle style)
        {
            if (state.IsOver) return TerminalScore(state, ply);
            if (depth == 0) return Evaluate(state, style);

            int best = -Infinity;
            bool any = false;
            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                any = true;
                int score = -Negamax(state.Apply(move), depth - 1, -beta, -alpha, ply + 1, style);
                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            return any ? best : 0;
        }

        // Same as Bot: scored for the side to move, faster wins and slower losses score better.
        static int TerminalScore(GameState state, int ply)
        {
            switch (state.Result)
            {
                case EvaluationResult.RedWin:
                    return state.RedToMove ? Bot.WinScore - ply : -(Bot.WinScore - ply);
                case EvaluationResult.BlueWin:
                    return state.RedToMove ? -(Bot.WinScore - ply) : Bot.WinScore - ply;
                default:
                    return 0;
            }
        }

        // Bot.Evaluate plus the style's centre and power-up terms, for the side to move.
        public static int Evaluate(GameState state, BotStyle style)
        {
            int score = Bot.Evaluate(state);
            int sign = state.RedToMove ? 1 : -1;

            if (style.CentreWeight != 0)
            {
                int centre = 0;
                for (int row = 0; row < Rows; row++)
                {
                    // Cell returns the CellOwner encoding as an int (0 empty, 1 Red, 2 Blue)
                    int owner = state.Cell(CentreColumn, row);
                    if (owner == (int)CellOwner.Red) centre++;
                    else if (owner == (int)CellOwner.Blue) centre--;
                }
                score += sign * centre * style.CentreWeight;
            }

            if (style.HoardWeight != 0)
            {
                int red = (state.QueenAvailable(true) ? 1 : 0) + (state.CleanerAvailable(true) ? 1 : 0);
                int blue = (state.QueenAvailable(false) ? 1 : 0) + (state.CleanerAvailable(false) ? 1 : 0);
                score += sign * (red - blue) * style.HoardWeight;
            }

            return score;
        }
    }
}
