using System;

namespace KineticRank.Logic
{
    public static class SoloLadder
    {
        public static BotStyle GetTierStyle(BotTier tier)
        {
            switch (tier)
            {
                case BotTier.Pine: return BotStyle.Spender;
                case BotTier.Birch: return BotStyle.Positional;
                case BotTier.Oak: return BotStyle.Default;
                case BotTier.Walnut: return BotStyle.Attacker;
                case BotTier.Ebony: return BotStyle.Hoarder;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public static int GetTierDepth(BotTier tier) => BotTiers.Depth(tier);

        public static int GetTierRating(BotTier tier) => BotTiers.Rating(tier);

        public static BotTier GetTierForRating(int rating) => BotTiers.RecommendedTier(rating);

        public static int CalculateLadderMatchDelta(int playerRating, BotTier tier, bool playerWon, int kFactor = 32)
        {
            double actualScore = playerWon ? 1.0 : 0.0;
            return BotTiers.RatingDelta(playerRating, tier, actualScore, kFactor);
        }

        public static int CalculateLadderDrawDelta(int playerRating, BotTier tier, int kFactor = 32)
        {
            return BotTiers.RatingDelta(playerRating, tier, 0.5, kFactor);
        }
    }
}
