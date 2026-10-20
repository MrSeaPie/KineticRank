using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DailyPuzzleMenuBootstrap : MonoBehaviour
{
    public static DailyPuzzleMenuBootstrap Instance { get; private set; }
    public static bool IsDailyMatchActive { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (Instance != null) return;
        var host = new GameObject("DailyPuzzleMenuBootstrap");
        DontDestroyOnLoad(host);
        Instance = host.AddComponent<DailyPuzzleMenuBootstrap>();
    }

    public static int GetCurrentStreak()
    {
        try
        {
            var type = Type.GetType("KineticRank.Logic.DailyStreak, KineticRank.Logic") ?? Type.GetType("KineticRank.Logic.DailyStreak");
            if (type == null) return 0;
            var prop = type.GetProperty("CurrentStreak") ?? type.GetProperty("Streak") ?? type.GetProperty("Current");
            if (prop != null) return (int)prop.GetValue(null);
            var method = type.GetMethod("GetStreak") ?? type.GetMethod("GetCurrentStreak");
            if (method != null) return (int)method.Invoke(null, null);
        }
        catch { }
        return 0;
    }

    void OnGUI()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "MainMenu") return;

        int streak = GetCurrentStreak();
        float x = Screen.width - 220;
        float y = 20;
        GUI.Box(new Rect(x, y, 200, 75), "Daily Challenge");
        GUI.Label(new Rect(x + 10, y + 22, 180, 20), $"Streak: {streak} days");

        if (GUI.Button(new Rect(x + 10, y + 44, 180, 24), "Play Daily Puzzle"))
        {
            LaunchDailyPuzzle();
        }
    }

    public static void LaunchDailyPuzzle()
    {
        IsDailyMatchActive = true;
    }
}
