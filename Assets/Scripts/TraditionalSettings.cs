// Traditional (no power-ups) match flag chosen in MainMenu and read by the Match scene. Not persisted; defaults to off.
// Combines with hot-seat or SoloSettings; Traditional results are always casual and never touch ranked, solo ladder or rank files.
public static class TraditionalSettings
{
    public static bool Enabled { get; private set; }

    public static void StartTraditional()
    {
        Enabled = true;
    }

    public static void Clear()
    {
        Enabled = false;
    }
}
