// File: Assets/Scripts/WinBanner.cs  (FULL FILE REPLACEMENT)
using UnityEngine;
using TMPro;

public class WinBanner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP text to display the win/draw banner. If left empty, this component will try GetComponent<TMP_Text>().")]
    public TMP_Text text;

    [Tooltip("Optional TMP text for short transient messages (e.g. ranked reset). If empty, one is created at runtime as a sibling of this banner so it never overwrites the result text.")]
    public TMP_Text toastText;

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

    /// <summary>
    /// Shows a transient message on the separate toast line. Never touches the result text.
    /// Timing is left to the caller, because this banner may be inactive and unable to run coroutines.
    /// </summary>
    public void ShowToast(string message, Color color)
    {
        var t = EnsureToastText();
        if (!t)
        {
            Debug.Log($"[WinBanner] {message}");
            return;
        }

        t.text = message;
        t.color = color;
        t.alpha = 1f;
        t.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the toast line only; the result banner is left as it is.
    /// </summary>
    public void HideToast()
    {
        if (!toastText) return;
        toastText.text = "";
        toastText.gameObject.SetActive(false);
    }

    // Creates the toast line next to the banner (not as a child, since Hide() deactivates this object).
    TMP_Text EnsureToastText()
    {
        if (toastText) return toastText;
        if (!text) text = GetComponent<TMP_Text>();
        if (!text) return null;

        var parent = transform.parent;
        var go = new GameObject("ToastText");

        if (text is TextMeshProUGUI)
        {
            // UI text must live under a Canvas
            if (!parent)
            {
                Destroy(go);
                return null;
            }

            var ui = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(900f, 60f);

            ui.raycastTarget = false;
            toastText = ui;
        }
        else
        {
            var world = go.AddComponent<TextMeshPro>();
            go.transform.SetParent(parent, false);
            go.transform.position = transform.position + Vector3.up * 1.5f;
            toastText = world;
        }

        toastText.font = text.font;
        toastText.fontSize = text.fontSize * 0.5f;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.text = "";
        go.transform.SetAsLastSibling();
        go.SetActive(false);
        return toastText;
    }
}
