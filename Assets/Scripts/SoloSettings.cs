using KineticRank.Logic;

// Solo match configuration chosen in MainMenu and read by Match. Not persisted.
public static class SoloSettings
{
    public static bool Enabled { get; private set; }
    public static bool IsRanked { get; private set; }
    public static BotTier Tier { get; private set; } = BotTier.Pine;

    // The human plays Red by default; rematch starting order is tracked separately.
    public static bool BotPlaysRed { get; private set; }

    public static int Depth => BotTiers.Depth(Tier);

    public static void StartSolo(BotTier tier, bool botPlaysRed = false)
    {
        Tier = tier;
        BotPlaysRed = botPlaysRed;
        IsRanked = false;
        Enabled = true;
    }

    public static void StartRankedSolo(BotTier tier, bool botPlaysRed = false)
    {
        StartSolo(tier, botPlaysRed);
        IsRanked = true;
    }

    public static void Clear()
    {
        Enabled = false;
        IsRanked = false;
        Tier = BotTier.Pine;
        BotPlaysRed = false;
    }
}
