using System;
using System.IO;
using KineticRank.Logic;
using UnityEngine;

// Player progression is stored separately from legacy local and solo ratings.
public static class PlayerRankStore
{
    static PlayerRankProfile data;
    static string testPath;
    static string SavePath => testPath ?? Path.Combine(Application.persistentDataPath, "kineticrank_player.json");

    public static PlayerRankProfile GetProfile()
    {
        EnsureLoaded();
        // Callers cannot mutate cached progression without persisting a result.
        return JsonUtility.FromJson<PlayerRankProfile>(JsonUtility.ToJson(data));
    }

    public static void RecordResult(int opponentRating, double score, bool offline = true)
    {
        EnsureLoaded();
        data.RecordResult(opponentRating, score, offline);
        Save();
    }

    public static void RecordMatch(int opponentRating, bool playerWon, bool offline = true)
    {
        RecordResult(opponentRating, playerWon ? 1.0 : 0.0, offline);
    }

    public static void RecordDraw(int opponentRating, bool offline = true)
    {
        RecordResult(opponentRating, 0.5, offline);
    }

    static void EnsureLoaded()
    {
        if (data != null) return;
        try
        {
            if (File.Exists(SavePath))
            {
                var loaded = JsonUtility.FromJson<PlayerRankProfile>(File.ReadAllText(SavePath));
                if (loaded == null || loaded.rating < EloCalculator.MinRating ||
                    loaded.gamesPlayed < 0 || loaded.gamesPlayed == int.MaxValue ||
                    loaded.placementGamesLeft != Math.Max(0, PlayerRankProfile.PlacementCount - loaded.gamesPlayed) ||
                    loaded.peakRating < loaded.rating || loaded.season < 1 ||
                    !Enum.IsDefined(typeof(RankTier), loaded.currentTier) ||
                    loaded.currentDivision != RankBands.DivisionFor(loaded.rating, loaded.currentTier) ||
                    loaded.promotionProtectionGamesLeft < 0 ||
                    loaded.promotionProtectionGamesLeft > PlayerRankProfile.ProtectionDuration)
                    throw new InvalidDataException("Invalid player rank profile.");
                data = loaded;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PlayerRankStore] Load failed: " + ex.Message);
        }
        if (data == null) data = new PlayerRankProfile();
    }

    static void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PlayerRankStore] Save failed: " + ex.Message);
        }
    }

    // Disk tests must never read or overwrite the real player's save.
    public static IDisposable BeginIsolatedTestScope()
    {
        return new TestScope();
    }

    sealed class TestScope : IDisposable
    {
        readonly PlayerRankProfile previousData = data;
        readonly string previousPath = testPath;
        readonly string isolatedPath;
        bool disposed;

        public TestScope()
        {
            isolatedPath = Path.Combine(Path.GetTempPath(), "kineticrank_player_test_" + Guid.NewGuid().ToString("N") + ".json");
            testPath = isolatedPath;
            data = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            data = previousData;
            testPath = previousPath;
            if (File.Exists(isolatedPath)) File.Delete(isolatedPath);
        }
    }
}
