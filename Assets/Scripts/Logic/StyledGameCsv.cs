using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KineticRank.Logic
{
    // Per-game CSV for fairness round 2 with the brief's opening, depth and repetition columns.
    // Prepends those columns to StyledGameRecord's row; StyledGameRecorder and StyledSelfPlay stay unchanged.
    public static class StyledGameCsv
    {
        public const string Header = "opening,red_depth,blue_depth,max_repeats," + StyledGameRecord.CsvHeader;

        // game supplies the opening and max repeats, record the power-up and draw stats; both must describe the same game
        public static string Row(StyledGame game, StyledGameRecord record, int redDepth, int blueDepth)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (game.Opening == null || game.Opening.Count != record.OpeningPlies || game.Result != record.Result
                || game.Plies != record.Plies || game.Capped != record.Capped || game.Seed != record.Seed
                || game.RedStyle != record.RedStyle || game.BlueStyle != record.BlueStyle)
                throw new ArgumentException("Game and record describe different games.", nameof(record));

            var row = new StringBuilder();
            row.Append(OpeningCell(game.Opening)).Append(',');
            row.Append(redDepth.ToString(CultureInfo.InvariantCulture)).Append(',');
            row.Append(blueDepth.ToString(CultureInfo.InvariantCulture)).Append(',');
            row.Append(game.MaxRepeats.ToString(CultureInfo.InvariantCulture)).Append(',');
            row.Append(record.ToCsvRow());
            return row.ToString();
        }

        // Plays the game twice (StyledSelfPlay for max repeats, StyledGameRecorder for power-up plies); both are deterministic for a given seed
        public static string PlayRow(IReadOnlyList<Move> opening, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seed, int maxPlies)
        {
            var game = StyledSelfPlay.PlayGame(opening, redStyle, blueStyle, redDepth, blueDepth, seed, maxPlies);
            var record = StyledGameRecorder.Record(opening, redStyle, blueStyle, redDepth, blueDepth, seed, maxPlies);
            return Row(game, record, redDepth, blueDepth);
        }

        // Semicolon-separated Kind@Column moves, e.g. "Normal@3;Queen@2", so the opening stays in one CSV cell
        public static string OpeningCell(IReadOnlyList<Move> opening)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            var cell = new StringBuilder();
            for (int i = 0; i < opening.Count; i++)
            {
                if (i > 0) cell.Append(';');
                cell.Append(opening[i].Kind).Append('@').Append(opening[i].Column.ToString(CultureInfo.InvariantCulture));
            }
            return cell.ToString();
        }

        public static List<Move> ParseOpening(string cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            var moves = new List<Move>();
            if (cell.Length == 0) return moves;
            foreach (var part in cell.Split(';'))
            {
                int at = part.IndexOf('@');
                if (at <= 0
                    || !Enum.TryParse(part.Substring(0, at), out MoveKind kind) || !Enum.IsDefined(typeof(MoveKind), kind)
                    || !int.TryParse(part.Substring(at + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int column))
                    throw new FormatException("Bad opening move: " + part);
                moves.Add(new Move(column, kind));
            }
            return moves;
        }
    }
}
