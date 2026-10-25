using System;

namespace KineticRank.Logic
{
    public enum PuzzleStatus
    {
        InProgress = 0,
        Solved = 1,
        Failed = 2
    }

    // Unity-free state machine for one puzzle attempt: judges attacker moves with PuzzleLine and plays the defender's best reply.
    // The side to move in the start position is the attacker; MovesN is the attacker's move budget.
    public sealed class PuzzleSession
    {
        public GameState Start { get; }
        public int MovesN { get; }
        public bool AttackerIsRed { get; }

        public GameState Current { get; private set; }
        public int MovesLeft { get; private set; }
        public PuzzleStatus Status { get; private set; }
        public Move? LastDefenderReply { get; private set; }

        public PuzzleSession(GameState start, int movesN)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (start.IsOver) throw new ArgumentException("Start position must be in progress.", nameof(start));
            if (movesN < 1) throw new ArgumentOutOfRangeException(nameof(movesN), "Puzzle needs at least one attacker move.");

            Start = start;
            MovesN = movesN;
            AttackerIsRed = start.RedToMove;
            Restart();
        }

        public void Restart()
        {
            Current = Start;
            MovesLeft = MovesN;
            Status = PuzzleStatus.InProgress;
            LastDefenderReply = null;
        }

        // Illegal moves throw so a misclick the UI should have filtered never counts as a failed attempt.
        // A wrong move leaves Current unchanged and sets Failed; a correct move either wins or is answered by the defender.
        public PuzzleStatus SubmitAttackerMove(Move move)
        {
            if (Status != PuzzleStatus.InProgress) throw new InvalidOperationException("Puzzle is not in progress.");
            if (!Current.IsLegal(move)) throw new ArgumentException($"Illegal move {move}.", nameof(move));

            LastDefenderReply = null;
            if (!PuzzleLine.IsCorrectAttackerMove(Current, move, MovesLeft))
            {
                Status = PuzzleStatus.Failed;
                return Status;
            }

            Current = Current.Apply(move);
            MovesLeft--;
            if (Current.IsOver)
            {
                Status = PuzzleStatus.Solved;
                return Status;
            }

            var reply = PuzzleLine.DefenderReply(Current, AttackerIsRed, MovesLeft);
            Current = Current.Apply(reply);
            LastDefenderReply = reply;
            return Status;
        }
    }
}
