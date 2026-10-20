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
        static readonly string[] Blurbs =
        {
            "Still learning the game",
            "Blocks your obvious threats",
            "Thinks a few moves ahead",
            "Sets traps and spots yours",
            "Very hard to beat"
        };

        public static int SearchDepth(BotTier tier) => Depths[Index(tier)];
        public static int Depth(BotTier tier) => SearchDepth(tier);
        public static string Blurb(BotTier tier) => Blurbs[Index(tier)];

        static int Index(BotTier tier)
        {
            int i = (int)tier;
            if (i < 0 || i >= Depths.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return i;
        }
    }
}
