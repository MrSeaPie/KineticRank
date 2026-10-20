using System;

namespace KineticRank.Logic
{
    public static class SoloLadder
    {
        public static BotStyle GetTierStyle(BotTier tier)
        {
            switch (tier)
            {
                case BotTier.Pine:
                    return BotStyle.Spender;
                case BotTier.Birch:
                    return BotStyle.Positional;
                case BotTier.Oak:
                    return BotStyle.Default;
                case BotTier.Walnut:
                    return BotStyle.Attacker;
                case BotTier.Ebony:
                    return BotStyle.Hoarder;
                default:
                    return BotStyle.Default;
            }
        }

        public static int GetTierDepth(BotTier tier)
        {
            switch (tier)
            {
                case BotTier.Pine:
                    return 1;
                case BotTier.Birch:
                    return 2;
                case BotTier.Oak:
                    return 3;
                case BotTier.Walnut:
                    return 4;
                case BotTier.Ebony:
                    return 5;
                default:
                    return 2;
            }
        }

        public static int GetTierRating(BotTier tier)
        {
            switch (tier)
            {
                case BotTier.Pine:
                    return 800;
                case BotTier.Birch:
                    return 1000;
                case BotTier.Oak:
                    return 1200;
                case BotTier.Walnut:
                    return 1400;
                case BotTier.Ebony:
                    return 1600;
                default:
                    return 1000;
            }
        }

        public static BotTier GetTierForRating(int rating)
        {
            if (rating < 900) return BotTier.Pine;
            if (rating < 1100) return BotTier.Birch;
            if (rating < 1300) return BotTier.Oak;
            if (rating < 1500) return BotTier.Walnut;
            return BotTier.Ebony;
        }

        public static int CalculateLadderMatchDelta(int playerRating, BotTier tier, bool playerWon, int kFactor = EloCalculator.DefaultKFactor)
        {
            int opponentRating = GetTierRating(tier);
            EloCalculator.CalculateMatch(playerRating, opponentRating, playerWon, out int newPlayer, out _, out int deltaPlayer, out _, kFactor);
            return deltaPlayer;
        }

        public static int CalculateLadderDrawDelta(int playerRating, BotTier tier, int kFactor = EloCalculator.DefaultKFactor)
        {
            int opponentRating = GetTierRating(tier);
            EloCalculator.CalculateDraw(playerRating, opponentRating, out int newPlayer, out _, out int deltaPlayer, out _, kFactor);
            return deltaPlayer;
        }
    }
}
