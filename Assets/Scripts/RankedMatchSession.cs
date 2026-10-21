using System;
using KineticRank.Logic;

// Create only for an admitted ranked Solo match. Captures match identity before
// mutable menu settings can change; casual and puzzle matches need no session.
public sealed class RankedMatchSession
{
    public int OpponentRating { get; }
    public bool HumanIsRed { get; }
    public bool IsCompleted { get; private set; }
    public int RatingChange { get; private set; }

    public RankedMatchSession(int opponentRating, bool humanIsRed)
    {
        if (opponentRating < EloCalculator.MinRating)
            throw new ArgumentOutOfRangeException(nameof(opponentRating));
        OpponentRating = opponentRating;
        HumanIsRed = humanIsRed;
    }

    // null means draw; otherwise the value identifies whether Red won.
    // Timeout winners use the same settlement path as board winners.
    public bool TryComplete(bool? redWon)
    {
        if (IsCompleted) return false;
        IsCompleted = true;
        double score = !redWon.HasValue ? 0.5 : redWon.Value == HumanIsRed ? 1.0 : 0.0;
        PlayerRankStore.RecordResult(OpponentRating, score, true);
        RatingChange = PlayerRankStore.GetProfile().lastChange;
        return true;
    }
}
