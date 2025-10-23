// File: Assets/Scripts/PlayAgainControls.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayAgainControls : MonoBehaviour
{
    [Tooltip("Enable Enter/Return to reload the active scene (Play Again)")]
    public bool enableEnterToRestart = true;

    void Update()
    {
        if (!enableEnterToRestart) return;

        // Enter on main keyboard or numpad
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Reload the currently active scene; ELO persists via SaveManager
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}
