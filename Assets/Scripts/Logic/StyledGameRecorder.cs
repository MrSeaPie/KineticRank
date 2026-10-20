using System;
using System.Collections.Generic;
using System.Text;

namespace KineticRank.Logic
{
    // How a fairness-tester game ended when nobody won: Cap is the ply limit, Rules is a draw the rules declared
    public enum StyledDrawKind
    {
        None,
        Cap,
        Rules
    }

    // Per-game power-up and draw stats for fairness round 2 (brief items 6 and 7).
    // Plies are 1-based and include the opening moves.
    public sealed class StyledGameRecord
    {
        public readonly IReadOnlyList<Move> Moves;
        public readonly int OpeningPlies;
        public readonly string RedStyle;
        public readonly string BlueStyle;
        public readonly int? Seed;
        public readonly EvaluationResult Result;
        public readonly int Plies;
        public readonly bool Capped;
        public readonly IReadOnlyList<int> RedQueenPlies;
        public readonly IReadOnlyList<int> RedCleanerPlies;
        public readonly IReadOnlyList<int> BlueQueenPlies;
        public readonly IReadOnlyList<int> BlueCleanerPlies;

        public StyledGameRecord(IReadOnlyList<Move> moves, int openingPlies, string redStyle, string blueStyle, int? seed, EvaluationResult result, bool capped,
            IReadOnlyList<int> redQueenPlies, IReadOnlyList<int> redCleanerPlies, IReadOnlyList<int> blueQueenPlies, IReadOnlyList<int> blueCleanerPlies)
        {
            Moves = moves ?? throw new ArgumentNullException(nameof(moves));
            OpeningPlies = openingPlies;
            RedStyle = redStyle;
            BlueStyle = blueStyle;
            Seed = seed;
            Result = result;
            Plies = moves.Count;
            Capped = capped;
            RedQueenPlies = redQueenPlies;
            RedCleanerPlies = redCleanerPlies;
            BlueQueenPlies = blueQueenPlies;
            BlueCleanerPlies = blueCleanerPlies;
        }

        public bool RedWon => Result == EvaluationResult.RedWin;
        public bool BlueWon => Result == EvaluationResult.BlueWin;
        public bool HasWinner => RedWon || BlueWon;

        public bool WinnerUsedQueen => RedWon ? RedQueenPlies.Count > 0 : BlueWon && BlueQueenPlies.Count > 0;
        public bool WinnerUsedCleaner => RedWon ? RedCleanerPlies.Count > 0 : BlueWon && BlueCleanerPlies.Count > 0;

        // Kind of the move that completed the winning line; null when nobody won
        public MoveKind? FinalMoveKind => HasWinner && Moves.Count > 0 ? Moves[Moves.Count - 1].Kind : (MoveKind?)null;

        public StyledDrawKind DrawKind => HasWinner ? StyledDrawKind.None : Capped ? StyledDrawKind.Cap : StyledDrawKind.Rules;

        public const string CsvHeader = "red_style,blue_style,seed,result,plies,draw_kind,final_move_kind,red_queen_plies,red_cleaner_plies,blue_queen_plies,blue_cleaner_plies,winner_used_queen,winner_used_cleaner";

        public string ToCsvRow()
        {
            var row = new StringBuilder();
            row.Append(RedStyle).Append(',');
            row.Append(BlueStyle).Append(',');
            row.Append(Seed.HasValue ? Seed.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "").Append(',');
            row.Append(Result).Append(',');
            row.Append(Plies.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            row.Append(DrawKind).Append(',');
            row.Append(FinalMoveKind.HasValue ? FinalMoveKind.Value.ToString() : "").Append(',');
            row.Append(JoinPlies(RedQueenPlies)).Append(',');
            row.Append(JoinPlies(RedCleanerPlies)).Append(',');
            row.Append(JoinPlies(BlueQueenPlies)).Append(',');
            row.Append(JoinPlies(BlueCleanerPlies)).Append(',');
            row.Append(WinnerUsedQueen ? "1" : "0").Append(',');
            row.Append(WinnerUsedCleaner ? "1" : "0");
            return row.ToString();
        }

        // Semicolon-separated so the list stays in one CSV cell
        static string JoinPlies(IReadOnlyList<int> plies)
        {
            var text = new StringBuilder();
            for (int i = 0; i < plies.Count; i++)
            {
                if (i > 0) text.Append(';');
                text.Append(plies[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }
    }

    // Replays StyledSelfPlay.PlayGame's loop with the same tie-break Random, so Result, Plies and Capped
    // match PlayGame for the same arguments. StyledSelfPlay stays unchanged.
    public static class StyledGameRecorder
    {
        public static StyledGameRecord Record(IReadOnlyList<Move> opening, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seed, int maxPlies)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (redStyle == null) throw new ArgumentNullException(nameof(redStyle));
            if (blueStyle == null) throw new ArgumentNullException(nameof(blueStyle));
            if (maxPlies < opening.Count) throw new ArgumentOutOfRangeException(nameof(maxPlies), "maxPlies must cover the opening");

            var tieBreak = seed.HasValue ? new Random(seed.Value) : null;
            var state = GameState.Initial();
            var moves = new List<Move>();
            var redQueen = new List<int>();
            var redCleaner = new List<int>();
            var blueQueen = new List<int>();
            var blueCleaner = new List<int>();

            foreach (var move in opening)
            {
                if (state.IsOver || !state.IsLegal(move))
                    throw new ArgumentException("Opening contains an illegal move.", nameof(opening));
                state = Play(state, move, moves, redQueen, redCleaner, blueQueen, blueCleaner);
            }

            while (!state.IsOver && moves.Count < maxPlies)
            {
                bool red = state.RedToMove;
                var move = StyledBot.ChooseMove(state, red ? redDepth : blueDepth, red ? redStyle : blueStyle, tieBreak);
                state = Play(state, move, moves, redQueen, redCleaner, blueQueen, blueCleaner);
            }

            return new StyledGameRecord(moves, opening.Count, redStyle.Name, blueStyle.Name, seed, state.Result, !state.IsOver,
                redQueen, redCleaner, blueQueen, blueCleaner);
        }

        // seedBase null records every game unseeded; otherwise game i uses seedBase + i, as StyledSelfPlay.Run does
        public static List<StyledGameRecord> RecordAll(IEnumerable<IReadOnlyList<Move>> openings, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seedBase, int maxPlies)
        {
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            var records = new List<StyledGameRecord>();
            int i = 0;
            foreach (var opening in openings)
            {
                int? seed = seedBase.HasValue ? unchecked(seedBase.Value + i) : (int?)null;
                records.Add(Record(opening, redStyle, blueStyle, redDepth, blueDepth, seed, maxPlies));
                i++;
            }
            return records;
        }

        static GameState Play(GameState state, Move move, List<Move> moves, List<int> redQueen, List<int> redCleaner, List<int> blueQueen, List<int> blueCleaner)
        {
            bool red = state.RedToMove;
            moves.Add(move);
            int ply = moves.Count;
            if (move.Kind == MoveKind.Queen) (red ? redQueen : blueQueen).Add(ply);
            else if (move.Kind == MoveKind.Cleaner) (red ? redCleaner : blueCleaner).Add(ply);
            return state.Apply(move);
        }
    }
}
