// File: Assets/Scripts/PlayAgainControls.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using KineticRank.Logic;

public class PlayAgainControls : MonoBehaviour
{
    [Tooltip("Enable Enter/Return to reload the active scene after the match ends (Play Again)")]
    public bool enableEnterToRestart = true;

    // Set once a reload has been requested so repeated Enter presses don't queue extra loads
    bool reloading;

    bool CanRestartMatch()
    {
        var manager = GameManager.Instance;
        return enableEnterToRestart && !reloading && !SceneFader.IsFading
            && manager != null && manager.IsMatchOver && !manager.IsResolvingMove;
    }

    void Update()
    {
        // Only completed matches accept Enter; ignore input during moves and scene transitions.
        if (!CanRestartMatch()) return;

        // Enter on main keyboard or numpad
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            reloading = true;
            RematchTracker.RegisterRematch();

            // Reload the currently active scene through the fader; ELO persists via SaveManager.
            var active = SceneManager.GetActiveScene();
            SceneFader.LoadScene(active.name, 0.3f);
        }
    }
}
