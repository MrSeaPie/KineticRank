using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    // Fairness round 2, brief items 4, 6 and 7: per-game CSV with timing for the style round-robin.
    // Each round-robin game is replayed through StyledGameRecorder for power-up stats; Bot and GameState stay unchanged.
    public class FairnessCsvRunTests
    {
        public const string TimingColumn = "ms_per_ply";

        static readonly BotStyle[] TwoStyles = { BotStyle.Default, BotStyle.Spender };

        static List<IReadOnlyList<Move>> FewOpenings() => StyledSelfPlay.TwoPlyOpenings().Take(3).ToList();

        [Test]
        public void BuildCsv_HasHeaderAndOneRowPerGame()
        {
            var report = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 0, 60, true);
            var csv = BuildCsv(report, TwoStyles, out double msPerPly);
            var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(StyledGameCsv.Header + "," + TimingColumn, lines[0]);
            Assert.AreEqual(report.Games + 1, lines.Length);
            Assert.That(msPerPly, Is.GreaterThanOrEqualTo(0.0));
        }

        [Test]
        public void BuildCsv_RowsMatchRoundRobinGames()
        {
            var report = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, 7, 60, true);
            var lines = BuildCsv(report, TwoStyles, out _).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var games = report.Pairings.SelectMany(p => p.AAsRed.Games.Concat(p.BAsRed.Games)).ToList();
            for (int i = 0; i < games.Count; i++)
            {
                StringAssert.StartsWith(StyledGameCsv.OpeningCell(games[i].Opening) + ",1,1,", lines[i + 1]);
            }
        }

        [Test]
        public void BuildCsv_RejectsUnknownStyle()
        {
            var report = StyleRoundRobin.Run(TwoStyles, FewOpenings(), 1, null, 60, false);
            Assert.Throws<ArgumentException>(() => BuildCsv(report, new[] { BotStyle.Default }, out _));
        }

        [Test]
        public void PowerUpSummary_CountsHandBuiltRows()
        {
            var header = (StyledGameCsv.Header + "," + TimingColumn).Split(',');
            var csv = new StringBuilder();
            csv.Append(string.Join(",", header)).Append('\n');
            csv.Append(HandRow(header, new Dictionary<string, string>
            {
                { "red_style", "Hoarder" }, { "blue_style", "Spender" }, { "draw_kind", "None" }, { "final_move_kind", "Normal" },
                { "blue_queen_plies", "1" }, { "blue_cleaner_plies", "5;30" }, { "winner_used_queen", "0" }, { "winner_used_cleaner", "0" },
            })).Append('\n');
            csv.Append(HandRow(header, new Dictionary<string, string>
            {
                { "red_style", "Spender" }, { "blue_style", "Hoarder" }, { "draw_kind", "Cap" },
                { "red_queen_plies", "70" }, { "winner_used_queen", "0" }, { "winner_used_cleaner", "0" },
            })).Append('\n');

            var summary = PowerUpSummary(csv.ToString());
            StringAssert.Contains("| None | 1 |", summary);
            StringAssert.Contains("| Cap | 1 |", summary);
            StringAssert.Contains("| Rules | 0 |", summary);
            StringAssert.Contains("| Normal | 1 |", summary);
            StringAssert.Contains("| Hoarder | 2 | 0 (0.0 %) | 0 (0.0 %) | - | - |", summary);
            StringAssert.Contains("| Spender | 2 | 2 (100.0 %) | 1 (50.0 %) | 35.5 | 5.0 |", summary);
            StringAssert.Contains("| 1-2 (opening) | 1 |", summary);
            StringAssert.Contains("| 61+ | 1 |", summary);
            StringAssert.Contains("| never | 2 |", summary);
            StringAssert.Contains("Winner used Queen: 0 of 1 decisive games", summary);
        }

        [Test]
        public void PowerUpSummary_RejectsMissingColumn()
        {
            Assert.Throws<ArgumentException>(() => PowerUpSummary("red_style,blue_style\nDefault,Default\n"));
        }

        // Long run: see Docs/FAIRNESS_REPORT_ROUND2.md for the command. Output goes to TestResults/.
        [Test, Explicit("Long fairness run"), Category("FairnessLong"), Timeout(86400000)]
        public void Csv_Depth2_TwoPlyOpenings()
        {
            var watch = Stopwatch.StartNew();
            var report = StyleRoundRobin.Run(BotStyle.All, StyledSelfPlay.TwoPlyOpenings(), 2, 1000, 160, true);
            double runSeconds = watch.Elapsed.TotalSeconds;
            var csv = BuildCsv(report, BotStyle.All, out double msPerPly);

            var summary = new StringBuilder();
            summary.Append(report.ToText()).Append('\n');
            summary.Append("Round-robin wall time: ").Append(runSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(" s\n");
            summary.Append("Mean replay time per ply (includes opening plies): ").Append(msPerPly.ToString("0.000", CultureInfo.InvariantCulture)).Append(" ms\n\n");
            summary.Append(PowerUpSummary(csv));

            Directory.CreateDirectory("TestResults");
            File.WriteAllText(Path.Combine("TestResults", "fairness_round2_depth2_games.csv"), csv);
            File.WriteAllText(Path.Combine("TestResults", "fairness_round2_depth2_summary.txt"), summary.ToString());
            TestContext.WriteLine(summary.ToString());
            Assert.AreEqual(report.Games + 1, csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        // One CSV row per round-robin game, in pairing order (A as Red, then B as Red); msPerPly is the mean replay time
        static string BuildCsv(RoundRobinReport report, IReadOnlyList<BotStyle> styles, out double msPerPly)
        {
            var byName = styles.ToDictionary(s => s.Name);
            var csv = new StringBuilder();
            csv.Append(StyledGameCsv.Header).Append(',').Append(TimingColumn).Append('\n');
            double totalMs = 0;
            long totalPlies = 0;
            foreach (var pairing in report.Pairings)
            {
                foreach (var game in pairing.AAsRed.Games.Concat(pairing.BAsRed.Games))
                {
                    if (!byName.TryGetValue(game.RedStyle, out var red) || !byName.TryGetValue(game.BlueStyle, out var blue))
                        throw new ArgumentException("Style missing from styles: " + game.RedStyle + "/" + game.BlueStyle, nameof(styles));
                    var watch = Stopwatch.StartNew();
                    var record = StyledGameRecorder.Record(game.Opening, red, blue, report.Depth, report.Depth, game.Seed, report.MaxPlies);
                    double ms = watch.Elapsed.TotalMilliseconds;
                    totalMs += ms;
                    totalPlies += record.Plies;
                    double perPly = record.Plies == 0 ? 0.0 : ms / record.Plies;
                    csv.Append(StyledGameCsv.Row(game, record, report.Depth, report.Depth)).Append(',')
                       .Append(perPly.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
                }
            }
            msPerPly = totalPlies == 0 ? 0.0 : totalMs / totalPlies;
            return csv.ToString();
        }

        // Markdown tables of draw kinds, winning move kinds, power-up use per style and first power-up timing.
        // Columns are found by header name; ply numbers include the opening plies, so plies 1-2 are the fixed opening.
        static string PowerUpSummary(string csv)
        {
            var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var header = lines[0].TrimEnd('\r').Split(',');
            int Col(string name)
            {
                int i = Array.IndexOf(header, name);
                if (i < 0) throw new ArgumentException("Missing column: " + name, nameof(csv));
                return i;
            }
            int redStyle = Col("red_style"), blueStyle = Col("blue_style"), drawKind = Col("draw_kind"), finalKind = Col("final_move_kind");
            int redQueen = Col("red_queen_plies"), redCleaner = Col("red_cleaner_plies"), blueQueen = Col("blue_queen_plies"), blueCleaner = Col("blue_cleaner_plies");
            int winnerQueen = Col("winner_used_queen"), winnerCleaner = Col("winner_used_cleaner");

            var drawKinds = new Dictionary<string, int> { { "None", 0 }, { "Cap", 0 }, { "Rules", 0 } };
            var finalKinds = new Dictionary<string, int>();
            var styleNames = new List<string>();
            // per style: sides played, Queen used, Cleaner used, sum of first Queen ply, sum of first Cleaner ply
            var styleUse = new Dictionary<string, long[]>();
            string[] bucketNames = { "1-2 (opening)", "3-20", "21-60", "61+", "never" };
            var buckets = new int[bucketNames.Length];
            int decisive = 0, decisiveQueen = 0, decisiveCleaner = 0;

            for (int row = 1; row < lines.Length; row++)
            {
                var cells = lines[row].TrimEnd('\r').Split(',');
                string kind = cells[drawKind];
                drawKinds[kind] = drawKinds.TryGetValue(kind, out var n) ? n + 1 : 1;
                if (kind == "None")
                {
                    decisive++;
                    if (cells[winnerQueen] == "1") decisiveQueen++;
                    if (cells[winnerCleaner] == "1") decisiveCleaner++;
                    string final = cells[finalKind];
                    finalKinds[final] = finalKinds.TryGetValue(final, out var f) ? f + 1 : 1;
                }
                AddSide(cells[redStyle], cells[redQueen], cells[redCleaner]);
                AddSide(cells[blueStyle], cells[blueQueen], cells[blueCleaner]);
            }

            void AddSide(string style, string queenCell, string cleanerCell)
            {
                if (!styleUse.TryGetValue(style, out var use))
                {
                    use = new long[5];
                    styleUse[style] = use;
                    styleNames.Add(style);
                }
                use[0]++;
                int queen = FirstPly(queenCell), cleaner = FirstPly(cleanerCell);
                if (queen > 0) { use[1]++; use[3] += queen; }
                if (cleaner > 0) { use[2]++; use[4] += cleaner; }
                int first = queen > 0 && cleaner > 0 ? Math.Min(queen, cleaner) : Math.Max(queen, cleaner);
                buckets[first <= 0 ? 4 : first <= 2 ? 0 : first <= 20 ? 1 : first <= 60 ? 2 : 3]++;
            }

            var text = new StringBuilder();
            text.Append("Power-up and draw breakdown (").Append(lines.Length - 1).Append(" games)\n\n");
            text.Append("| Draw kind | Games |\n|---|---|\n");
            foreach (var pair in drawKinds) text.Append("| ").Append(pair.Key).Append(" | ").Append(pair.Value).Append(" |\n");
            text.Append("\n| Winning move kind | Games |\n|---|---|\n");
            foreach (var pair in finalKinds.OrderByDescending(p => p.Value)) text.Append("| ").Append(pair.Key).Append(" | ").Append(pair.Value).Append(" |\n");
            text.Append("\nWinner used Queen: ").Append(decisiveQueen).Append(" of ").Append(decisive).Append(" decisive games\n");
            text.Append("Winner used Cleaner: ").Append(decisiveCleaner).Append(" of ").Append(decisive).Append(" decisive games\n");
            text.Append("\n| Style | Sides | Used Queen | Used Cleaner | Mean first Queen ply | Mean first Cleaner ply |\n|---|---|---|---|---|---|\n");
            foreach (var name in styleNames)
            {
                var use = styleUse[name];
                text.Append("| ").Append(name).Append(" | ").Append(use[0]).Append(" | ").Append(Pct(use[1], use[0])).Append(" | ").Append(Pct(use[2], use[0]))
                    .Append(" | ").Append(Mean(use[3], use[1])).Append(" | ").Append(Mean(use[4], use[2])).Append(" |\n");
            }
            text.Append("\n| First power-up ply (per side) | Sides |\n|---|---|\n");
            for (int i = 0; i < bucketNames.Length; i++) text.Append("| ").Append(bucketNames[i]).Append(" | ").Append(buckets[i]).Append(" |\n");
            return text.ToString();
        }

        // First ply in a ";"-separated ply list, or 0 when the list is empty
        static int FirstPly(string cell) =>
            string.IsNullOrEmpty(cell) ? 0 : int.Parse(cell.Split(';')[0], CultureInfo.InvariantCulture);

        static string Pct(long count, long total) =>
            count + " (" + (total == 0 ? 0.0 : 100.0 * count / total).ToString("0.0", CultureInfo.InvariantCulture) + " %)";

        static string Mean(long sum, long count) =>
            count == 0 ? "-" : ((double)sum / count).ToString("0.0", CultureInfo.InvariantCulture);

        static string HandRow(string[] header, Dictionary<string, string> values) =>
            string.Join(",", header.Select(h => values.TryGetValue(h, out var v) ? v : ""));
    }
}
