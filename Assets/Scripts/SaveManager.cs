// File: Assets/Scripts/SaveManager.cs  (FULL FILE REPLACEMENT)
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
        public int games = 0;
        public int lastDeltaRed = 0;
        public int lastDeltaBlue = 0;

        public int redWins = 0;
        public int blueWins = 0;
    }

    static RankedProfile data;
    static readonly int kFactor = 32;
    static string SavePath => Path.Combine(Application.persistentDataPath, "kineticrank_ranked.json");

    static SaveManager() { Load(); }

    // ───────────────── Public API ─────────────────
    public static void RecordMatch(bool redWon)
    {
        EnsureLoaded();

        int oldRed = data.redElo;
        int oldBlue = data.blueElo;

        float expRed  = Expected(oldRed,  oldBlue);
        float expBlue = Expected(oldBlue, oldRed);

        int sRed  = redWon ? 1 : 0;
        int sBlue = redWon ? 0 : 1;

        int newRed  = Mathf.RoundToInt(oldRed  + kFactor * (sRed  - expRed));
        int newBlue = Mathf.RoundToInt(oldBlue + kFactor * (sBlue - expBlue));

        data.lastDeltaRed  = newRed  - oldRed;
        data.lastDeltaBlue = newBlue - oldBlue;
        data.redElo  = Mathf.Max(1, newRed);
        data.blueElo = Mathf.Max(1, newBlue);
        data.games  += 1;

        if (redWon) data.redWins += 1; else data.blueWins += 1;

        Save();
    }

    public static void ResetRanked()
    {
        data = new RankedProfile();
        Save();
    }

    public static int GetRedElo()        { EnsureLoaded(); return data.redElo; }
    public static int GetBlueElo()       { EnsureLoaded(); return data.blueElo; }
    public static int GetGames()         { EnsureLoaded(); return data.games; }
    public static int GetLastDeltaRed()  { EnsureLoaded(); return data.lastDeltaRed; }
    public static int GetLastDeltaBlue() { EnsureLoaded(); return data.lastDeltaBlue; }
    public static int GetRedWins()       { EnsureLoaded(); return data.redWins; }
    public static int GetBlueWins()      { EnsureLoaded(); return data.blueWins; }

    // ───────────────── Internals ─────────────────
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
                var loaded = JsonUtility.FromJson<RankedProfile>(json);
                data = loaded ?? new RankedProfile();
            }
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
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
        }
    }

    static float Expected(int ra, int rb)
    {
        float expo = (rb - ra) / 400f;
        return 1f / (1f + Mathf.Pow(10f, expo));
    }
}
