using System;
using System.Collections.Generic;
using System.Globalization;

namespace KineticRank.Logic
{
    // One finished self-play game: the forced opening, the outcome and its length in plies.
    // A capped game never reached a result and counts as a draw.
    public readonly struct SelfPlayGame
    {
        public readonly Move[] Opening;
        public readonly EvaluationResult Result;
        public readonly int Plies;
        public readonly bool Capped;

        public SelfPlayGame(Move[] opening, EvaluationResult result, int plies, bool capped)
        {
            Opening = opening;
            Result = result;
            Plies = plies;
            Capped = capped;
        }

        public bool RedWon => Result == EvaluationResult.RedWin;
        public bool BlueWon => Result == EvaluationResult.BlueWin;
        public bool Drawn => !RedWon && !BlueWon;

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "{0} -> {1} in {2} plies{3}", string.Join(" ", Opening), Result, Plies, Capped ? " (capped)" : "");
    }

    // Totals for one depth pairing. Red always moves first, so RedWins are first-player wins.
    public sealed class SelfPlayReport
    {
        private readonly List<SelfPlayGame> games = new List<SelfPlayGame>();

        public int RedDepth { get; }
        public int BlueDepth { get; }
        public int MaxPlies { get; }
        public IReadOnlyList<SelfPlayGame> Games => games;
        public int GameCount => games.Count;
        public int RedWins { get; private set; }
        public int BlueWins { get; private set; }
        public int Draws { get; private set; }
        public int CappedGames { get; private set; }
        public int TotalPlies { get; private set; }

        public double AveragePlies => GameCount == 0 ? 0.0 : (double)TotalPlies / GameCount;
        public double RedWinRate => Rate(RedWins);
        public double BlueWinRate => Rate(BlueWins);
        public double DrawRate => Rate(Draws);

        public SelfPlayReport(int redDepth, int blueDepth, int maxPlies)
        {
            RedDepth = redDepth;
            BlueDepth = blueDepth;
            MaxPlies = maxPlies;
        }

        internal void Add(SelfPlayGame game)
        {
            games.Add(game);
            if (game.RedWon) RedWins++;
            else if (game.BlueWon) BlueWins++;
            else Draws++;
            if (game.Capped) CappedGames++;
            TotalPlies += game.Plies;
        }

        double Rate(int count) => GameCount == 0 ? 0.0 : (double)count / GameCount;

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "Red depth {0} vs Blue depth {1}, cap {2}: {3} games, first-player (Red) wins {4} ({5:P1}), second-player (Blue) wins {6} ({7:P1}), draws {8} ({9:P1}, {10} capped), average length {11:F1} plies",
            RedDepth, BlueDepth, MaxPlies, GameCount, RedWins, RedWinRate, BlueWins, BlueWinRate, Draws, DrawRate, CappedGames, AveragePlies);
    }

    // Deterministic bot-vs-bot harness for measuring first-player advantage. No randomness:
    // variety comes only from the forced openings, so every run gives identical results.
    public static class SelfPlay
    {
        // All 49 openings where Red and then Blue drop a Normal token in any column
        public static List<Move[]> TwoPlyOpenings()
        {
            var openings = new List<Move[]>();
            for (int first = 0; first < GameState.Columns; first++)
            {
                for (int second = 0; second < GameState.Columns; second++)
                {
                    openings.Add(new[] { new Move(first, MoveKind.Normal), new Move(second, MoveKind.Normal) });
                }
            }
            return openings;
        }

        public static SelfPlayGame PlayGame(Move[] opening, int redDepth, int blueDepth, int maxPlies)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            ValidateDepths(redDepth, blueDepth);
            if (maxPlies < opening.Length) throw new ArgumentOutOfRangeException(nameof(maxPlies), "maxPlies must cover the opening");

            var state = GameState.Initial();
            int plies = 0;
            foreach (var move in opening)
            {
                if (!state.IsLegal(move)) throw new ArgumentException($"Illegal opening move {move} at ply {plies}.", nameof(opening));
                state = state.Apply(move);
                plies++;
            }

            while (!state.IsOver && plies < maxPlies)
            {
                state = state.Apply(Bot.ChooseMove(state, state.RedToMove ? redDepth : blueDepth));
                plies++;
            }

            return new SelfPlayGame((Move[])opening.Clone(), state.Result, plies, !state.IsOver);
        }

        public static SelfPlayReport Run(IEnumerable<Move[]> openings, int redDepth, int blueDepth, int maxPlies)
        {
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            ValidateDepths(redDepth, blueDepth);

            var report = new SelfPlayReport(redDepth, blueDepth, maxPlies);
            foreach (var opening in openings)
            {
                report.Add(PlayGame(opening, redDepth, blueDepth, maxPlies));
            }
            return report;
        }

        static void ValidateDepths(int redDepth, int blueDepth)
        {
            if (redDepth < 1) throw new ArgumentOutOfRangeException(nameof(redDepth), "depth must be at least 1");
            if (blueDepth < 1) throw new ArgumentOutOfRangeException(nameof(blueDepth), "depth must be at least 1");
        }
    }
}
