using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Exact position key: 2 bits per cell (0 empty, 1 Red, 2 Blue) plus side to move and the four power-up flags.
    // Cells 0..31 fill Low; the remaining cells, then the side and flags, fill High. There are no collisions.
    public readonly struct PositionKey : IEquatable<PositionKey>
    {
        public readonly ulong Low;
        public readonly ulong High;

        public PositionKey(ulong low, ulong high)
        {
            Low = low;
            High = high;
        }

        public static PositionKey From(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            ulong low = 0;
            ulong high = 0;
            int index = 0;
            for (int column = 0; column < GameState.Columns; column++)
            {
                for (int row = 0; row < GameState.Rows; row++, index++)
                {
                    ulong owner = (ulong)(state.Cell(column, row) & 3);
                    if (index < 32) low |= owner << (index * 2);
                    else high |= owner << ((index - 32) * 2);
                }
            }

            // 42 cells leave 10 in High (bits 0..19), so the flags start at bit 20
            int bit = (index - 32) * 2;
            if (state.RedToMove) high |= 1UL << bit;
            if (state.QueenAvailable(true)) high |= 1UL << (bit + 1);
            if (state.CleanerAvailable(true)) high |= 1UL << (bit + 2);
            if (state.QueenAvailable(false)) high |= 1UL << (bit + 3);
            if (state.CleanerAvailable(false)) high |= 1UL << (bit + 4);
            return new PositionKey(low, high);
        }

        public bool Equals(PositionKey other) => Low == other.Low && High == other.High;
        public override bool Equals(object obj) => obj is PositionKey other && Equals(other);
        public override int GetHashCode() => unchecked((int)(Low ^ (Low >> 29) ^ (High * 0x9E3779B97F4A7C15UL) ^ (High >> 32)));
    }

    internal enum TableBound : byte
    {
        Exact = 0,
        Lower = 1,
        Upper = 2
    }

    // Opt-in search cache for CachedStyledBot. An entry is only used at exactly the depth it was stored at,
    // so a cached search returns the same move and score as the uncached one at the same depth.
    // A table is bound to one BotStyle; switching style clears it.
    public sealed class TranspositionTable
    {
        public const int DefaultCapacity = 1 << 20;

        // Win scores are stored as distance from the stored node, not from the root
        const int MateThreshold = Bot.WinScore / 2;

        struct Entry
        {
            public int Score;
            public int Depth;
            public TableBound Bound;
        }

        readonly Dictionary<PositionKey, Entry> entries = new Dictionary<PositionKey, Entry>();

        public TranspositionTable(int capacity = DefaultCapacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity must be at least 1");
            Capacity = capacity;
        }

        public int Capacity { get; }
        public BotStyle Style { get; private set; }
        public int Count => entries.Count;
        public long Probes { get; private set; }
        public long Hits { get; private set; }

        public void Clear()
        {
            entries.Clear();
            Style = null;
        }

        internal void Bind(BotStyle style)
        {
            if (ReferenceEquals(Style, style)) return;
            entries.Clear();
            Style = style;
        }

        internal bool TryGet(PositionKey key, int depth, int ply, out int score, out TableBound bound)
        {
            Probes++;
            if (entries.TryGetValue(key, out var entry) && entry.Depth == depth)
            {
                Hits++;
                score = FromStored(entry.Score, ply);
                bound = entry.Bound;
                return true;
            }
            score = 0;
            bound = TableBound.Exact;
            return false;
        }

        internal void Store(PositionKey key, int depth, int ply, int score, TableBound bound)
        {
            // Simple memory cap: start over when full. Dropping entries never changes results.
            if (entries.Count >= Capacity && !entries.ContainsKey(key)) entries.Clear();
            entries[key] = new Entry { Score = ToStored(score, ply), Depth = depth, Bound = bound };
        }

        static int ToStored(int score, int ply)
        {
            if (score >= MateThreshold) return score + ply;
            if (score <= -MateThreshold) return score - ply;
            return score;
        }

        static int FromStored(int score, int ply)
        {
            if (score >= MateThreshold) return score - ply;
            if (score <= -MateThreshold) return score + ply;
            return score;
        }
    }

    // StyledBot with an opt-in TranspositionTable. Same move order, leaf score, root bias and tie-break rule,
    // so for the same state, depth, style and tie-break seed it picks the same move as StyledBot.ChooseMove.
    public static class CachedStyledBot
    {
        const int Infinity = Bot.WinScore * 2;

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

        public static Move ChooseMove(GameState state, int depth, BotStyle style, TranspositionTable table, Random tieBreak = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "depth must be at least 1");
            if (state.IsOver) throw new InvalidOperationException("The game is already over.");

            table.Bind(style);

            var best = new List<Move>();
            int bestScore = -Infinity;

            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                int bias = style.Bias(move.Kind);
                // Widen the window by one when collecting ties, so equal scores come back exact
                int floor = tieBreak == null ? bestScore : bestScore - 1;
                int score = -Negamax(state.Apply(move), depth - 1, -Infinity, -(floor - bias), 1, style, table) + bias;
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

        static int Negamax(GameState state, int depth, int alpha, int beta, int ply, BotStyle style, TranspositionTable table)
        {
            if (state.IsOver) return TerminalScore(state, ply);
            if (depth == 0) return StyledBot.Evaluate(state, style);

            var key = PositionKey.From(state);
            if (table.TryGet(key, depth, ply, out int cached, out var bound))
            {
                if (bound == TableBound.Exact) return cached;
                if (bound == TableBound.Lower && cached >= beta) return cached;
                if (bound == TableBound.Upper && cached <= alpha) return cached;
            }

            int alphaStart = alpha;
            int best = -Infinity;
            bool any = false;
            foreach (var move in Order)
            {
                if (!state.IsLegal(move)) continue;
                any = true;
                int score = -Negamax(state.Apply(move), depth - 1, -beta, -alpha, ply + 1, style, table);
                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            if (!any) best = 0;

            var stored = best <= alphaStart ? TableBound.Upper : best >= beta ? TableBound.Lower : TableBound.Exact;
            table.Store(key, depth, ply, best, stored);
            return best;
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
    }
}
