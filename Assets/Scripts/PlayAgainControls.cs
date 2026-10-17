// File: Assets/Scripts/PlayAgainControls.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayAgainControls : MonoBehaviour
{
    [Tooltip("Enable Enter/Return to reload the active scene (Play Again)")]
    public bool enableEnterToRestart = true;

    // Set once a reload has been requested so repeated Enter presses don't queue extra loads
    bool reloading;

    void Update()
    {
        // Also ignore Enter while another fade (e.g. Back to Menu) is already loading a scene
        if (!enableEnterToRestart || reloading || SceneFader.IsFading) return;

        // Enter on main keyboard or numpad
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            reloading = true;

            // Reload the currently active scene through the fader so it shares the in-progress guard; ELO persists via SaveManager
            var active = SceneManager.GetActiveScene();
            SceneFader.LoadScene(active.name, 0.3f);
        }
    }
}
