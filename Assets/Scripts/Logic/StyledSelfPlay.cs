using System;
using System.Collections.Generic;
using System.Text;

namespace KineticRank.Logic
{
    // One fairness-tester game between two BotStyle profiles. Plies include the opening moves.
    public sealed class StyledGame
    {
        public readonly IReadOnlyList<Move> Opening;
        public readonly string RedStyle;
        public readonly string BlueStyle;
        public readonly int? Seed;
        public readonly EvaluationResult Result;
        public readonly int Plies;
        public readonly bool Capped;
        public readonly int MaxRepeats;   // most times any one position occurred (cells, side to move, power-ups)

        public StyledGame(IReadOnlyList<Move> opening, string redStyle, string blueStyle, int? seed, EvaluationResult result, int plies, bool capped, int maxRepeats)
        {
            Opening = opening;
            RedStyle = redStyle;
            BlueStyle = blueStyle;
            Seed = seed;
            Result = result;
            Plies = plies;
            Capped = capped;
            MaxRepeats = maxRepeats;
        }

        // Capped games count as draws; DecidedDraw is a draw the rules declared before the cap
        public bool IsDraw => Result != EvaluationResult.RedWin && Result != EvaluationResult.BlueWin;
        public bool DecidedDraw => IsDraw && !Capped;
        public bool Repeated => MaxRepeats >= 3;
    }

    public sealed class StyledReport
    {
        public readonly IReadOnlyList<StyledGame> Games;
        public readonly int RedWins;
        public readonly int BlueWins;
        public readonly int Draws;
        public readonly int CappedGames;
        public readonly int DecidedDraws;
        public readonly int RepetitionGames;
        public readonly double AveragePlies;

        public StyledReport(IReadOnlyList<StyledGame> games)
        {
            Games = games ?? throw new ArgumentNullException(nameof(games));
            long plies = 0;
            foreach (var game in games)
            {
                if (game.Result == EvaluationResult.RedWin) RedWins++;
                else if (game.Result == EvaluationResult.BlueWin) BlueWins++;
                else Draws++;
                if (game.Capped) CappedGames++;
                if (game.DecidedDraw) DecidedDraws++;
                if (game.Repeated) RepetitionGames++;
                plies += game.Plies;
            }
            AveragePlies = games.Count == 0 ? 0 : (double)plies / games.Count;
        }
    }

    // Fairness tester round 2: style-vs-style self-play with optional seeded tie-breaks.
    // SelfPlay stays unchanged; with no seed every game here is fully deterministic.
    public static class StyledSelfPlay
    {
        // Traditional mode (Phase 1 brief): empty board, Red to move, no Queen or Cleaner for either side
        public static GameState TraditionalStart() =>
            GameState.FromGrid(new int[GameState.Columns, GameState.Rows], true, false, false, false, false);

        // Every two-ply opening over Normal/Queen/Cleaner that leaves the game in progress
        public static List<IReadOnlyList<Move>> TwoPlyOpenings() => TwoPlyOpenings(GameState.Initial());

        // Same, from a given start; a start with no power-ups yields Normal-only openings
        public static List<IReadOnlyList<Move>> TwoPlyOpenings(GameState start)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            var openings = new List<IReadOnlyList<Move>>();
            foreach (var first in start.LegalMoves())
            {
                var afterFirst = start.Apply(first);
                if (afterFirst.IsOver) continue;
                foreach (var second in afterFirst.LegalMoves())
                {
                    if (afterFirst.Apply(second).IsOver) continue;
                    openings.Add(new[] { first, second });
                }
            }
            return openings;
        }

        // Every three-ply opening of Normal drops that leaves the game in progress
        public static List<IReadOnlyList<Move>> ThreePlyNormalOpenings()
        {
            var openings = new List<IReadOnlyList<Move>>();
            var start = GameState.Initial();
            for (int a = 0; a < GameState.Columns; a++)
            {
                var first = new Move(a, MoveKind.Normal);
                var afterFirst = start.Apply(first);
                if (afterFirst.IsOver) continue;
                for (int b = 0; b < GameState.Columns; b++)
                {
                    var second = new Move(b, MoveKind.Normal);
                    var afterSecond = afterFirst.Apply(second);
                    if (afterSecond.IsOver) continue;
                    for (int c = 0; c < GameState.Columns; c++)
                    {
                        var third = new Move(c, MoveKind.Normal);
                        if (afterSecond.Apply(third).IsOver) continue;
                        openings.Add(new[] { first, second, third });
                    }
                }
            }
            return openings;
        }

