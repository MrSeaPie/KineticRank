using System;

namespace KineticRank.Logic
{
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
        public static readonly string[] Difficulties = 
        {
            "Beginner",
            "Easy",
            "Medium",
            "Hard",
            "Expert"
        };

        static readonly string[] Blurbs = 
        {
            "Still learning the game",
            "Plays mostly by instinct",
            "Thinks a few moves ahead",
            "Plans attacks and counters",
            "Very hard to beat"
        };

        static readonly int[] Depths = { 1, 2, 4, 6, 8 };

        public static string Difficulty(BotTier tier)
        {
            int idx = (int)tier;
            if (idx < 0 || idx >= Difficulties.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            return Difficulties[idx];
        }

        public static string Blurb(BotTier tier)
        {
            int idx = (int)tier;
            if (idx < 0 || idx >= Blurbs.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            return Blurbs[idx];
        }

        public static string Title(BotTier tier) => $"{tier} ({Difficulty(tier)})";

        public static int Depth(BotTier tier)
        {
            int idx = (int)tier;
            if (idx < 0 || idx >= Depths.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            return Depths[idx];
        }
    }
}