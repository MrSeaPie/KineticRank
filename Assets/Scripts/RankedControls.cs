// File: Assets/Scripts/RankedControls.cs  (FULL FILE REPLACEMENT)
using System.Collections;
using TMPro;
using UnityEngine;

public class RankedControls : MonoBehaviour
{
    [Header("Optional UI")]
    public WinBanner winBanner; // Drag Canvas/WinBanner here
    public GameObject eloHudObject; // Drag Canvas/EloHUD (TMP) here

    [Tooltip("How long to show the 'Ranked reset' toast")]
    public float toastSeconds = 1.25f;

    void Awake()
    {
        // Fallback: try find EloHUD by name if not linked
        if (!eloHudObject)
        {
            var hud = GameObject.Find("EloHUD");
            if (hud) eloHudObject = hud;
        }
    }

    void Update()
    {
        // Reset ranked
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SaveManager.ResetRanked();
            EloHud.RefreshAll();

            if (winBanner != null)
                StartCoroutine(ToastRankedReset());
            else
                Debug.Log("<color=#00FFFF>[Ranked]</color> Reset ELO (no WinBanner assigned)");
        }

        // Toggle HUD
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (eloHudObject)
                eloHudObject.SetActive(!eloHudObject.activeSelf);
        }
    }

    IEnumerator ToastRankedReset()
    {
        winBanner.Show("Ranked reset", Color.cyan);
        yield return new WaitForSeconds(toastSeconds);
        winBanner.Hide();
    }
}
