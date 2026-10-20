using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    public static class SoloLadder
    {
        public const int DefaultKFactor = 32;

        public static int DepthForTier(BotTier tier)
        {
            switch (tier)
            {
                case BotTier.Pine: return 1;
                case BotTier.Birch: return 2;
                case BotTier.Oak: return 4;
                case BotTier.Walnut: return 6;
                case BotTier.Ebony: return 8;
                default: return 1;
            }
        }

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

        public static double ExpectedScore(int playerRating, int opponentRating)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - playerRating) / 400.0));
        }

        public static int CalculateDelta(int playerRating, int opponentRating, double actualScore, int kFactor = DefaultKFactor)
        {
            double expected = ExpectedScore(playerRating, opponentRating);
            return (int)Math.Round(kFactor * (actualScore - expected));
        }

        public static int CalculateDelta(int playerRating, BotTier opponentTier, double actualScore, int kFactor = DefaultKFactor)
        {
            int opponentRating = BotTierRatings.RatingFor(opponentTier);
            return CalculateDelta(playerRating, opponentRating, actualScore, kFactor);
        }

        public static int CalculateDelta(int playerRating, BotTier opponentTier, bool won, bool isDraw = false, int kFactor = DefaultKFactor)
        {
            double actualScore = isDraw ? 0.5 : (won ? 1.0 : 0.0);
            return CalculateDelta(playerRating, opponentTier, actualScore, kFactor);
        }
    }
}
