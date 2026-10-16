// File: Assets/Scripts/WinBanner.cs  (FULL FILE REPLACEMENT)
using UnityEngine;
using TMPro;

public class WinBanner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP text to display the win/draw banner. If left empty, this component will try GetComponent<TMP_Text>().")]
    public TMP_Text text;

    [Header("Behavior")]
    [Tooltip("Hide the banner automatically on Awake().")]
    [SerializeField] bool hideOnAwake = true;

    void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        if (hideOnAwake) Hide();
    }

    /// <summary>
    /// Shows the banner with a message and color (e.g., Color.red / Color.cyan / Color.yellow).
    /// </summary>
    public void Show(string message, Color color)
    {
        if (!text) return;

        // Ensure object is active/visible
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Apply content + color
        text.text = message;
        text.color = color;

        // Make sure alpha isn't 0 from prior styling
        text.alpha = 1f;
    }

    /// <summary>
    /// Appends a small second line, e.g. "Press Enter for rematch".
    /// Uses smaller, light-gray text via TMP rich text.
    /// </summary>
    public void AppendHint(string hint)
    {
        if (!text) return;
        text.text += $"\n<size=70%><color=#DDDDDD>{hint}</color></size>";
    }

    /// <summary>
    /// Hides the banner and clears any message.
    /// </summary>
    public void Hide()
    {
        if (text) text.text = "";
        gameObject.SetActive(false);
    }
}
