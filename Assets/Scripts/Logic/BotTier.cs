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

        public static int Depth(BotTier tier)
        {
            int i = (int)tier;
            if (i < 0 || i >= Depths.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return Depths[i];
        }
    }
}
