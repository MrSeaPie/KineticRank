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

        public static int Depth(BotTier tier) => Depths[Index(tier)];

        // Plain-word difficulty shown next to the wood name, e.g. "Medium" for Oak
        public static string Difficulty(BotTier tier) => Difficulties[Index(tier)];

        // "Oak (Medium)"
        public static string Title(BotTier tier) => $"{tier} ({Difficulty(tier)})";

        // "Looks 4 moves ahead"; a move is one player's turn
        public static string LookAhead(BotTier tier)
        {
            int depth = Depth(tier);
            return depth == 1 ? "Looks 1 move ahead" : $"Looks {depth} moves ahead";
        }

        static int Index(BotTier tier)
        {
            int i = (int)tier;
            if (i < 0 || i >= Depths.Length) throw new ArgumentOutOfRangeException(nameof(tier));
            return i;
        }
    }
}
