using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// Presentation-only rules panel for the Match scene. It opens once on the first match (own PlayerPrefs flag,
// separate from SaveManager) and again on demand with F1 or '?'. H is already the rating HUD toggle, so it is not
// used here. While open it disables PlayerController so no move can start behind it; rules, move admission and
// rank data are never touched.
public class HowToPlayOverlay : MonoBehaviour
{
    public const string SeenKey = "KineticRank.HowToPlaySeen";

    [SerializeField] bool showCornerHint = true;
    [SerializeField] int canvasSortingOrder = 100;

    const string BodyText =
        "<size=46><b>How to play</b></size>\n\n" +
        "<b>Goal:</b>  get four of your colour in a row, across, up and down, or diagonally.\n" +
        "<b>Your turn:</b>  pick a token, then click a column. It falls to the lowest empty square. RED goes first.\n\n" +
        "<size=34><b>Your three tokens</b></size>   <i>click one in your panel, or press its key</i>\n" +
        "<b>N   Normal</b>  \u2013  a plain token of your colour. Use it as often as you like.\n" +
        "<b>Q   Queen</b>  \u2013  once per match. It lands as your colour, then every other token in that column switches colour.\n" +
        "<b>C   Cleaner</b>  \u2013  once per match. It empties the whole column, and the Cleaner disappears too.\n" +
        "Your selected token has a gold outline in your panel and floats above the column you point at. " +
        "After a Queen or Cleaner you are back on Normal.\n\n" +
        "<b>Full column?</b>  You can still play there. The bottom token drops out, the rest slide down, and yours lands on top.\n" +
        "<b>Draw:</b>  if one move gives both colours four in a row, nobody wins.\n\n" +
        "Enter: rematch (after match)      H: ratings      F1 or ?: this help\n" +
        "<i>Click anywhere to close.</i>";

    GameObject panel;
    PlayerController lockedPlayer;
    bool playerWasEnabled;
    bool restorePending;
    int openedFrame = -1;

    public static HowToPlayOverlay Instance { get; private set; }

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Self-attaches to any scene that has a BoardManager, so no scene edits are needed
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    static void Attach()
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;
        foreach (var existing in FindObjectsOfType<HowToPlayOverlay>())
            if (existing.gameObject.scene == board.gameObject.scene) return;

        var go = new GameObject("HowToPlayOverlay");
        SceneManager.MoveGameObjectToScene(go, board.gameObject.scene);
        go.AddComponent<HowToPlayOverlay>();
    }

    void Start()
    {
        Build();
        // Batch test runs must neither block input nor consume the player's first-time flag
        if (Application.isBatchMode) return;
        if (PlayerPrefs.GetInt(SeenKey, 0) == 0)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Open();
        }
    }

    void Update()
    {
        bool toggle = Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Slash);
        if (IsOpen)
        {
            if (Time.frameCount != openedFrame
                && (toggle || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape)))
                Close();
        }
        else if (toggle) Open();
    }

    void Open()
    {
        if (panel == null || IsOpen) return;
        var manager = GameManager.Instance;
        var player = manager != null && manager.player != null ? manager.player : FindObjectOfType<PlayerController>();
        // A reopen before the pending restore keeps the original enabled state
        if (!restorePending || player != lockedPlayer) playerWasEnabled = player != null && player.enabled;
        restorePending = false;
        lockedPlayer = player;
        if (lockedPlayer != null) lockedPlayer.enabled = false;
        openedFrame = Time.frameCount;
        panel.SetActive(true);
    }

    void Close()
    {
        panel.SetActive(false);
        restorePending = true;
        StartCoroutine(RestorePlayerNextFrame());
    }

    // One frame later, so the closing click is not also read as a drop
    IEnumerator RestorePlayerNextFrame()
    {
        yield return null;
        if (!restorePending || IsOpen) yield break;
        restorePending = false;
        var manager = GameManager.Instance;
        if (lockedPlayer != null && playerWasEnabled && (manager == null || !manager.IsMatchOver))
            lockedPlayer.enabled = true;
        lockedPlayer = null;
    }

    void Build()
    {
        var canvasGo = new GameObject("HowToPlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        panel = new GameObject("HowToPlayPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        Stretch(panel.GetComponent<RectTransform>(), 0f);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(1240f, 900f);
        box.GetComponent<Image>().color = new Color(0.22f, 0.14f, 0.08f, 0.96f);

        var body = MakeText(box.transform, "Text", font, BodyText, 29, TextAnchor.UpperLeft);
        body.lineSpacing = 1.1f;
        Stretch(body.rectTransform, 48f);

        panel.SetActive(false);

        if (!showCornerHint) return;
        var hint = MakeText(canvasGo.transform, "HowToPlayHint", font, "F1  How to play", 22, TextAnchor.LowerLeft);
        hint.color = new Color(1f, 1f, 1f, 0.55f);
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = Vector2.zero;
        hintRect.anchoredPosition = new Vector2(18f, 14f);
        hintRect.sizeDelta = new Vector2(400f, 40f);
    }

    static Text MakeText(Transform parent, string name, Font font, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
