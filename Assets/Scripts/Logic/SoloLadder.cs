using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    public static class SoloLadder
    {
        public static int DepthForTier(BotTier tier) => BotTiers.Depth(tier);

        public static BotTier RecommendOpponent(int playerRating)
        {
            var tiers = BotTierRatings.TiersByRating;
            if (tiers == null || tiers.Count == 0)
            {
                return BotTier.Pine;
            }

            BotTier bestTier = tiers[0];
            int bestDistance = Math.Abs(BotTierRatings.RatingFor(bestTier) - playerRating);
            int bestRating = BotTierRatings.RatingFor(bestTier);

            for (int i = 1; i < tiers.Count; i++)
            {
                BotTier candidate = tiers[i];
                int candidateRating = BotTierRatings.RatingFor(candidate);
                int distance = Math.Abs(candidateRating - playerRating);

                if (distance < bestDistance || (distance == bestDistance && candidateRating > bestRating))
                {
                    bestTier = candidate;
                    bestDistance = distance;
                    bestRating = candidateRating;
                }
            }

            return bestTier;
        }

        public static BotTier GetNearestTier(int playerRating)
        {
            return RecommendOpponent(playerRating);
        }
    }
}
