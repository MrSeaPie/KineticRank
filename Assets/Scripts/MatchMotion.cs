using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using KineticRank.Logic;

// Presentation-only motion for the Match scene: a translucent column hover highlight with the selected token
// floating above it, and a pulse on the result banner when the match ends. It only reads game state and never
// touches move admission or rules.
public class MatchMotion : MonoBehaviour
{
    [Header("Column hover")]
    [SerializeField] float hoverAlpha = 0.14f;
    [SerializeField] float powerUpHoverAlpha = 0.32f;
    [SerializeField] int hoverSortingOrder = 5;

    [Header("Selected token preview above the column")]
    [SerializeField] float previewAlpha = 0.9f;
    [SerializeField] float previewBob = 0.06f;
    [SerializeField] int previewSortingOrder = 30;

    [Header("Win pulse (set scale or duration to 0 to disable)")]
    [SerializeField] float pulseScale = 0.12f;
    [SerializeField] float pulseDuration = 0.35f;
    [SerializeField] int pulseCount = 2;

    static readonly Color QueenTint = new Color(1f, 0.78f, 0.25f);
    static readonly Color CleanerTint = Color.white;

    SpriteRenderer highlight;
    Transform preview;
    SpriteRenderer previewRenderer;
    TextMeshPro previewLetter;
    bool wasMatchOver;
    Transform pulseTarget;
    Vector3 pulseBaseScale;
    float pulseStart = -1f;

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
        foreach (var existing in FindObjectsOfType<MatchMotion>())
            if (existing.gameObject.scene == board.gameObject.scene) return;

        var go = new GameObject("MatchMotion");
        SceneManager.MoveGameObjectToScene(go, board.gameObject.scene);
        go.AddComponent<MatchMotion>();
    }

    void Awake()
    {
        var tex = Texture2D.whiteTexture;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);

        var go = new GameObject("ColumnHoverHighlight");
        go.transform.SetParent(transform, false);
        highlight = go.AddComponent<SpriteRenderer>();
        highlight.sprite = sprite;
        highlight.sortingOrder = hoverSortingOrder;
        highlight.enabled = false;

        preview = new GameObject("SelectedTokenPreview").transform;
        preview.SetParent(transform, false);
        previewRenderer = preview.gameObject.AddComponent<SpriteRenderer>();
        previewRenderer.sortingOrder = previewSortingOrder;
        previewRenderer.enabled = false;

        var letterGo = new GameObject("Letter");
        letterGo.transform.SetParent(preview, false);
        previewLetter = letterGo.AddComponent<TextMeshPro>();
        previewLetter.fontSize = 5f;
        previewLetter.fontStyle = FontStyles.Bold;
        previewLetter.alignment = TextAlignmentOptions.Center;
        previewLetter.color = Color.white;
        previewLetter.rectTransform.sizeDelta = Vector2.one;
        previewLetter.sortingOrder = previewSortingOrder + 1;
        previewLetter.text = "";
        previewLetter.enabled = false;
    }

    void Update()
    {
        var manager = GameManager.Instance;
        UpdateHover(manager);
        UpdatePulse(manager);
    }

    // Same column mapping as a click in PlayerController; hidden whenever a click would be ignored
    void UpdateHover(GameManager manager)
    {
        bool show = false;
        if (manager != null && manager.board != null && manager.player != null && manager.player.enabled
            && !manager.IsInputLocked && !manager.IsMatchOver && manager.player.IsHumanTurn && Camera.main != null
            && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            var world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int col = BoardManager.WorldToColumn(world.x);
            int rows = manager.board.rows;
            if (col >= 0 && col < manager.board.columns)
            {
                var bottom = BoardManager.GridToWorld(col, 0);
                highlight.transform.position = new Vector3(bottom.x, bottom.y - 0.5f + rows * 0.5f, 0f);
                highlight.transform.localScale = new Vector3(1f, rows, 1f);
                // A power-up acts on the whole column, so its highlight is stronger and not team-coloured
                var kind = manager.player.SelectedKind;
                Color tint = kind == MoveKind.Queen ? QueenTint
                           : kind == MoveKind.Cleaner ? CleanerTint
                           : manager.player.IsRedTurn ? Color.red : Color.cyan;
                tint.a = kind == MoveKind.Normal ? hoverAlpha : powerUpHoverAlpha;
                highlight.color = tint;
                UpdatePreview(manager.player, bottom.x, rows);
                show = true;
            }
        }
        highlight.enabled = show;
        previewRenderer.enabled = show && previewRenderer.sprite != null;
        previewLetter.enabled = previewRenderer.enabled && previewLetter.text.Length > 0;
    }

    // The token that a click would drop, floating above the hovered column
    void UpdatePreview(PlayerController player, float x, int rows)
    {
        var prefab = player.IsRedTurn ? player.redTokenPrefab : player.blueTokenPrefab;
        var source = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
        previewRenderer.sprite = source != null ? source.sprite : null;
        previewRenderer.color = new Color(1f, 1f, 1f, previewAlpha);
        var kind = player.SelectedKind;
        previewLetter.text = kind == MoveKind.Queen ? "Q" : kind == MoveKind.Cleaner ? "C" : "";
        float bob = previewBob * Mathf.Sin(Time.unscaledTime * 5f);
        preview.position = new Vector3(x, rows + 0.72f + bob, 0f);
    }

    void UpdatePulse(GameManager manager)
    {
        bool over = manager != null && manager.IsMatchOver;
        if (over && !wasMatchOver) StartPulse(manager);
        wasMatchOver = over;

        if (pulseTarget == null || pulseStart < 0f) return;
        float total = pulseDuration * Mathf.Max(1, pulseCount);
        float t = Time.unscaledTime - pulseStart;
        if (pulseDuration <= 0f || t >= total) { StopPulse(); return; }

        float phase = (t % pulseDuration) / pulseDuration;
        pulseTarget.localScale = pulseBaseScale * (1f + pulseScale * Mathf.Sin(phase * Mathf.PI));
    }

    void StartPulse(GameManager manager)
    {
        StopPulse();
        if (manager.winBanner == null || pulseScale <= 0f || pulseDuration <= 0f) return;
        pulseTarget = manager.winBanner.transform;
        pulseBaseScale = pulseTarget.localScale;
        pulseStart = Time.unscaledTime;
    }

    // Always restores the banner's original scale
    void StopPulse()
    {
        if (pulseTarget != null) pulseTarget.localScale = pulseBaseScale;
        pulseTarget = null;
        pulseStart = -1f;
    }

    void OnDestroy()
    {
        StopPulse();
        if (highlight != null && highlight.sprite != null) Destroy(highlight.sprite);
    }
}
