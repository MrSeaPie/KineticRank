using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KineticRank.Logic
{
    public enum MatchEndReason { BoardResult, Timeout, Resignation, Abandoned }

    // Public fields allow Unity's JSON serializer to persist the record without Unity dependencies.
    [Serializable]
    public sealed class RecordedMove
    {
        public int Column;
        public MoveKind Kind;

        public RecordedMove() { }
        public RecordedMove(Move move) { Column = move.Column; Kind = move.Kind; }
        public Move ToMove() => new Move(Column, Kind);
    }

    [Serializable]
    public sealed class MatchRecord
    {
        public const int CurrentFormatVersion = 1;
        public const int CurrentRulesVersion = 1;

        public int FormatVersion = CurrentFormatVersion;
        public int RulesVersion = CurrentRulesVersion;
        public string Mode;
        public bool RedStarts = true;
        public bool PowerUpsEnabled = true;
        public string RedLabel;
        public string BlueLabel;
        public int RedRating;
        public int BlueRating;
        public List<RecordedMove> Moves = new List<RecordedMove>();
        public EvaluationResult Result;
        public MatchEndReason EndReason;
        public int Plies;
        // Round-trip UTC strings preserve timestamps through field-based JSON serialization.
        public string StartedUtc;
        public string EndedUtc;

        // Reconstructs the board, not an external timeout/resignation outcome.
        public GameState Replay()
        {
            ValidateVersions();
            if (Moves == null) throw new InvalidOperationException("Move list is missing.");
            if (Plies != Moves.Count) throw new InvalidOperationException("Ply count does not match the move list.");
            var state = GameState.FromGrid(new int[GameState.Columns, GameState.Rows], RedStarts,
                PowerUpsEnabled, PowerUpsEnabled, PowerUpsEnabled, PowerUpsEnabled);
            for (int i = 0; i < Moves.Count; i++)
            {
                if (Moves[i] == null || !state.IsLegal(Moves[i].ToMove()))
                    throw new InvalidOperationException("Illegal recorded move at ply " + (i + 1));
                state = state.Apply(Moves[i].ToMove());
            }
            if (EndReason == MatchEndReason.BoardResult && state.Result != Result)
                throw new InvalidOperationException("Recorded result does not match the replayed board.");
            if (EndReason != MatchEndReason.BoardResult && state.IsOver)
                throw new InvalidOperationException("An external end reason cannot follow a terminal board.");
            return state;
        }

        // Columns are one-based for display: N1, Q4, C7.
        public string CompactNotation()
        {
            if (Moves == null) throw new InvalidOperationException("Move list is missing.");
            var text = new StringBuilder();
            for (int i = 0; i < Moves.Count; i++)
            {
                var move = Moves[i];
                if (move == null || move.Column < 0 || move.Column >= GameState.Columns ||
                    !Enum.IsDefined(typeof(MoveKind), move.Kind))
                    throw new InvalidOperationException("Invalid recorded move at ply " + (i + 1));
                if (i > 0) text.Append(' ');
                text.Append(move.Kind == MoveKind.Queen ? 'Q' : move.Kind == MoveKind.Cleaner ? 'C' : 'N');
                text.Append((move.Column + 1).ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        public static string UtcTimestamp(DateTime timestamp)
        {
            if (timestamp.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Timestamp must explicitly be UTC.", nameof(timestamp));
            return timestamp.ToString("O", CultureInfo.InvariantCulture);
        }

        private void ValidateVersions()
        {
            if (FormatVersion != CurrentFormatVersion || RulesVersion != CurrentRulesVersion)
                throw new NotSupportedException("Unsupported match record format or rules version.");
            if (!Enum.IsDefined(typeof(MatchEndReason), EndReason))
                throw new InvalidOperationException("Unknown match end reason.");
        }
    }
}
