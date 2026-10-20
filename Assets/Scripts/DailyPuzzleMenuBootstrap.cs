using System;
using System.Reflection;
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
            var type = typeof(DailyStreak);
            var prop = type.GetProperty("CurrentStreak") ?? type.GetProperty("Streak");
            if (prop != null) return (int)prop.GetValue(null);
            var method = type.GetMethod("GetCurrentStreak") ?? type.GetMethod("GetStreak");
            if (method != null) return (int)method.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to get streak: " + ex.Message);
        }
        return 0;
    }

    public static bool IsSolvedToday()
    {
        try
        {
            var type = typeof(DailyStreak);
            var prop = type.GetProperty("HasSolvedToday") ?? type.GetProperty("SolvedToday");
            if (prop != null) return (bool)prop.GetValue(null);
            var method = type.GetMethod("HasSolvedToday") ?? type.GetMethod("IsSolvedToday");
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0) return (bool)method.Invoke(null, null);
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(DateTime))
                    return (bool)method.Invoke(null, new object[] { DateTime.UtcNow });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to check solved status: " + ex.Message);
        }
        return false;
    }

    public static void RecordDailySolve()
    {
        if (_solvedRecordedThisMatch) return;
        _solvedRecordedThisMatch = true;
        try
        {
            var type = typeof(DailyStreak);
            var method = type.GetMethod("RecordSolve");
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                    method.Invoke(null, null);
                else if (parameters.Length == 1 && parameters[0].IsOptional)
                    method.Invoke(null, new object[] { Type.Missing });
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(DateTime))
                    method.Invoke(null, new object[] { DateTime.UtcNow });
                else
                    method.Invoke(null, null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to record solve: " + ex.Message);
        }
    }

    public static object GetTodayPuzzle()
    {
        try
        {
            var type = typeof(DailyPuzzle);
            var method = type.GetMethod("Today");
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0) return method.Invoke(null, null);
                if (parameters.Length == 1 && parameters[0].IsOptional) return method.Invoke(null, new object[] { Type.Missing });
            }
            var forDate = type.GetMethod("ForDate");
            if (forDate != null)
            {
                var parameters = forDate.GetParameters();
                if (parameters.Length == 1) return forDate.Invoke(null, new object[] { DateTime.UtcNow });
                if (parameters.Length >= 2 && parameters[1].IsOptional) return forDate.Invoke(null, new object[] { DateTime.UtcNow, Type.Missing });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to get today's puzzle: " + ex.Message);
        }
        return null;
    }

    public static void LaunchDailyPuzzle()
    {
        if (SceneFader.IsFading) return;
        var entryObj = GetTodayPuzzle();
        if (entryObj == null)
        {
            Debug.LogWarning("[DailyPuzzle] No daily puzzle available for today.");
            return;
        }

        GameState start = null;
        int movesN = 0;
        string id = "";

        if (entryObj is PuzzleEntry pe)
        {
            start = pe.Start;
            movesN = pe.MovesN;
            id = pe.Id;
        }
        else
        {
            var t = entryObj.GetType();
            start = t.GetProperty("Start")?.GetValue(entryObj) as GameState
                    ?? t.GetField("Start")?.GetValue(entryObj) as GameState;
            var movesProp = t.GetProperty("MovesN") ?? t.GetProperty("Moves");
            if (movesProp != null) movesN = (int)movesProp.GetValue(entryObj);
            else
            {
                var movesField = t.GetField("MovesN") ?? t.GetField("Moves");
                if (movesField != null) movesN = (int)movesField.GetValue(entryObj);
            }
            var idProp = t.GetProperty("Id") ?? t.GetProperty("ID");
            if (idProp != null) id = idProp.GetValue(entryObj) as string ?? "";
            else
            {
                var idField = t.GetField("Id") ?? t.GetField("ID");
                if (idField != null) id = idField.GetValue(entryObj) as string ?? "";
            }
        }

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
