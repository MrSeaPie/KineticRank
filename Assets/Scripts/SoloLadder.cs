using System;
using System.IO;
using UnityEngine;
using KineticRank.Logic;

// Persists solo-vs-bot results in its own file, separate from SaveManager's ranked Elo profile.
public static class SoloLadder
{
    static SoloProfile data;
    static string savePathOverride;
    static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "kineticrank_solo.json");

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    public static IDisposable BeginIsolatedTestScope()
    {
        return new IsolatedTestScope();
    }

    sealed class IsolatedTestScope : IDisposable
    {
        readonly SoloProfile previousData;
        readonly string previousPath;
        readonly string isolatedPath;
        bool disposed;

        public IsolatedTestScope()
        {
            previousData = data;
            previousPath = savePathOverride;
            isolatedPath = Path.Combine(Application.temporaryCachePath,
                "kineticrank_solo_test_" + Guid.NewGuid().ToString("N") + ".json");
            savePathOverride = isolatedPath;
            data = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            data = previousData;
            savePathOverride = previousPath;
            try
            {
                if (File.Exists(isolatedPath)) File.Delete(isolatedPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SoloLadder] Test cleanup failed: {e.Message}");
            }
        }
    }
#endif

    public static void RecordWin(BotTier tier) { EnsureLoaded(); data.RecordWin(tier); Save(); }
    public static void RecordLoss(BotTier tier) { EnsureLoaded(); data.RecordLoss(tier); Save(); }
    public static void RecordDraw(BotTier tier) { EnsureLoaded(); data.RecordDraw(tier); Save(); }

    public static int GetWins(BotTier tier) { EnsureLoaded(); return data.GetWins(tier); }
    public static int GetLosses(BotTier tier) { EnsureLoaded(); return data.GetLosses(tier); }
    public static int GetDraws(BotTier tier) { EnsureLoaded(); return data.GetDraws(tier); }
    public static bool TryGetHighestTierBeaten(out BotTier tier) { EnsureLoaded(); return data.TryGetHighestTierBeaten(out tier); }

    public static void ResetSolo()
    {
        EnsureLoaded();
        data.Reset();
        Save();
    }

    static void EnsureLoaded()
    {
        if (data == null) Load();
    }

    static void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SoloProfile>(json) ?? new SoloProfile();
            }
            else
            {
                data = new SoloProfile();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SoloLadder] Load failed: {e.Message}");
            data = new SoloProfile();
        }
    }

    static void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SoloLadder] Save failed: {e.Message}");
        }
    }
}
