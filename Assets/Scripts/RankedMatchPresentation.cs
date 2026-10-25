using System;
using KineticRank.Logic;

// Pure text formatting shared by ranked match presentation surfaces.
public static class RankedMatchPresentation
{
    public static string Format(RankedMatchSession session, PlayerRankProfile profile)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        string rank = session.IsCompleted ? session.RankAfter : profile.DisplayRank;
        int rating = session.IsCompleted ? session.RatingAfter : profile.rating;
        string text = "Ranked Solo | " + rank + " | Rating " + rating
            + "\nOpponent rating " + session.OpponentRating;
        if (session.IsCompleted)
        {
            int change = session.RatingChange;
            text += " | Rating change " + (change > 0 ? "+" : "") + change;
            text += "\n" + session.ResultLabel + " | " + session.RankBefore + " → " + session.RankAfter;
            if (!string.IsNullOrEmpty(session.RankTransitionLabel))
                text += "\n" + session.RankTransitionLabel + ": " + session.RankAfter;
            if (session.ProtectionGamesLeft > 0)
                text += "\nProtected: " + session.ProtectionGamesLeft + " games left";
        }
        return text;
    }
}
