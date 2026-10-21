using System;
using KineticRank.Logic;

// Captures ranked match identity and settlement independently of mutable menus.
public sealed class RankedMatchSession
{
    public int OpponentRating { get; }
    public bool HumanIsRed { get; }
    public bool IsCompleted { get; private set; }
    public int RatingChange { get; private set; }
    public string ResultLabel { get; private set; }
    public string RankBefore { get; private set; }
    public string RankAfter { get; private set; }
    public int RatingAfter { get; private set; }
    public int ProtectionGamesLeft { get; private set; }
    public string TierBefore { get; private set; }
    public string TierAfter { get; private set; }

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
        var before = PlayerRankStore.GetProfile();
        double score = !redWon.HasValue ? 0.5 : redWon.Value == HumanIsRed ? 1.0 : 0.0;
        PlayerRankStore.RecordResult(OpponentRating, score, true);
        var after = PlayerRankStore.GetProfile();
        RankBefore = before.DisplayRank;
        RankAfter = after.DisplayRank;
        TierBefore = before.currentTier.ToString();
        TierAfter = after.currentTier.ToString();
        RatingAfter = after.rating;
        RatingChange = after.lastChange;
        ProtectionGamesLeft = after.promotionProtectionGamesLeft;
        ResultLabel = score == 1.0 ? "You won" : score == 0.0 ? "You lost" : "Draw";
        IsCompleted = true;
        return true;
    }
}
