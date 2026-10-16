// File: Assets/Scripts/MainMenuController.cs  (FULL FILE REPLACEMENT — with SFX + fade)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    void Awake()
    {
        if (playButton)  playButton.onClick.AddListener(Play);
        if (resetButton) resetButton.onClick.AddListener(ResetRanked);
        if (quitButton)  quitButton.onClick.AddListener(QuitGame);

        RefreshSummary();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Play();
        if (Input.GetKeyDown(KeyCode.Backspace)) ResetRanked();
        if (Input.GetKeyDown(KeyCode.Escape))     QuitGame();
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

    public void Play()
    {
        AudioController.Instance.PlayClick();
        if (string.IsNullOrEmpty(matchSceneName))
        {
            Debug.LogWarning("[MainMenu] matchSceneName is empty.");
            return;
        }
        SceneFader.LoadScene(matchSceneName, 0.3f);
    }

    public void ResetRanked()
    {
        AudioController.Instance.PlayClick();
        SaveManager.ResetRanked();
        RefreshSummary();
        EloHud.RefreshAll();
    }

    public void QuitGame()
    {
        AudioController.Instance.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL cannot quit programmatically
#else
        Application.Quit();
#endif
    }
}
