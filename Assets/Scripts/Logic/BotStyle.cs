using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Playing-style weights layered on top of Bot.Evaluate for the fairness tester.
    // Leaf terms are scored for the side to move; kind biases apply only to the root move choice.
    public sealed class BotStyle
    {
        public readonly string Name;
        public readonly int CentreWeight;   // per own token in the centre column, minus the opponent's
        public readonly int HoardWeight;    // per own unused power-up, minus the opponent's
        public readonly int NormalBias;
        public readonly int QueenBias;
        public readonly int CleanerBias;

        public BotStyle(string name, int centreWeight, int hoardWeight, int normalBias, int queenBias, int cleanerBias)
        {
            Name = name;
            CentreWeight = centreWeight;
            HoardWeight = hoardWeight;
            NormalBias = normalBias;
            QueenBias = queenBias;
            CleanerBias = cleanerBias;
        }

        public int Bias(MoveKind kind)
        {
            switch (kind)
            {
                case MoveKind.Queen: return QueenBias;
                case MoveKind.Cleaner: return CleanerBias;
                default: return NormalBias;
            }
        }

        // Adds nothing to Bot.Evaluate, so StyledBot plays exactly like Bot.
        public static readonly BotStyle Default = new BotStyle("Default", 0, 0, 0, 0, 0);
        public static readonly BotStyle Positional = new BotStyle("Positional", 6, 0, 0, 0, 0);
        public static readonly BotStyle Hoarder = new BotStyle("Hoarder", 0, 25, 0, -20, -20);
        public static readonly BotStyle Spender = new BotStyle("Spender", 0, -15, 0, 20, 20);

        public static IReadOnlyList<BotStyle> All { get; } = new[] { Default, Positional, Hoarder, Spender };
    }
}
