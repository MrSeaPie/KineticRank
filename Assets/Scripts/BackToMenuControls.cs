// File: Assets/Scripts/BackToMenuControls.cs  (FULL FILE REPLACEMENT — with SFX + fade)
using UnityEngine;

public class BackToMenuControls : MonoBehaviour
{
    [Tooltip("Press this key to return to the Main Menu scene.")]
    public KeyCode backToMenuKey = KeyCode.M;

    [Tooltip("The exact name of your menu scene in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    void Update()
    {
        if (Input.GetKeyDown(backToMenuKey))
        {
            BackToMenuNow();
        }
    }

    // Called by button OnClick or via hotkey
    public void BackToMenuNow()
    {
        AudioController.Instance.PlayClick();

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[BackToMenuControls] mainMenuSceneName is empty.");
            return;
        }
        SceneFader.LoadScene(mainMenuSceneName, 0.3f);
    }
}
