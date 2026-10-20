using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation-only: keeps the open MainMenu panel between the title and the bottom of the screen. That is the main
// buttons (with the ranked summary under them) or a Solo / Puzzles / Tutorial list. The panel is centred in that space
// and shrunk only when it would not fit, so short or wide windows no longer push buttons over the title.
// Panels are the button group and its clones (siblings with a LayoutGroup), so no scene edits are needed.
public class MenuLayoutFit : MonoBehaviour
{
    // Canvas units kept free under the title, above the bottom edge, and between the buttons and the summary
    const float TitleGap = 24f;
    const float BottomMargin = 24f;
    const float SummaryGap = 16f;

    MainMenuController menu;
    RectTransform area, mainGroup, title, summary;
    RectTransform lastPanel;
    Vector2 lastScreen;
    float lastHeight = -1f;
    readonly Vector3[] corners = new Vector3[4];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var owner = FindObjectOfType<MainMenuController>();
        if (!owner || !owner.playButton || !owner.playButton.transform.parent) return;
        if (owner.GetComponent<MenuLayoutFit>()) return;
        owner.gameObject.AddComponent<MenuLayoutFit>().menu = owner;
    }

    void Start()
    {
        mainGroup = menu.playButton.transform.parent as RectTransform;
        area = mainGroup ? mainGroup.parent as RectTransform : null;
        if (!area) return;
        title = area.Find("Title") as RectTransform;
        // The summary only moves with the buttons when it shares their parent
        if (menu.rankedSummary && menu.rankedSummary.transform.parent == area) summary = menu.rankedSummary.rectTransform;
    }

    // Refits only when the open panel, its height or the window size changes
    void LateUpdate()
    {
        if (!area) return;
        var panel = OpenPanel();
        if (!panel) return;

        var screen = new Vector2(Screen.width, Screen.height);
        if (panel == lastPanel && screen == lastScreen && Mathf.Approximately(panel.rect.height, lastHeight)) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Fit(panel);
        lastPanel = panel;
        lastScreen = screen;
        lastHeight = panel.rect.height;
    }

    RectTransform OpenPanel()
    {
        if (mainGroup.gameObject.activeInHierarchy) return mainGroup;
        foreach (Transform child in area)
        {
            if (!child.gameObject.activeSelf || !child.GetComponent<LayoutGroup>()) continue;
            if (child.GetComponentInChildren<Button>() && child is RectTransform rect) return rect;
        }
        return null;
    }

    void Fit(RectTransform panel)
    {
        float height = panel.rect.height;
        bool withSummary = panel == mainGroup && summary && summary.gameObject.activeSelf;
        float total = height + (withSummary ? SummaryGap + summary.rect.height : 0f);
        if (total <= 0f) return;

        // Area-local space: y grows upwards from the area's pivot
        Rect bounds = area.rect;
        float top = bounds.yMax - TitleGap;
        if (title && title.gameObject.activeSelf) top = Mathf.Min(top, LowestEdge(title) - TitleGap);
        float bottom = bounds.yMin + BottomMargin;
        float space = Mathf.Max(1f, top - bottom);

        float scale = Mathf.Min(1f, space / total);
        float stackTop = top - (space - total * scale) * 0.5f;

        SetTop(panel, stackTop, scale);
        if (withSummary) SetTop(summary, stackTop - (height + SummaryGap) * scale, scale);
    }

    // Scales the rect and puts its top edge at y (area-local), keeping its x
    static void SetTop(RectTransform rect, float y, float scale)
    {
        rect.localScale = new Vector3(scale, scale, 1f);
        var p = rect.localPosition;
        rect.localPosition = new Vector3(p.x, y - rect.rect.height * (1f - rect.pivot.y) * scale, p.z);
    }

    float LowestEdge(RectTransform rect)
    {
        rect.GetWorldCorners(corners);
        return area.InverseTransformPoint(corners[0]).y;
    }
}
