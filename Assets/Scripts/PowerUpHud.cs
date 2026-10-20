using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KineticRank.Logic;

// One panel per player beside the board, showing that player's Normal / Queen / Cleaner tokens.
// The side to move is bright and its selected token is outlined in gold; the other side is dimmed.
// Clicking a token selects it through PlayerController.Select, so rules and move admission stay there.
public class PowerUpHud : MonoBehaviour
{
    // Reference layout (1920 x 1080, scaled like the other HUD canvases); BoardFraming keeps the board clear of it
    public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;
    public const float PanelWidth = 320f, EdgeMargin = 36f, BoardGap = 24f;
    const float PanelHeight = 600f, SlotHeight = 140f, SlotGap = 14f, SlotTop = 126f;

    static readonly Color Ivory = new Color(0.953f, 0.910f, 0.824f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
    static readonly Color DarkWood = new Color(0.149f, 0.082f, 0.043f, 0.96f);
    static readonly Color SlotWood = new Color(0.26f, 0.16f, 0.09f, 0.95f);
    static readonly Color SelectedFill = new Color(0.99f, 0.91f, 0.66f);
    static readonly Color Ink = new Color(0.18f, 0.10f, 0.05f);
    static readonly Color RedTint = new Color(0.90f, 0.25f, 0.22f);
    static readonly Color BlueTint = new Color(0.30f, 0.55f, 0.95f);

    static readonly MoveKind[] Kinds = { MoveKind.Normal, MoveKind.Queen, MoveKind.Cleaner };
    static readonly string[] Names = { "Normal", "Queen", "Cleaner" };
    static readonly string[] Keys = { "N", "Q", "C" };
    static readonly string[] Effects = { "Plain token", "Flips the column", "Clears the column" };

    [SerializeField] Sprite panelSprite;
    PlayerController player;
    GameObject canvasObject;
    Font font;
    readonly Side[] sides = new Side[2];

    class Slot
    {
        public RectTransform root;
        public Image border, fill, icon;
        public Text letter, name, detail, key, tag;
        public Button button;
    }

    class Side
    {
        public bool red;
        public RectTransform root;
        public CanvasGroup group;
        public Image border;
        public Text header, subtitle, status;
        public readonly Slot[] slots = new Slot[3];
    }

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
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvasObject = new GameObject("PowerUpHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        sides[0] = BuildSide(true);
        sides[1] = BuildSide(false);
        Refresh();
    }

    Side BuildSide(bool red)
    {
        var side = new Side { red = red };
        var go = new GameObject(red ? "RedPanel" : "BluePanel", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasObject.transform, false);
        side.root = go.GetComponent<RectTransform>();
        side.group = go.GetComponent<CanvasGroup>();
        float x = red ? 0f : 1f;
        side.root.anchorMin = side.root.anchorMax = side.root.pivot = new Vector2(x, 0.5f);
        side.root.anchoredPosition = new Vector2(red ? EdgeMargin : -EdgeMargin, 0f);
        side.root.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // Gold frame behind the panel while it is this side's turn
        side.border = MakeImage(side.root, "TurnFrame", Gold);
        Place(side.border.rectTransform, -6f, -6f, PanelWidth + 12f, PanelHeight + 12f);

        var back = MakeImage(side.root, "Panel", DarkWood);
        if (panelSprite != null)
        {
            back.sprite = panelSprite;
            back.type = Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = 4f;
            back.color = Color.white;
        }
        // Catches clicks so they don't fall through to the board
        back.raycastTarget = true;
        Place(back.rectTransform, 0f, 0f, PanelWidth, PanelHeight);

        side.header = MakeText(side.root, "Header", 44, FontStyle.Bold, red ? RedTint : BlueTint, TextAnchor.UpperLeft);
        side.header.text = red ? "RED" : "BLUE";
        Place(side.header.rectTransform, 18f, 14f, PanelWidth - 36f, 52f);

        side.subtitle = MakeText(side.root, "Subtitle", 20, FontStyle.Normal, Ivory, TextAnchor.UpperLeft);
        Place(side.subtitle.rectTransform, 20f, 64f, PanelWidth - 36f, 26f);

        side.status = MakeText(side.root, "Status", 22, FontStyle.Bold, Gold, TextAnchor.UpperLeft);
        Place(side.status.rectTransform, 20f, 92f, PanelWidth - 36f, 28f);

        for (int i = 0; i < Kinds.Length; i++)
            side.slots[i] = BuildSlot(side, i);
        return side;
    }

    Slot BuildSlot(Side side, int index)
    {
        var slot = new Slot();
        float width = PanelWidth - 32f;
        var go = new GameObject(Names[index] + "Slot", typeof(RectTransform));
        go.transform.SetParent(side.root, false);
        slot.root = go.GetComponent<RectTransform>();
        Place(slot.root, 16f, SlotTop + index * (SlotHeight + SlotGap), width, SlotHeight);
        slot.root.pivot = new Vector2(0.5f, 0.5f);
        slot.root.anchoredPosition += new Vector2(width * 0.5f, -SlotHeight * 0.5f);

        slot.border = MakeImage(slot.root, "SelectedFrame", Gold);
        Place(slot.border.rectTransform, -5f, -5f, width + 10f, SlotHeight + 10f);
        slot.fill = MakeImage(slot.root, "Fill", SlotWood);
        slot.fill.raycastTarget = true;
        Place(slot.fill.rectTransform, 0f, 0f, width, SlotHeight);

        slot.icon = MakeImage(slot.root, "Icon", Color.white);
        Place(slot.icon.rectTransform, 14f, 30f, 80f, 80f);
        slot.letter = MakeText(slot.icon.rectTransform, "Letter", 44, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        slot.letter.text = index == 0 ? "" : Keys[index];
        slot.letter.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        Place(slot.letter.rectTransform, 0f, 0f, 80f, 80f);

        slot.name = MakeText(slot.root, "Name", 30, FontStyle.Bold, Ivory, TextAnchor.UpperLeft);
        slot.name.text = Names[index];
        Place(slot.name.rectTransform, 106f, 22f, width - 156f, 36f);
        slot.detail = MakeText(slot.root, "Detail", 19, FontStyle.Normal, Ivory, TextAnchor.UpperLeft);
        Place(slot.detail.rectTransform, 106f, 60f, width - 114f, 48f);

        // Keycap so the letter reads as "press N", not as part of the name
        var keycap = MakeImage(slot.root, "Keycap", new Color(0.12f, 0.07f, 0.03f, 0.9f));
        Place(keycap.rectTransform, width - 44f, 10f, 34f, 34f);
        slot.key = MakeText(keycap.rectTransform, "Key", 22, FontStyle.Bold, Ivory, TextAnchor.MiddleCenter);
        slot.key.text = Keys[index];
        Place(slot.key.rectTransform, 0f, 0f, 34f, 34f);

        slot.tag = MakeText(slot.root, "SelectedTag", 17, FontStyle.Bold, Ink, TextAnchor.LowerRight);
        slot.tag.text = "SELECTED";
        Place(slot.tag.rectTransform, width - 130f, SlotHeight - 32f, 120f, 24f);

        slot.button = go.AddComponent<Button>();
        slot.button.targetGraphic = slot.fill;
        slot.button.transition = Selectable.Transition.None;
        var kind = Kinds[index];
        slot.button.onClick.AddListener(() => { if (player != null) player.Select(kind); });
        return slot;
    }

    Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    // Top-left placement inside the parent, in reference pixels
    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    void Update() => Refresh();

    void Refresh()
    {
        if (canvasObject == null) return;
        bool visible = player != null && player.enabled;
        canvasObject.SetActive(visible);
        if (!visible) return;

        var manager = GameManager.Instance;
        bool resolving = manager != null && manager.IsResolvingMove;
        foreach (var side in sides) RefreshSide(side, resolving);
    }

    void RefreshSide(Side side, bool resolving)
    {
        bool toMove = player.IsRedTurn == side.red;
        bool human = IsHuman(side.red);
        bool canPick = toMove && human && !resolving;

        side.group.alpha = toMove ? 1f : 0.5f;
        side.border.enabled = toMove;
        side.subtitle.text = Subtitle(side.red);
        side.status.text = resolving && toMove ? "Moving..."
                         : toMove ? (human ? "Your turn: pick a token" : "Thinking...")
                         : "Waiting";

        Sprite sprite = TokenSprite(side.red);
        Color tint = side.red ? RedTint : BlueTint;
        for (int i = 0; i < Kinds.Length; i++)
        {
            var slot = side.slots[i];
            bool available = Kinds[i] == MoveKind.Normal
                || (Kinds[i] == MoveKind.Queen ? player.QueenAvailable(side.red) : player.CleanerAvailable(side.red));
            bool selected = toMove && player.SelectedKind == Kinds[i];

            slot.border.enabled = selected;
            slot.fill.color = selected ? SelectedFill : SlotWood;
            slot.root.localScale = selected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
            slot.tag.enabled = selected;

            slot.icon.sprite = sprite;
            slot.icon.color = sprite != null ? new Color(1f, 1f, 1f, available ? 1f : 0.25f)
                                             : new Color(tint.r, tint.g, tint.b, available ? 1f : 0.25f);
            slot.letter.enabled = available && i > 0;

            Color textColor = selected ? Ink : Ivory;
            if (!available) textColor = new Color(0.6f, 0.55f, 0.5f);
            slot.name.color = textColor;
            slot.detail.color = textColor;
            slot.key.color = available ? Ivory : textColor;
            slot.detail.text = !available ? "USED" : i == 0 ? $"{Effects[i]}\nUnlimited" : $"{Effects[i]}\nOnce per match";
            slot.button.interactable = canPick && available;
        }
    }

    bool IsHuman(bool red)
    {
        if (SoloSettings.Enabled) return red != SoloSettings.BotPlaysRed;
        if (PuzzleSettings.Enabled) return red == PuzzleSettings.AttackerIsRed;
        return true;
    }

    static string Subtitle(bool red)
    {
        if (SoloSettings.Enabled)
            return red == SoloSettings.BotPlaysRed ? $"Bot: {BotTiers.Title(SoloSettings.Tier)}" : "You";
        if (PuzzleSettings.Enabled)
            return red == PuzzleSettings.AttackerIsRed ? "You" : "Puzzle defender";
        return red ? "Player 1 (moves first)" : "Player 2";
    }

    Sprite TokenSprite(bool red)
    {
        var prefab = red ? player.redTokenPrefab : player.blueTokenPrefab;
        if (prefab == null) return null;
        var renderer = prefab.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }
}
