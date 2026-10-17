// File: Assets/Scripts/RankedControls.cs  (FULL FILE REPLACEMENT)
using System.Collections;
using KineticRank.Logic;
using UnityEngine;

public class RankedControls : MonoBehaviour
{
    [Header("Optional UI")]
    public WinBanner winBanner;    // Drag Canvas/WinBanner here
    public GameObject eloHudObject; // Drag Canvas/EloHUD (TMP) here

    [Tooltip("How long to show the 'Ranked reset' toast")]
    public float toastSeconds = 1.25f;

    [Tooltip("Second Backspace must come within this many seconds (unscaled) to confirm a ranked reset")]
    public float resetConfirmSeconds = 2f;

    ConfirmWindow resetConfirm;
    Coroutine toastRoutine;

    void Awake()
    {
        resetConfirm = new ConfirmWindow(resetConfirmSeconds);

        // Fallback: try find EloHUD by name if not linked
        if (!eloHudObject)
        {
            var hud = GameObject.Find("EloHUD");
            if (hud) eloHudObject = hud;
        }

        // Apply saved HUD visibility (default TRUE on fresh installs)
        if (eloHudObject)
            eloHudObject.SetActive(SaveManager.GetHudVisible());
    }

    void Update()
    {
        // Reset ranked (Backspace twice within resetConfirmSeconds)
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            // Block mid-match resets; allowed in menus (no GameManager) and after a win or draw
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsMatchOver)
            {
                resetConfirm.Cancel();
                ShowToast("Finish the match before resetting ranked", Color.yellow, toastSeconds);
            }
            else if (resetConfirm.Press(Time.unscaledTime))
            {
                SaveManager.ResetRanked();
                EloHud.RefreshAll();
                ShowToast("Ranked reset", Color.cyan, toastSeconds);
            }
            else
            {
                ShowToast("Press Backspace again to reset ranked", Color.yellow, resetConfirmSeconds);
            }
        }

        // Toggle HUD visibility (H)
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (eloHudObject)
            {
                bool next = !eloHudObject.activeSelf;
                eloHudObject.SetActive(next);
                SaveManager.SetHudVisible(next); // persist preference
            }
        }
    }

    void ShowToast(string text, Color color, float seconds)
    {
        if (winBanner == null)
        {
            Debug.Log("<color=#00FFFF>[Ranked]</color> " + text + " (no WinBanner assigned)");
            return;
        }

        // Restart instead of stacking, so an older toast cannot hide a newer one early.
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Toast(text, color, seconds));
    }

    // Uses the banner's separate toast line, so the winner/draw result text is never overwritten or hidden.
    IEnumerator Toast(string text, Color color, float seconds)
    {
        winBanner.ShowToast(text, color);
        yield return new WaitForSecondsRealtime(seconds);
        winBanner.HideToast();
        toastRoutine = null;
    }
}
