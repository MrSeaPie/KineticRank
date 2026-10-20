using System;

namespace KineticRank.Logic
{
    // Solo opponent tiers, weakest first. Depths are starting values; WebGL timing may lower the top tiers.
    public enum BotTier
    {
        Pine = 0,
        Birch = 1,
        Oak = 2,
        Walnut = 3,
        Ebony = 4
    }

    public static class BotTiers
    {
        static readonly int[] Depths = { 1, 2, 4, 6, 8 };
        static readonly string[] Difficulties = { "Beginner", "Easy", "Medium", "Hard", "Expert" };
        static readonly int[] Ratings = { 800, 1000, 1200, 1400, 1600 };
        // How each tier plays, in player terms (search depths 1 / 2 / 4 / 6 / 8 behind the scenes)
        static readonly string[] Blurbs =
        {
            "Still learning the game",
            "Blocks your obvious threats",
            "Thinks a few moves ahead",
            "Sets traps and spots yours",
            "Very hard to beat"
        };

        public static int Count => Depths.Length;

        public static int Depth(BotTier tier) => Depths[Index(tier)];

        // Plain-word difficulty shown next to the wood name, e.g. "Medium" for Oak
        public static string Difficulty(BotTier tier) => Difficulties[Index(tier)];

        // Nominal ladder rating for this bot tier
        public static int Rating(BotTier tier) => Ratings[Index(tier)];

        // "Oak (Medium)"
        public static string Title(BotTier tier) => $"{tier} ({Difficulty(tier)})";

        // One line on how the tier plays, e.g. "Thinks a few moves ahead" for Oak
        public static string Blurb(BotTier tier) => Blurbs[Index(tier)];

        // Calculate expected Elo score for player rating vs tier rating
        public static double ExpectedScore(int playerRating, BotTier tier)
        {
            double diff = (Rating(tier) - playerRating) / 400.0;
            return 1.0 / (1.0 + Math.Pow(10.0, diff));
        }

        // Calculate Elo change for player against bot tier
        public static int RatingDelta(int playerRating, BotTier tier, double actualScore, int kFactor = 32)
        {
            double expected = ExpectedScore(playerRating, tier);
            return (int)Math.Round(kFactor * (actualScore - expected));
        }

        // Pick recommended tier based on current player Elo
        public static BotTier RecommendedTier(int playerRating)
        {
            if (playerRating < 900) return BotTier.Pine;
            if (playerRating < 1100) return BotTier.Birch;
            if (playerRating < 1300) return BotTier.Oak;
            if (playerRating < 1500) return BotTier.Walnut;
            return BotTier.Ebony;
        }

        static int Index(BotTier tier)
        {
            int i = (int)tier;
            if (i < 0 || i >= Depths.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return i;
        }
    }
}
