// File: Assets/Scripts/TraditionalMenuBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

// Adds a Traditional (no power-ups) button and an opponent panel to MainMenu at runtime, so the scene file stays untouched.
// Buttons are clones of PlayButton; every choice sets TraditionalSettings, then hands off to the existing Play/PlaySolo paths.
public class TraditionalMenuBootstrap : MonoBehaviour
{
    const float ButtonGap = 16f;
    const float PanelDrop = 70f;
    static readonly Vector2 ChoiceButtonGrow = new Vector2(1.25f, 1.2f);

    MainMenuController menu;
    GameObject mainButtons;
    GameObject choicePanel;
    GameObject summary;
    Button firstChoice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var owner = FindObjectOfType<MainMenuController>();
        if (!owner) return;

        // Back in the menu: Traditional is off until chosen again, so Play/Solo/Puzzle/Daily never inherit it
        TraditionalSettings.Clear();

        if (!owner.playButton || !owner.playButton.transform.parent) return;
        if (owner.GetComponent<TraditionalMenuBootstrap>()) return;

        owner.gameObject.AddComponent<TraditionalMenuBootstrap>().menu = owner;
    }

    // Built in Start, after every sceneLoaded hook, so the Solo button already exists
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

        // Choice panel: a copy of the button group (same layout and position) with its buttons removed
        choicePanel = Instantiate(mainButtons, group.parent);
        choicePanel.name = "TraditionalPanel";
        choicePanel.SetActive(false);
        for (int i = choicePanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = choicePanel.transform.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        // Traditional button sits right below Solo (or Play when there is no Solo button)
        var traditional = CloneButton(play, group, "TraditionalButton", "Traditional");
        traditional.transform.SetSiblingIndex(InsertIndex(group, play));
        traditional.onClick.AddListener(OpenChoices);

        if (choicePanel.transform is RectTransform panelRect)
            panelRect.anchoredPosition -= new Vector2(0f, PanelDrop);
        AddHeading(play, choicePanel.transform, "Traditional: no power-ups, casual");

        var hotSeat = AddChoice(play, "TwoPlayersButton", "<b>Two players</b>\n<size=60%>Same screen, take turns</size>");
        hotSeat.onClick.AddListener(ChooseHotSeat);
        firstChoice = hotSeat;

        foreach (BotTier tier in System.Enum.GetValues(typeof(BotTier)))
        {
            var picked = tier;
            AddChoice(play, picked + "Button", TierLabel(picked)).onClick.AddListener(() => ChooseBot(picked));
        }

        CloneButton(play, choicePanel.transform, "BackButton", "Back").onClick.AddListener(CloseChoices);

        KeepClearOfSummary(group as RectTransform);
    }

    // Right after Solo if present, otherwise right after Play
    static int InsertIndex(Transform group, Button play)
    {
        int index = play.transform.GetSiblingIndex() + 1;
        foreach (Transform child in group)
            if (child.name.StartsWith("SoloButton"))
                index = Mathf.Max(index, child.GetSiblingIndex() + 1);
        return Mathf.Min(index, group.childCount - 1);
    }

    Button AddChoice(Button style, string name, string label)
    {
        var button = CloneButton(style, choicePanel.transform, name, label);
        if (button.transform is RectTransform rect)
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * ChoiceButtonGrow.x, rect.sizeDelta.y * ChoiceButtonGrow.y);
        return button;
    }

    // "Oak · Medium" with "Thinks a few moves ahead" underneath
    static string TierLabel(BotTier tier) =>
        $"<b>{tier}</b>  ·  {BotTiers.Difficulty(tier)}\n<size=60%>{BotTiers.Blurb(tier)}</size>";

    // Plain text line above the choices, in the buttons' font
    static void AddHeading(Button style, Transform parent, string content)
    {
        var go = new GameObject("TraditionalHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        var source = style.GetComponentInChildren<TMP_Text>(true);
        if (source) text.font = source.font;
        text.text = content;
        text.fontSize = 30;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        if (style.transform is RectTransform styleRect)
            ((RectTransform)go.transform).sizeDelta = new Vector2(styleRect.sizeDelta.x * ChoiceButtonGrow.x, 44f);
        go.transform.SetAsFirstSibling();
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

    // The extra Traditional button makes the group taller; push RankedSummary down if they would touch
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
        if (choicePanel && choicePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseChoices();
    }

    void ChooseHotSeat()
    {
        if (SceneFader.IsFading) return;
        menu.Play();
        // Play clears Solo and Puzzle but not Traditional; set it after so nothing else can reset it
        TraditionalSettings.StartTraditional();
    }

    void ChooseBot(BotTier tier)
    {
        if (SceneFader.IsFading) return;
        menu.PlaySolo(tier);
        TraditionalSettings.StartTraditional();
    }

    void OpenChoices()
    {
        if (SceneFader.IsFading) return;

        // Stop the menu's key handling so Enter can't start a ranked match behind the panel
        menu.enabled = false;
        mainButtons.SetActive(false);
        if (summary) summary.SetActive(false);
        choicePanel.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(firstChoice.gameObject);
    }

    void CloseChoices()
    {
        choicePanel.SetActive(false);
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
