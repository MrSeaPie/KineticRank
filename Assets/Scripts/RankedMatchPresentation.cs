using System;
using KineticRank.Logic;

// Pure text formatting shared by ranked match presentation surfaces.
public static class RankedMatchPresentation
{
    public static string Format(RankedMatchSession session, PlayerRankProfile profile)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        string text = "Ranked Solo | " + profile.DisplayRank + " | Rating " + profile.rating
            + "\nOpponent rating " + session.OpponentRating;
        if (session.IsCompleted)
        {
            int change = session.RatingChange;
            text += " | Rating change " + (change > 0 ? "+" : "") + change;
        }
        return text;
    }
}
