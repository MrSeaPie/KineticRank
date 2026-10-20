using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Ratings for the Solo bot tiers, on the same scale as player ratings. SoloLadder uses them to pick the nearest opponent.
    // The values come from the calibration in Docs/RANKED_RATINGS.md:
    // adjacent tiers play each other with alternating starts, and each measured score becomes a capped Elo gap above Pine.
    public static class BotTierRatings
    {
        public const int Anchor = 800;   // Pine
        public const int MaxGap = 400;   // cap on each adjacent-tier gap

        // Indexed by BotTier, weakest first. Pine-Birch (196) and Birch-Oak (259) are measured;
        // Oak-Walnut and Walnut-Ebony still use the brief's +200 guess until their calibration runs land.
        static readonly int[] Ratings = { 800, 996, 1255, 1455, 1655 };

        // All tiers by rating, lowest first. Equal ratings keep the weaker tier first.
        public static IReadOnlyList<BotTier> TiersByRating { get; } = SortByRating();

        public static int RatingFor(BotTier tier)
        {
            int i = (int)tier;
            if (i < 0 || i >= Ratings.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return Ratings[i];
        }

        // Elo gap for the stronger tier's score against the weaker one (draws count half): 400 * log10(s / (1 - s)).
        // Capped at MaxGap; a score of 0.5 or less gives 0 so the ladder never runs backwards.
        public static int ScoreToGap(double score)
        {
            if (double.IsNaN(score) || score < 0.0 || score > 1.0) throw new ArgumentOutOfRangeException(nameof(score), "score must be between 0 and 1");
            if (score <= 0.5) return 0;
            if (score >= 1.0) return MaxGap;
            double gap = 400.0 * Math.Log10(score / (1.0 - score));
            return (int)Math.Min(MaxGap, Math.Round(gap, MidpointRounding.AwayFromZero));
        }

        // Ratings for every tier from the adjacent gaps (Pine-Birch first), starting at Anchor
        public static int[] RatingsFromGaps(IReadOnlyList<int> gaps)
        {
            if (gaps == null) throw new ArgumentNullException(nameof(gaps));
            if (gaps.Count != Ratings.Length - 1) throw new ArgumentException($"Expected {Ratings.Length - 1} gaps.", nameof(gaps));

            var result = new int[Ratings.Length];
            result[0] = Anchor;
            for (int i = 0; i < gaps.Count; i++)
            {
                if (gaps[i] < 0 || gaps[i] > MaxGap) throw new ArgumentOutOfRangeException(nameof(gaps), $"Gap {i} must be between 0 and {MaxGap}.");
                result[i + 1] = result[i] + gaps[i];
            }
            return result;
        }

        static IReadOnlyList<BotTier> SortByRating()
        {
            var order = new List<BotTier>((BotTier[])Enum.GetValues(typeof(BotTier)));
            order.Sort((a, b) =>
            {
                int byRating = Ratings[(int)a].CompareTo(Ratings[(int)b]);
                return byRating != 0 ? byRating : ((int)a).CompareTo((int)b);
            });
            return order.AsReadOnly();
        }
    }
}
