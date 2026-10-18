using KineticRank.Logic;

// Solo match configuration chosen in MainMenu and read by the Match scene. Not persisted; defaults to off.
public static class SoloSettings
{
    public static bool Enabled { get; private set; }
    public static BotTier Tier { get; private set; } = BotTier.Pine;

    // The human plays Red (moves first) by default; the bot takes the other side
    public static bool BotPlaysRed { get; private set; }

    public static int Depth => BotTiers.Depth(Tier);

    public static void StartSolo(BotTier tier, bool botPlaysRed = false)
    {
        Tier = tier;
        BotPlaysRed = botPlaysRed;
        Enabled = true;
    }

    public static void Clear()
    {
        Enabled = false;
        Tier = BotTier.Pine;
        BotPlaysRed = false;
    }
}
