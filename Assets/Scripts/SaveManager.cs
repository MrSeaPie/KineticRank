using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    [Serializable]
    public class RankedProfile
    {
        public int redElo = 1000;
        public int blueElo = 1000;
        public int games;
        public int lastDeltaRed;
        public int lastDeltaBlue;
        public int redWins;
        public int blueWins;
        public int draws;
        public bool hudVisible = true;
    }

    static RankedProfile data;
    const int kFactor = 32;
    static string SavePath => Path.Combine(Application.persistentDataPath, "kineticrank_ranked.json");

    static SaveManager() { Load(); }

    public static void RecordMatch(bool redWon)
    {
        RecordResult(redWon ? 1f : 0f);
    }

    public static void RecordDraw()
    {
        RecordResult(0.5f);
    }

    static void RecordResult(float redScore)
    {
        EnsureLoaded();
        int oldRed = data.redElo;
        int oldBlue = data.blueElo;
        data.redElo = Mathf.Max(1, Mathf.RoundToInt(oldRed + kFactor * (redScore - Expected(oldRed, oldBlue))));
        data.blueElo = Mathf.Max(1, Mathf.RoundToInt(oldBlue + kFactor * (1f - redScore - Expected(oldBlue, oldRed))));
        // Report the actual change, including the rating floor.
        data.lastDeltaRed = data.redElo - oldRed;
        data.lastDeltaBlue = data.blueElo - oldBlue;
        data.games++;
        if (redScore == 1f) data.redWins++;
        else if (redScore == 0f) data.blueWins++;
        else data.draws++;
        Save();
    }

    public static void ResetRanked()
    {
        bool keepHud = GetHudVisible();
        data = new RankedProfile { hudVisible = keepHud };
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

    static float Expected(int rating, int opponent)
    {
        return 1f / (1f + Mathf.Pow(10f, ((float)opponent - rating) / 400f));
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
                data = JsonUtility.FromJson<RankedProfile>(File.ReadAllText(SavePath)) ?? new RankedProfile();
            else
            {
                data = new RankedProfile();
                Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Load failed: {e.Message}");
            data = new RankedProfile();
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
            Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
        }
    }
}
