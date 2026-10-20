using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using KineticRank.Logic;

public class DailyPuzzleMenuBootstrap : MonoBehaviour
{
    public static DailyPuzzleMenuBootstrap Instance { get; private set; }
    public static bool IsDailyPuzzleActive { get; set; }
    private static bool _solvedRecordedThisMatch = false;

    private static int _cachedStreak = 0;
    private static bool _cachedSolvedToday = false;

    private Button _dailyButton;
    private GameObject _streakLabelObj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (Instance != null) return;
        var host = new GameObject("DailyPuzzleMenuBootstrap");
        DontDestroyOnLoad(host);
        Instance = host.AddComponent<DailyPuzzleMenuBootstrap>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshCachedStatus();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _dailyButton = null;
        _streakLabelObj = null;
        RefreshCachedStatus();
    }

    public static void RefreshCachedStatus()
    {
        try
        {
            var now = DateTime.UtcNow;
            var record = DailyStreak.Load();
            int todayNum = DailyStreak.DayNumber(now);
            _cachedStreak = DailyStreak.CurrentAsOf(record, todayNum);
            _cachedSolvedToday = (record.LastSolvedDay == todayNum);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DailyPuzzle] Failed to refresh cached streak: " + ex.Message);
        }
    }

    public static int GetCurrentStreak()
    {
        RefreshCachedStatus();
        return _cachedStreak;
    }

    public static bool IsSolvedToday()
    {
        RefreshCachedStatus();
        return _cachedSolvedToday;
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
        RefreshCachedStatus();
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

        if (entry.Start == null)
        {
            Debug.LogWarning("[DailyPuzzle] Could not extract start state from puzzle object.");
            return;
        }

        IsDailyPuzzleActive = true;
        _solvedRecordedThisMatch = false;

        var menu = UnityEngine.Object.FindObjectOfType<MainMenuController>();
        if (menu != null)
        {
            menu.PlayPuzzle(entry);
        }
        else
        {
            SoloSettings.Clear();
            bool attackerIsRed = new PuzzleSession(entry.Start, entry.MovesN).AttackerIsRed;
            PuzzleSettings.StartPuzzle(string.IsNullOrEmpty(entry.Id) ? "daily" : entry.Id, attackerIsRed);
            PuzzleMatchDriver.Begin(entry.Start, entry.MovesN);

            if (AudioController.Instance != null) AudioController.Instance.PlayClick();
            SceneFader.LoadScene("Match", 0.3f);
        }
    }

    void Update()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu")
        {
            IsDailyPuzzleActive = false;
            _solvedRecordedThisMatch = false;

            EnsureUI();

            var menu = FindObjectOfType<MainMenuController>();
            if (menu == null || !menu.isActiveAndEnabled) return;

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

    void EnsureUI()
    {
        var menu = FindObjectOfType<MainMenuController>();
        if (menu == null || menu.playButton == null) return;

        if (_dailyButton == null)
        {
            var existingBtn = GameObject.Find("DailyPuzzleButton");
            if (existingBtn != null)
            {
                _dailyButton = existingBtn.GetComponent<Button>();
            }
            else
            {
                var btnObj = Instantiate(menu.playButton.gameObject, menu.playButton.transform.parent);
                btnObj.name = "DailyPuzzleButton";
                if (menu.resetButton != null)
                {
                    btnObj.transform.SetSiblingIndex(menu.resetButton.transform.GetSiblingIndex());
                }
                _dailyButton = btnObj.GetComponent<Button>();
                _dailyButton.onClick.RemoveAllListeners();
                _dailyButton.onClick.AddListener(OnDailyButtonClicked);
            }
        }

        if (_dailyButton != null)
        {
            _dailyButton.interactable = menu.isActiveAndEnabled;

            var btnText = _dailyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = _cachedSolvedToday ? "Daily Puzzle (Solved)" : "Daily Puzzle";
            }
            else
            {
                var t = _dailyButton.GetComponentInChildren<Text>();
                if (t != null) t.text = _cachedSolvedToday ? "Daily Puzzle (Solved)" : "Daily Puzzle";
            }

            if (_streakLabelObj == null)
            {
                var existingLabel = GameObject.Find("DailyStreakLabel");
                if (existingLabel != null)
                {
                    _streakLabelObj = existingLabel;
                }
                else if (btnText != null)
                {
                    _streakLabelObj = Instantiate(btnText.gameObject, menu.playButton.transform.parent);
                    _streakLabelObj.name = "DailyStreakLabel";
                    if (menu.resetButton != null)
                    {
                        _streakLabelObj.transform.SetSiblingIndex(menu.resetButton.transform.GetSiblingIndex());
                    }
                }
            }

            if (_streakLabelObj != null)
            {
                var labelTmp = _streakLabelObj.GetComponent<TMP_Text>();
                if (labelTmp != null)
                {
                    labelTmp.text = _cachedSolvedToday ? $"Streak: {_cachedStreak}d (Solved!)" : $"Streak: {_cachedStreak}d";
                }
                else
                {
                    var labelLegacy = _streakLabelObj.GetComponent<Text>();
                    if (labelLegacy != null)
                    {
                        labelLegacy.text = _cachedSolvedToday ? $"Streak: {_cachedStreak}d (Solved!)" : $"Streak: {_cachedStreak}d";
                    }
                }
            }
        }
    }

    static void OnDailyButtonClicked()
    {
        var menu = FindObjectOfType<MainMenuController>();
        if (menu == null || !menu.isActiveAndEnabled || SceneFader.IsFading) return;
        LaunchDailyPuzzle();
    }

    void OnGUI()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Match" && IsDailyPuzzleActive)
        {
            DrawMatchHUD();
        }
    }

    void DrawMatchHUD()
    {
        int streak = _cachedStreak;
        bool solved = _cachedSolvedToday || _solvedRecordedThisMatch;
        string text = solved ? $"Daily Challenge [Streak: {streak}d - Solved!]" : $"Daily Challenge [Streak: {streak}d]";
        GUI.Label(new Rect(20, Screen.height - 35, 300, 24), text);
    }
}
