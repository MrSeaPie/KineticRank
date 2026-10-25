using UnityEngine;
using TMPro;

public class WinBanner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP text to display the win/draw banner. If left empty, this component will try GetComponent<TMP_Text>().")]
    public TMP_Text text;

    [Tooltip("Optional separate text for transient messages.")]
    public TMP_Text toastText;

    [Header("Behavior")]
    [SerializeField] bool hideOnAwake = true;

    void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        if (hideOnAwake) Hide();
    }

    public void Show(string message, Color color)
    {
        if (!text) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        text.text = message;
        text.color = color;
        text.alpha = 1f;
    }

    // Keep the result and its rematch hint inside the Canvas at narrow aspect ratios.
    public void AppendHint(string hint)
    {
        if (!text) return;
        if (text is TextMeshProUGUI)
        {
            var rect = text.rectTransform;
            var minimum = rect.anchorMin;
            var maximum = rect.anchorMax;
            minimum.x = 0.05f;
            maximum.x = 0.95f;
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            var lower = rect.offsetMin;
            var upper = rect.offsetMax;
            lower.x = 0f;
            upper.x = 0f;
            rect.offsetMin = lower;
            rect.offsetMax = upper;
            if (!text.enableAutoSizing)
            {
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = Mathf.Min(18f, text.fontSize);
                text.enableAutoSizing = true;
            }
            text.enableWordWrapping = true;
            text.outlineColor = new Color32(24, 16, 10, 255);
            text.outlineWidth = 0.18f;
        }
        text.text += $"\n<size=70%><color=#FFFFFF>{hint}</color></size>";
    }

    public void Hide()
    {
        if (text) text.text = "";
        gameObject.SetActive(false);
    }

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

    public void HideToast()
    {
        if (!toastText) return;
        toastText.text = "";
        toastText.gameObject.SetActive(false);
    }

    TMP_Text EnsureToastText()
    {
        if (toastText) return toastText;
        if (!text) text = GetComponent<TMP_Text>();
        if (!text) return null;
        var parent = transform.parent;
        var go = new GameObject("ToastText");
        if (text is TextMeshProUGUI)
        {
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
