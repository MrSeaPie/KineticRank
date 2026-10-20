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
            summary.Append("Mean replay time per ply (includes opening plies): ").Append(msPerPly.ToString("0.000", CultureInfo.InvariantCulture)).Append(" ms\n");

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
    }
}
