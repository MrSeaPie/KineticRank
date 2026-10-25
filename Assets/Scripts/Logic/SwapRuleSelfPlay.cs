using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // One swap-rule game, reported from the original players' point of view.
    // The first player makes ply 1 as Red; if the swap fired, the second player took over Red from there.
    public sealed class SwapGame
    {
        public SwapGame(StyledGame game, bool swapped, int swapScore)
        {
            Game = game;
            Swapped = swapped;
            SwapScore = swapScore;
        }

        public StyledGame Game { get; }
        public bool Swapped { get; }
        // Score of the position after ply 1 for the side to move (the original second player)
        public int SwapScore { get; }
        public string FirstStyle => Swapped ? Game.BlueStyle : Game.RedStyle;
        public string SecondStyle => Swapped ? Game.RedStyle : Game.BlueStyle;
        public bool FirstPlayerWon => Game.Result == (Swapped ? EvaluationResult.BlueWin : EvaluationResult.RedWin);
        public bool SecondPlayerWon => Game.Result == (Swapped ? EvaluationResult.RedWin : EvaluationResult.BlueWin);
    }

    public sealed class SwapReport
    {
        public SwapReport(IReadOnlyList<SwapGame> games)
        {
            Games = games;
            foreach (var game in games)
            {
                if (game.Swapped) Swaps++;
                if (game.FirstPlayerWon) FirstPlayerWins++;
                else if (game.SecondPlayerWon) SecondPlayerWins++;
                else Draws++;
                if (game.Game.Result == EvaluationResult.RedWin) RedWins++;
                else if (game.Game.Result == EvaluationResult.BlueWin) BlueWins++;
            }
        }

        public IReadOnlyList<SwapGame> Games { get; }
        public int Swaps { get; private set; }
        public int FirstPlayerWins { get; private set; }
        public int SecondPlayerWins { get; private set; }
        // Decided draws and capped games together, as in StyledReport
        public int Draws { get; private set; }
        public int RedWins { get; private set; }
        public int BlueWins { get; private set; }

        public double FirstPlayerShareOfDecisive
        {
            get
            {
                int decisive = FirstPlayerWins + SecondPlayerWins;
                return decisive == 0 ? 0.0 : (double)FirstPlayerWins / decisive;
            }
        }
    }

    // Fairness tester only: a deterministic swap rule after ply 1.
    // The second player scores the position with StyledBot.Score at the game depth and takes over Red when it scores
    // below 0 (ties keep). The rest of the opening is replayed by StyledSelfPlay.PlayGame; GameState is unchanged.
    public static class SwapRuleSelfPlay
    {
        public static SwapGame PlayGame(IReadOnlyList<Move> opening, BotStyle firstStyle, BotStyle secondStyle, int depth, int? seed, int maxPlies)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (firstStyle == null) throw new ArgumentNullException(nameof(firstStyle));
            if (secondStyle == null) throw new ArgumentNullException(nameof(secondStyle));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "depth must be at least 1");
            if (opening.Count < 1) throw new ArgumentException("The opening needs at least one move.", nameof(opening));

            var afterFirst = GameState.Initial();
            if (!afterFirst.IsLegal(opening[0])) throw new ArgumentException($"Illegal first move {opening[0]}.", nameof(opening));
            afterFirst = afterFirst.Apply(opening[0]);
            if (afterFirst.IsOver) throw new ArgumentException("The game ends on the first move.", nameof(opening));

            int score = StyledBot.Score(afterFirst, depth, secondStyle);
            bool swap = score < 0;
            var game = swap
                ? StyledSelfPlay.PlayGame(opening, secondStyle, firstStyle, depth, depth, seed, maxPlies)
                : StyledSelfPlay.PlayGame(opening, firstStyle, secondStyle, depth, depth, seed, maxPlies);
            return new SwapGame(game, swap, score);
        }

        // Game i uses seed seedBase + i, or no seed when seedBase is null.
        public static SwapReport Run(IEnumerable<IReadOnlyList<Move>> openings, BotStyle firstStyle, BotStyle secondStyle, int depth, int? seedBase, int maxPlies)
        {
            if (openings == null) throw new ArgumentNullException(nameof(openings));

            var games = new List<SwapGame>();
            int i = 0;
            foreach (var opening in openings)
            {
                games.Add(PlayGame(opening, firstStyle, secondStyle, depth, seedBase + i, maxPlies));
                i++;
            }
            return new SwapReport(games);
        }
    }
}
