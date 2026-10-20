using System;

namespace KineticRank.Logic
{
    public sealed class PuzzleDef
    {
        public string Id { get; }
        public string Title { get; }
        public string Hint { get; }
        public GameState Start { get; }
        public int MovesN { get; }

        public PuzzleDef(string id, string title, string hint, GameState start, int movesN)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Puzzle Id must not be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Puzzle Title must not be null or empty.", nameof(title));
            if (start == null)
                throw new ArgumentNullException(nameof(start));
            if (movesN < 1 || movesN > PuzzleVerifier.MaxSupportedMoves)
                throw new ArgumentOutOfRangeException(nameof(movesN), $"movesN must be between 1 and {PuzzleVerifier.MaxSupportedMoves}.");

            Id = id;
            Title = title;
            Hint = hint ?? string.Empty;
            Start = start;
            MovesN = movesN;
        }
    }
}
