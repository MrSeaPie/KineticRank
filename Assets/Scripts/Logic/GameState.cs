using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Token kinds a player can drop; values match BoardManager's effect codes (1 = Queen, 2 = Cleaner)
    public enum MoveKind
    {
        Normal = 0,
        Queen = 1,
        Cleaner = 2
    }

    public readonly struct Move : IEquatable<Move>
    {
        public readonly int Column;
        public readonly MoveKind Kind;

        public Move(int column, MoveKind kind)
        {
            Column = column;
            Kind = kind;
        }

        public bool Equals(Move other) => Column == other.Column && Kind == other.Kind;
        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => Column * 3 + (int)Kind;
        public override string ToString() => $"{Kind}@{Column}";
    }

    // Immutable, Unity-free model of BoardManager's rules so bots and tests can play without scenes.
    // Cells use BoardEvaluator's layout: grid[column, row], row 0 at the bottom, 0 empty, 1 Red, 2 Blue.
    public sealed class GameState
    {
        public static int Columns => BoardEvaluator.Columns;
        public static int Rows => BoardEvaluator.Rows;

        private readonly int[,] cells;
        private readonly bool redQueen;
        private readonly bool redCleaner;
        private readonly bool blueQueen;
        private readonly bool blueCleaner;

        public bool RedToMove { get; }
        public EvaluationResult Result { get; }
        public bool IsOver => Result != EvaluationResult.InProgress;

        private GameState(int[,] cells, bool redToMove, bool redQueen, bool redCleaner, bool blueQueen, bool blueCleaner)
        {
            this.cells = cells;
            RedToMove = redToMove;
            this.redQueen = redQueen;
            this.redCleaner = redCleaner;
            this.blueQueen = blueQueen;
            this.blueCleaner = blueCleaner;
            Result = BoardEvaluator.Evaluate(cells);
        }

        // Red moves first and each player starts with one Queen and one Cleaner, as in PlayerController
        public static GameState Initial() => new GameState(new int[Columns, Rows], true, true, true, true, true);

        public static GameState FromGrid(int[,] grid, bool redToMove, bool redQueen = true, bool redCleaner = true, bool blueQueen = true, bool blueCleaner = true)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.GetLength(0) != Columns || grid.GetLength(1) != Rows)
                throw new ArgumentException("Grid must be Columns x Rows.", nameof(grid));
            return new GameState((int[,])grid.Clone(), redToMove, redQueen, redCleaner, blueQueen, blueCleaner);
        }

        public int Cell(int column, int row) => cells[column, row];
        public int[,] ToGrid() => (int[,])cells.Clone();
        public bool QueenAvailable(bool red) => red ? redQueen : blueQueen;
        public bool CleanerAvailable(bool red) => red ? redCleaner : blueCleaner;

        public bool IsLegal(Move move)
        {
            if (IsOver || move.Column < 0 || move.Column >= Columns) return false;
            switch (move.Kind)
            {
                case MoveKind.Normal: return true;
                case MoveKind.Queen: return QueenAvailable(RedToMove);
                case MoveKind.Cleaner: return CleanerAvailable(RedToMove);
                default: return false;
            }
        }

        // Every column is always playable because a full column pops its bottom token
        public List<Move> LegalMoves()
        {
            var moves = new List<Move>();
            if (IsOver) return moves;
            for (int c = 0; c < Columns; c++)
            {
                moves.Add(new Move(c, MoveKind.Normal));
                if (QueenAvailable(RedToMove)) moves.Add(new Move(c, MoveKind.Queen));
                if (CleanerAvailable(RedToMove)) moves.Add(new Move(c, MoveKind.Cleaner));
            }
            return moves;
        }

        public GameState Apply(Move move)
        {
            if (IsOver) throw new InvalidOperationException("The match is already over.");
            if (!IsLegal(move)) throw new ArgumentException($"Illegal move {move}.", nameof(move));

            var next = (int[,])cells.Clone();
            int mover = RedToMove ? (int)CellOwner.Red : (int)CellOwner.Blue;
            int column = move.Column;

            // BoardManager.GetLandingRow: a full column drops its bottom token and shifts down one row
            if (next[column, Rows - 1] != 0)
            {
                for (int r = 1; r < Rows; r++) next[column, r - 1] = next[column, r];
                next[column, Rows - 1] = 0;
            }
            int landing = 0;
            while (landing < Rows - 1 && next[column, landing] != 0) landing++;
            next[column, landing] = mover;

            // BoardManager.ResolveMove: a Queen flips the column, then the landing cell becomes the mover's token;
            // a Cleaner empties the whole column, including itself
            if (move.Kind == MoveKind.Queen)
            {
                for (int r = 0; r < Rows; r++)
                {
                    if (next[column, r] != 0) next[column, r] = Opponent(next[column, r]);
                }
                next[column, landing] = mover;
            }
            else if (move.Kind == MoveKind.Cleaner)
            {
                for (int r = 0; r < Rows; r++) next[column, r] = 0;
            }

            bool usedQueen = move.Kind == MoveKind.Queen;
            bool usedCleaner = move.Kind == MoveKind.Cleaner;
            return new GameState(next, !RedToMove,
                redQueen && !(RedToMove && usedQueen),
                redCleaner && !(RedToMove && usedCleaner),
                blueQueen && !(!RedToMove && usedQueen),
                blueCleaner && !(!RedToMove && usedCleaner));
        }

        private static int Opponent(int owner) => owner == (int)CellOwner.Red ? (int)CellOwner.Blue : (int)CellOwner.Red;
    }
}
