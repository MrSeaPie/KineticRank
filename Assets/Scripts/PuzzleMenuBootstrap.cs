// File: Assets/Scripts/PuzzleMenuBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

// Adds a Puzzles button and a puzzle list panel to MainMenu at runtime, so the scene file stays untouched.
// Buttons are clones of PlayButton; picking a puzzle hands off to MainMenuController.PlayPuzzle.
public class PuzzleMenuBootstrap : MonoBehaviour
{
    const float ButtonGap = 16f;
    const float ListPanelDrop = 70f;
    const float CellGap = 12f;
    const int Columns = 2;
    const float CellHeightScale = 1f;
    const float MinLabelSize = 16f;
    const float LabelSidePadding = 26f;

    MainMenuController menu;
    GameObject mainButtons;
    GameObject listPanel;
    GameObject summary;
    TMP_Text heading;
    Button[] entryButtons;
    Button firstEntry;

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
        if (owner.GetComponent<PuzzleMenuBootstrap>()) return;

        // No puzzles registered: no button at all
        if (PuzzleCatalog.All.Count == 0) return;

        owner.gameObject.AddComponent<PuzzleMenuBootstrap>().menu = owner;
    }

    // Built in Start, after every sceneLoaded hook, so the Solo and How-to-play buttons already exist
    void Start()
    {
        if (!menu || !menu.playButton || !menu.playButton.transform.parent)
        {
            Destroy(this);
            return;
        }
        Build();
    }

    void Build()
    {
        var play = menu.playButton;
        var group = play.transform.parent;
        mainButtons = group.gameObject;
        if (menu.rankedSummary) summary = menu.rankedSummary.gameObject;

        // List panel: a copy of the button group (same layout and position) with its buttons removed
        listPanel = Instantiate(mainButtons, group.parent);
        listPanel.name = "PuzzleListPanel";
        listPanel.SetActive(false);
        for (int i = listPanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = listPanel.transform.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        // Puzzles button sits below Solo and How to play
        var puzzles = CloneButton(play, group, "PuzzlesButton", "Puzzles");
        puzzles.transform.SetSiblingIndex(InsertIndex(group, play));
        puzzles.onClick.AddListener(OpenList);

        if (listPanel.transform is RectTransform panelRect)
            panelRect.anchoredPosition -= new Vector2(0f, ListPanelDrop);

        // Two columns of full-height buttons; MenuLayoutFit shrinks the panel if a window is too short for it
        var cell = play.transform is RectTransform playRect
            ? new Vector2(playRect.sizeDelta.x, playRect.sizeDelta.y * CellHeightScale)
            : new Vector2(320f, 64f);
        float listWidth = cell.x * Columns + CellGap * (Columns - 1);

        var list = new GameObject("PuzzleList", typeof(RectTransform), typeof(GridLayoutGroup));
        list.transform.SetParent(listPanel.transform, false);
        var grid = list.GetComponent<GridLayoutGroup>();
        grid.cellSize = cell;
        grid.spacing = new Vector2(CellGap, CellGap);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.childAlignment = TextAnchor.UpperCenter;

        var all = PuzzleCatalog.All;
        int rows = (all.Count + Columns - 1) / Columns;
        ((RectTransform)list.transform).sizeDelta =
            new Vector2(listWidth, rows * cell.y + CellGap * Mathf.Max(0, rows - 1));

        entryButtons = new Button[all.Count];
        for (int i = 0; i < all.Count; i++)
        {
            var entry = all[i];
            entryButtons[i] = CloneButton(play, list.transform, "Puzzle_" + entry.Id, "");
            entryButtons[i].onClick.AddListener(() => Choose(entry));
            // Long titles ("Direct Horizontal Four") shrink to fit the cell instead of spilling out of the button
            var label = entryButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = MinLabelSize;
                // The label spans the whole button, rounded ends included; keep the text on the flat wood
                label.margin = new Vector4(LabelSidePadding, 0f, LabelSidePadding, 0f);
            }
        }
        firstEntry = entryButtons[0];

        heading = AddHeading(play, listPanel.transform, listWidth);
        CloneButton(play, listPanel.transform, "BackButton", "Back").onClick.AddListener(CloseList);
        RefreshLabels();

        KeepClearOfSummary(group as RectTransform);
    }

    // Right after the last of Play, Solo and How to play, whichever are present
    static int InsertIndex(Transform group, Button play)
    {
        int index = play.transform.GetSiblingIndex() + 1;
        foreach (Transform child in group)
        {
            var name = child.name;
            if (name.StartsWith("SoloButton") || name.Contains("HowTo"))
                index = Mathf.Max(index, child.GetSiblingIndex() + 1);
        }
        return Mathf.Min(index, group.childCount - 1);
    }

    // Labels are rebuilt on every open so solved flags stay current
    void RefreshLabels()
    {
        var all = PuzzleCatalog.All;
        int solved = 0;
        for (int i = 0; i < entryButtons.Length && i < all.Count; i++)
        {
            bool done = PuzzleProgress.IsSolved(all[i].Id);
            if (done) solved++;
            var text = entryButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (text) text.text = EntryLabel(all[i], done);
        }
        if (heading) heading.text = $"Choose a puzzle  ({solved}/{entryButtons.Length} solved)";
    }

    // "Finish the line" with "Win in 1" or "Solved" underneath
    static string EntryLabel(PuzzleEntry entry, bool solved) =>
        $"<b>{entry.Title}</b>\n<size=60%>{(solved ? "Solved" : $"Win in {entry.MovesN}")}</size>";

    // Plain text line above the puzzle list, in the buttons' font
    static TMP_Text AddHeading(Button style, Transform parent, float width)
    {
        var go = new GameObject("PuzzleHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        var source = style.GetComponentInChildren<TMP_Text>(true);
        if (source) text.font = source.font;
        text.fontSize = 30;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        ((RectTransform)go.transform).sizeDelta = new Vector2(width, 44f);
        go.transform.SetAsFirstSibling();
        return text;
    }

    static Button CloneButton(Button source, Transform parent, string name, string label)
    {
        var button = Instantiate(source, parent);
        button.name = name;
        // Fresh event so no Play listener carries over from the source
        button.onClick = new Button.ButtonClickedEvent();
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text) text.text = label;
        return button;
    }

    // The extra Puzzles button makes the group taller; push RankedSummary down if they would touch
    void KeepClearOfSummary(RectTransform group)
    {
        if (!summary || !group) return;
        var summaryRect = summary.transform as RectTransform;
        if (!summaryRect || !summaryRect.parent) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(group);
        var corners = new Vector3[4];
        float lowest = float.MaxValue;
        foreach (Transform child in group)
        {
            var rect = child as RectTransform;
            if (!rect || !child.gameObject.activeSelf) continue;
            rect.GetWorldCorners(corners);
            lowest = Mathf.Min(lowest, corners[0].y);
        }
        if (lowest == float.MaxValue) return;

        float scale = summaryRect.parent.lossyScale.y;
        if (scale <= 0f) return;
        summaryRect.GetWorldCorners(corners);
        float overlap = corners[1].y - (lowest - ButtonGap * scale);
        if (overlap > 0f) summaryRect.anchoredPosition -= new Vector2(0f, overlap / scale);
    }

    void Update()
    {
        if (SceneFader.IsFading) return;
        if (listPanel && listPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseList();
    }

    void Choose(PuzzleEntry entry)
    {
        if (SceneFader.IsFading || entry == null) return;
        menu.PlayPuzzle(entry);
    }

    void OpenList()
    {
        if (SceneFader.IsFading) return;

        // Stop the menu's key handling so Enter can't start a ranked match behind the panel
        menu.enabled = false;
        RefreshLabels();
        mainButtons.SetActive(false);
        if (summary) summary.SetActive(false);
        listPanel.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(firstEntry.gameObject);
    }

    void CloseList()
    {
        listPanel.SetActive(false);
        mainButtons.SetActive(true);
        if (summary) summary.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(EnableMenuNextFrame());
    }

    // One frame later, so the key that closed the panel isn't also read by the menu
    IEnumerator EnableMenuNextFrame()
    {
        yield return null;
        if (menu) menu.enabled = true;
    }
}
