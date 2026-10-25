using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KineticRank.Logic
{
    // One style pairing in the round-robin: every opening is played twice, once with each style as Red (first mover).
    // A mirror pairing (a style against itself) is played once per opening, since swapping colours changes nothing.
    public sealed class StylePairing
    {
        public readonly string StyleA;
        public readonly string StyleB;
        public readonly StyledReport AAsRed;
        public readonly StyledReport BAsRed;   // empty for a mirror pairing

        public StylePairing(string styleA, string styleB, StyledReport aAsRed, StyledReport bAsRed)
        {
            StyleA = styleA ?? throw new ArgumentNullException(nameof(styleA));
            StyleB = styleB ?? throw new ArgumentNullException(nameof(styleB));
            AAsRed = aAsRed ?? throw new ArgumentNullException(nameof(aAsRed));
            BAsRed = bAsRed ?? throw new ArgumentNullException(nameof(bAsRed));
        }

        public bool Mirror => StyleA == StyleB;
        public int Games => AAsRed.Games.Count + BAsRed.Games.Count;
        public int AWins => AAsRed.RedWins + BAsRed.BlueWins;
        public int BWins => AAsRed.BlueWins + BAsRed.RedWins;
        public int Draws => AAsRed.Draws + BAsRed.Draws;
        public int CappedGames => AAsRed.CappedGames + BAsRed.CappedGames;
        public int RepetitionGames => AAsRed.RepetitionGames + BAsRed.RepetitionGames;
        public int FirstMoverWins => AAsRed.RedWins + BAsRed.RedWins;
        public int SecondMoverWins => AAsRed.BlueWins + BAsRed.BlueWins;
    }

    public sealed class RoundRobinReport
    {
        public readonly IReadOnlyList<StylePairing> Pairings;
        public readonly int Depth;
        public readonly int Openings;
        public readonly int? SeedBase;
        public readonly int MaxPlies;
        public readonly int Games;
        public readonly int FirstMoverWins;
        public readonly int SecondMoverWins;
        public readonly int Draws;
        public readonly int CappedGames;
        public readonly int RepetitionGames;

        public RoundRobinReport(IReadOnlyList<StylePairing> pairings, int depth, int openings, int? seedBase, int maxPlies)
        {
            Pairings = pairings ?? throw new ArgumentNullException(nameof(pairings));
            Depth = depth;
            Openings = openings;
            SeedBase = seedBase;
            MaxPlies = maxPlies;
            foreach (var pairing in pairings)
            {
                Games += pairing.Games;
                FirstMoverWins += pairing.FirstMoverWins;
                SecondMoverWins += pairing.SecondMoverWins;
                Draws += pairing.Draws;
                CappedGames += pairing.CappedGames;
                RepetitionGames += pairing.RepetitionGames;
            }
        }

        // Markdown table, ready to paste into Docs/FAIRNESS_REPORT_ROUND2.md
        public string ToText()
        {
            string seed = SeedBase.HasValue ? SeedBase.Value.ToString(CultureInfo.InvariantCulture) : "none";
            var text = new StringBuilder();
            text.AppendLine($"Style round-robin: depth {Depth}, {Openings} openings, seedBase {seed}, ply cap {MaxPlies}");
            text.AppendLine();
            text.AppendLine("| A | B | Games | A wins | B wins | Draws (capped) | First-mover wins | Second-mover wins | Repetition games |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var p in Pairings)
            {
                text.AppendLine($"| {p.StyleA} | {p.StyleB} | {p.Games} | {p.AWins} | {p.BWins} | {p.Draws} ({p.CappedGames}) | {p.FirstMoverWins} ({Percent(p.FirstMoverWins, p.Games)}) | {p.SecondMoverWins} ({Percent(p.SecondMoverWins, p.Games)}) | {p.RepetitionGames} |");
            }
            text.AppendLine($"| All | All | {Games} | - | - | {Draws} ({CappedGames}) | {FirstMoverWins} ({Percent(FirstMoverWins, Games)}) | {SecondMoverWins} ({Percent(SecondMoverWins, Games)}) | {RepetitionGames} |");
            return text.ToString();
        }

        static string Percent(int part, int whole) =>
            whole == 0 ? "0.0 %" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + " %";
    }

    // Fairness tester round 2: every style against every other style at one shared depth, both colour orders.
    // Built on StyledSelfPlay, which stays unchanged; with no seed every game is fully deterministic.
    public static class StyleRoundRobin
    {
        public static RoundRobinReport Run(IReadOnlyList<BotStyle> styles, IReadOnlyList<IReadOnlyList<Move>> openings, int depth, int? seedBase, int maxPlies, bool includeMirrors) =>
            Run(GameState.Initial(), styles, openings, depth, seedBase, maxPlies, includeMirrors);

        // Same round-robin from a given start position, e.g. StyledSelfPlay.TraditionalStart() for the no-power-up mode
        public static RoundRobinReport Run(GameState start, IReadOnlyList<BotStyle> styles, IReadOnlyList<IReadOnlyList<Move>> openings, int depth, int? seedBase, int maxPlies, bool includeMirrors)
        {
            if (styles == null) throw new ArgumentNullException(nameof(styles));
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            if (styles.Count == 0) throw new ArgumentException("At least one style is required.", nameof(styles));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "depth must be at least 1");

            // Pairings are keyed by name, so names must be unique
            var names = new HashSet<string>();
            foreach (var style in styles)
            {
                if (style == null) throw new ArgumentException("styles contains null.", nameof(styles));
                if (!names.Add(style.Name)) throw new ArgumentException($"Duplicate style name {style.Name}.", nameof(styles));
            }

            var pairings = new List<StylePairing>();
            for (int i = 0; i < styles.Count; i++)
            {
                for (int j = i; j < styles.Count; j++)
                {
                    if (i == j && !includeMirrors) continue;
                    var a = styles[i];
                    var b = styles[j];
                    // Both colour orders use the same seeds, so each opening gets the same seed either way round
                    var aAsRed = StyledSelfPlay.Run(start, openings, a, b, depth, depth, seedBase, maxPlies);
                    var bAsRed = i == j
                        ? new StyledReport(new List<StyledGame>())
                        : StyledSelfPlay.Run(start, openings, b, a, depth, depth, seedBase, maxPlies);
                    pairings.Add(new StylePairing(a.Name, b.Name, aAsRed, bAsRed));
                }
            }
            return new RoundRobinReport(pairings, depth, openings.Count, seedBase, maxPlies);
        }
    }
}
