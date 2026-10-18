// File: Assets/Scripts/MainMenuController.cs  (FULL FILE REPLACEMENT - with SFX + fade)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Label that shows current ranked summary")]
    public TMP_Text rankedSummary;

    [Tooltip("Play button")]
    public Button playButton;

    [Tooltip("Reset Ranked button")]
    public Button resetButton;

    [Tooltip("Quit button (optional)")]
    public Button quitButton;

    [Header("Scene")]
    [Tooltip("Name of the gameplay scene (e.g., 'Match')")]
    public string matchSceneName = "Match";

    [Header("Reset Confirm")]
    [Tooltip("Seconds allowed between the two reset presses (Backspace or Reset button)")]
    public float resetConfirmSeconds = 2f;

    ConfirmWindow resetConfirm;
    bool showingResetPrompt;

    void Awake()
    {
        resetConfirm = new ConfirmWindow(resetConfirmSeconds);

        if (playButton)  playButton.onClick.AddListener(Play);
        if (resetButton) resetButton.onClick.AddListener(ResetRanked);
        if (quitButton)  quitButton.onClick.AddListener(QuitGame);

        RefreshSummary();
    }

    void Update()
    {
        // Ignore menu keys while a scene load is fading out (no repeated clicks, no reset mid-load)
        if (SceneFader.IsFading) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Play();
        if (Input.GetKeyDown(KeyCode.Backspace)) ResetRanked();
        if (Input.GetKeyDown(KeyCode.Escape))     QuitGame();

        // Restore the summary once the confirm window lapses without a second press
        if (showingResetPrompt && !resetConfirm.IsArmed(Time.unscaledTime))
        {
            showingResetPrompt = false;
            RefreshSummary();
        }
    }

    void RefreshSummary()
    {
        if (!rankedSummary) return;

        int red  = SaveManager.GetRedElo();
        int blue = SaveManager.GetBlueElo();
        int rw   = SaveManager.GetRedWins();
        int bw   = SaveManager.GetBlueWins();

        rankedSummary.text = $"RED {red} (W{rw})  |  BLUE {blue} (W{bw})";
    }

    static void PlayClick()
    {
        if (AudioController.Instance != null) AudioController.Instance.PlayClick();
    }

    public void Play()
    {
        if (SceneFader.IsFading) return;

        // Hot-seat ranked match: a previous solo choice must not carry over
        SoloSettings.Clear();
        LoadMatch();
    }

    // Solo match vs the bot at the chosen tier (called by SoloMenuPanel)
    public void PlaySolo(BotTier tier)
    {
        if (SceneFader.IsFading) return;

        SoloSettings.StartSolo(tier);
        if (!LoadMatch()) SoloSettings.Clear();
    }

    bool LoadMatch()
    {
        PlayClick();
        if (string.IsNullOrEmpty(matchSceneName))
        {
            Debug.LogWarning("[MainMenu] matchSceneName is empty.");
            return false;
        }
        SceneFader.LoadScene(matchSceneName, 0.3f);
        return true;
    }

    // First press arms the confirm window; a second press within resetConfirmSeconds resets
    public void ResetRanked()
    {
        PlayClick();

        if (!resetConfirm.Press(Time.unscaledTime))
        {
            showingResetPrompt = true;
            if (rankedSummary) rankedSummary.text = "Press Backspace (or Reset) again to reset ranked";
            else Debug.Log("[MainMenu] Press Backspace again to reset ranked.");
            return;
        }

        showingResetPrompt = false;
        SaveManager.ResetRanked();
        RefreshSummary();
        EloHud.RefreshAll();
    }

    public void QuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL cannot quit programmatically
#else
        Application.Quit();
#endif
    }
}
