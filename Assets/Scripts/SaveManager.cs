using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    [Serializable]
    public class RankedProfile : KineticRank.Logic.RankedProfile
    {
    }

    static KineticRank.Logic.RankedProfile data;
    const int kFactor = 32;
    static string savePathOverride;
    static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "kineticrank_ranked.json");

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    // Install isolation before any profile access, including GameManager lifecycle calls.
    public static IDisposable BeginIsolatedTestScope()
    {
        return new IsolatedTestScope();
    }

    sealed class IsolatedTestScope : IDisposable
    {
        readonly KineticRank.Logic.RankedProfile previousData;
        readonly string previousPath;
        readonly string isolatedPath;
        bool disposed;

        public IsolatedTestScope()
        {
            previousData = data;
            previousPath = savePathOverride;
            isolatedPath = Path.Combine(Application.temporaryCachePath,
                "kineticrank_test_" + Guid.NewGuid().ToString("N") + ".json");
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
                Debug.LogWarning($"[SaveManager] Test cleanup failed: {e.Message}");
            }
        }
    }
#endif

    public static void RecordMatch(bool redWon)
    {
        EnsureLoaded();
        data.RecordMatch(redWon, kFactor);
        Save();
    }

    public static void RecordDraw()
    {
        EnsureLoaded();
        data.RecordDraw(kFactor);
        Save();
    }

    public static void ResetRanked()
    {
        EnsureLoaded();
        data.ResetRanked(preserveHud: true);
        Save();
    }

    public static int GetRedElo() { EnsureLoaded(); return data.redElo; }
    public static int GetBlueElo() { EnsureLoaded(); return data.blueElo; }
    public static int GetGames() { EnsureLoaded(); return data.games; }
    public static int GetLastDeltaRed() { EnsureLoaded(); return data.lastDeltaRed; }
    public static int GetLastDeltaBlue() { EnsureLoaded(); return data.lastDeltaBlue; }
    public static int GetRedWins() { EnsureLoaded(); return data.redWins; }
    public static int GetBlueWins() { EnsureLoaded(); return data.blueWins; }
    public static int GetDraws() { EnsureLoaded(); return data.draws; }
    public static bool GetHudVisible() { EnsureLoaded(); return data.hudVisible; }

    public static void SetHudVisible(bool visible)
    {
        EnsureLoaded();
        if (data.hudVisible == visible) return;
        data.hudVisible = visible;
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
                var loaded = JsonUtility.FromJson<KineticRank.Logic.RankedProfile>(json);
                data = loaded ?? new KineticRank.Logic.RankedProfile();
            }
            else
            {
                data = new KineticRank.Logic.RankedProfile();
                Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Load failed: {e.Message}");
            data = new KineticRank.Logic.RankedProfile();
        }
    }

    static void Save()
    {
        try
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
        }
    }
}