        // seed null keeps both bots on their earliest-best-move rule; otherwise one Random breaks ties for both sides
        public static StyledGame PlayGame(IReadOnlyList<Move> opening, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seed, int maxPlies)
            => PlayGame(GameState.Initial(), opening, redStyle, blueStyle, redDepth, blueDepth, seed, maxPlies);

        // Same, from a given start position (e.g. TraditionalStart); Plies counts from that start
        public static StyledGame PlayGame(GameState start, IReadOnlyList<Move> opening, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seed, int maxPlies)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (redStyle == null) throw new ArgumentNullException(nameof(redStyle));
            if (blueStyle == null) throw new ArgumentNullException(nameof(blueStyle));
            if (maxPlies < opening.Count) throw new ArgumentOutOfRangeException(nameof(maxPlies), "maxPlies must cover the opening");

            var tieBreak = seed.HasValue ? new Random(seed.Value) : null;
            var seen = new Dictionary<string, int>();
            var state = start;
            int maxRepeats = Visit(seen, state);
            int plies = 0;

            foreach (var move in opening)
            {
                if (state.IsOver || !state.IsLegal(move))
                    throw new ArgumentException("Opening contains an illegal move.", nameof(opening));
                state = state.Apply(move);
                plies++;
                maxRepeats = Math.Max(maxRepeats, Visit(seen, state));
            }

            while (!state.IsOver && plies < maxPlies)
            {
                bool red = state.RedToMove;
                var move = StyledBot.ChooseMove(state, red ? redDepth : blueDepth, red ? redStyle : blueStyle, tieBreak);
                state = state.Apply(move);
                plies++;
                maxRepeats = Math.Max(maxRepeats, Visit(seen, state));
            }

            return new StyledGame(opening, redStyle.Name, blueStyle.Name, seed, state.Result, plies, !state.IsOver, maxRepeats);
        }

        // seedBase null plays every game unseeded; otherwise game i uses seedBase + i
        public static StyledReport Run(IEnumerable<IReadOnlyList<Move>> openings, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seedBase, int maxPlies)
            => Run(GameState.Initial(), openings, redStyle, blueStyle, redDepth, blueDepth, seedBase, maxPlies);

        // Same, with every game starting from the given position
        public static StyledReport Run(GameState start, IEnumerable<IReadOnlyList<Move>> openings, BotStyle redStyle, BotStyle blueStyle, int redDepth, int blueDepth, int? seedBase, int maxPlies)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            var games = new List<StyledGame>();
            int i = 0;
            foreach (var opening in openings)
            {
                int? seed = seedBase.HasValue ? unchecked(seedBase.Value + i) : (int?)null;
                games.Add(PlayGame(start, opening, redStyle, blueStyle, redDepth, blueDepth, seed, maxPlies));
                i++;
            }
            return new StyledReport(games);
        }

        static int Visit(Dictionary<string, int> seen, GameState state)
        {
            string key = Key(state);
            seen.TryGetValue(key, out int count);
            seen[key] = ++count;
            return count;
        }

        static string Key(GameState state)
        {
            var key = new StringBuilder(GameState.Columns * GameState.Rows + 5);
            for (int c = 0; c < GameState.Columns; c++)
            {
                for (int r = 0; r < GameState.Rows; r++)
                {
                    key.Append((char)('0' + state.Cell(c, r)));
                }
            }
            key.Append(state.RedToMove ? 'R' : 'B');
            key.Append(state.QueenAvailable(true) ? 'q' : '-');
            key.Append(state.CleanerAvailable(true) ? 'c' : '-');
            key.Append(state.QueenAvailable(false) ? 'q' : '-');
            key.Append(state.CleanerAvailable(false) ? 'c' : '-');
            return key.ToString();
        }
    }
}
