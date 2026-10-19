using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Display-only match information; keyboard and board input stay with PlayerController.
public class PowerUpHud : MonoBehaviour
{
    [SerializeField] Sprite panelSprite;
    PlayerController player;
    GameObject canvasObject;
    RectTransform panel;
    Text heading, redUses, blueUses, hint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc == null || FindObjectOfType<PowerUpHud>() != null) return;
        var go = new GameObject("PowerUpHud");
        SceneManager.MoveGameObjectToScene(go, pc.gameObject.scene);
        go.AddComponent<PowerUpHud>().player = pc;
    }

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        canvasObject = new GameObject("PowerUpHudCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var panelObject = new GameObject("PowerUpPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = Vector2.zero;
        panel.pivot = Vector2.zero;
        var image = panelObject.GetComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 4f;
        image.color = panelSprite != null ? Color.white : new Color(0.149f, 0.082f, 0.043f, 0.96f);
        image.raycastTarget = false;

        Color ivory = new Color(0.953f, 0.910f, 0.824f);
        heading = MakeText("Turn", ivory, FontStyle.Bold);
        redUses = MakeText("RedUses", ivory, FontStyle.Normal);
        blueUses = MakeText("BlueUses", ivory, FontStyle.Normal);
        hint = MakeText("SelectionHint", new Color(0.886f, 0.678f, 0.447f), FontStyle.Normal);
        hint.text = "Select: N Normal / Q Queen / C Cleaner";
        Refresh();
    }

    Text MakeText(string name, Color color, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(panel, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    void Update() => Refresh();

    void Refresh()
    {
        if (panel == null) return;
        Rect safe = Screen.safeArea;
        float width = Mathf.Min(620f, Mathf.Max(0f, safe.width - 20f));
        bool visible = player != null && player.enabled && width >= 1f;
        panel.gameObject.SetActive(visible);
        if (!visible) return;

        float scale = Mathf.Clamp(width / 620f, 0.5f, 1f);
        panel.anchoredPosition = new Vector2(safe.x + (safe.width - width) * 0.5f, safe.y + 10f);
        panel.sizeDelta = new Vector2(width, 88f * scale);
        float innerWidth = width - 28f * scale;
        float gap = 12f * scale;
        float columnWidth = (innerWidth - gap) * 0.5f;
        Place(heading, 14f * scale, 6f * scale, innerWidth, 25f * scale, 18f * scale);
        Place(redUses, 14f * scale, 32f * scale, columnWidth, 22f * scale, 14f * scale);
        Place(blueUses, 14f * scale + columnWidth + gap, 32f * scale, columnWidth, 22f * scale, 14f * scale);
        Place(hint, 14f * scale, 55f * scale, innerWidth, 23f * scale, 14f * scale);
        var manager = GameManager.Instance;
        string status = manager != null && manager.IsResolvingMove
            ? "Resolving move..."
            : $"{(player.IsRedTurn ? "RED" : "BLUE")} to move";
        heading.text = $"{status}   |   {player.CurrentPieceName}";
        redUses.text = Uses(true);
        blueUses.text = Uses(false);
    }

    static void Place(Text text, float x, float y, float width, float height, float fontSize)
    {
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        text.fontSize = Mathf.RoundToInt(fontSize);
    }

    string Uses(bool red)
    {
        string queen = player.QueenAvailable(red) ? "ready" : "used";
        string cleaner = player.CleanerAvailable(red) ? "ready" : "used";
        return $"{(red ? "RED" : "BLUE")}   Queen: {queen} | Cleaner: {cleaner}";
    }
}
