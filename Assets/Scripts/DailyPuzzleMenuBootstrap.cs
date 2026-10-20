using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using KineticRank.Logic;

public class DailyPuzzleMenuBootstrap : MonoBehaviour
{
    public static DailyPuzzleMenuBootstrap Instance { get; private set; }
    public static bool IsDailyPuzzleActive { get; set; }
    private static bool _solvedRecordedThisMatch = false;

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
            var now = DateTime.UtcNow;
            return DailyStreak.CurrentAsOf(DailyStreak.Load(), DailyStreak.DayNumber(now));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to get streak: " + ex.Message);
            return 0;
        }
    }

    public static bool IsSolvedToday()
    {
        try
        {
            var now = DateTime.UtcNow;
            return DailyStreak.Load().LastSolvedDay == DailyStreak.DayNumber(now);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to check solved status: " + ex.Message);
            return false;
        }
    }

    public static void RecordDailySolve()
    {
        if (_solvedRecordedThisMatch) return;
        _solvedRecordedThisMatch = true;
        try
        {
            DailyStreak.RecordSolve(DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to record solve: " + ex.Message);
        }
    }

    public static PuzzleEntry GetTodayPuzzle()
    {
        try
        {
            return DailyPuzzle.Today();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to get today's puzzle: " + ex.Message);
            return null;
        }
    }

    public static void LaunchDailyPuzzle()
    {
        if (SceneFader.IsFading) return;
        var entry = GetTodayPuzzle();
        if (entry == null)
        {
            Debug.LogWarning("[DailyPuzzle] No daily puzzle available for today.");
            return;
        }

        GameState start = entry.Start;
        int movesN = entry.MovesN;
        string id = entry.Id;

        if (start == null)
        {
            Debug.LogWarning("[DailyPuzzle] Could not extract start state from puzzle object.");
            return;
        }

        SoloSettings.Clear();
        bool attackerIsRed = new PuzzleSession(start, movesN).AttackerIsRed;
        PuzzleSettings.StartPuzzle(string.IsNullOrEmpty(id) ? "daily" : id, attackerIsRed);
        PuzzleMatchDriver.Begin(start, movesN);

        IsDailyPuzzleActive = true;
        _solvedRecordedThisMatch = false;

        if (AudioController.Instance != null) AudioController.Instance.PlayClick();
        SceneFader.LoadScene("Match", 0.3f);
    }

    void Update()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu")
        {
            IsDailyPuzzleActive = false;
            _solvedRecordedThisMatch = false;

            if (!SceneFader.IsFading && Input.GetKeyDown(KeyCode.D))
            {
                LaunchDailyPuzzle();
            }
            return;
        }

        if (sceneName == "Match" && IsDailyPuzzleActive && !_solvedRecordedThisMatch)
        {
            var driver = FindObjectOfType<PuzzleMatchDriver>();
            if (driver != null && driver.Session != null)
            {
                if (driver.Session.Status == PuzzleStatus.Solved)
                {
                    RecordDailySolve();
                }
            }
        }
    }

    void OnGUI()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu")
        {
            DrawMainMenuHUD();
        }
        else if (sceneName == "Match" && IsDailyPuzzleActive)
        {
            DrawMatchHUD();
        }
    }

    void DrawMainMenuHUD()
    {
        int streak = GetCurrentStreak();
        bool solved = IsSolvedToday();

        float width = 210f;
        float height = 75f;
        float x = Screen.width - width - 20f;
        float y = 20f;

        GUI.Box(new Rect(x, y, width, height), "Daily Challenge (D)");
        string statusText = solved ? $"Streak: {streak}d (Solved!)" : $"Streak: {streak}d";
        GUI.Label(new Rect(x + 10, y + 22, width - 20, 20), statusText);

        string buttonText = solved ? "Replay Today's Daily" : "Play Daily Puzzle";
        if (GUI.Button(new Rect(x + 10, y + 44, width - 20, 24), buttonText))
        {
            LaunchDailyPuzzle();
        }
    }

    void DrawMatchHUD()
    {
        int streak = GetCurrentStreak();
        bool solved = IsSolvedToday() || _solvedRecordedThisMatch;
        string text = solved ? $"Daily Challenge [Streak: {streak}d - Solved!]" : $"Daily Challenge [Streak: {streak}d]";
        GUI.Label(new Rect(20, Screen.height - 35, 300, 24), text);
    }
}
