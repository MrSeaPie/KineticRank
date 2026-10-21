using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

// Builds wooden Solo entries from the scene's existing Play button.
public class SoloMenuBootstrap : MonoBehaviour
{
    const float ButtonGap = 16f;
    const float TierPanelDrop = 70f;
    static readonly Vector2 TierButtonGrow = new Vector2(1.25f, 1.2f);
    MainMenuController menu;
    GameObject mainButtons;
    GameObject tierPanel;
    GameObject summary;
    Button firstTier;

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
        if (FindObjectOfType<SoloMenuPanel>(true)) return;
        if (owner.GetComponent<SoloMenuBootstrap>()) return;
        owner.gameObject.AddComponent<SoloMenuBootstrap>().Build(owner);
    }

    void Build(MainMenuController owner)
    {
        menu = owner;
        var play = menu.playButton;
        var group = play.transform.parent;
        mainButtons = group.gameObject;
        if (menu.rankedSummary) summary = menu.rankedSummary.gameObject;
        if (!group.GetComponent<LayoutGroup>())
        {
            var layout = mainButtons.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ButtonGap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // Clone before adding entries so the opponent panel contains no main-menu actions.
        tierPanel = Instantiate(mainButtons, group.parent);
        tierPanel.name = "SoloTierPanel";
        tierPanel.SetActive(false);
        for (int i = tierPanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = tierPanel.transform.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        var ranked = CloneButton(play, group, "RankedSoloButton", "Ranked Solo");
        ranked.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
        ranked.onClick.AddListener(StartRanked);
        var solo = CloneButton(play, group, "SoloButton", "Casual Solo");
        solo.transform.SetSiblingIndex(ranked.transform.GetSiblingIndex() + 1);
        solo.onClick.AddListener(OpenTiers);

        if (tierPanel.transform is RectTransform panelRect)
            panelRect.anchoredPosition -= new Vector2(0f, TierPanelDrop);
        AddHeading(play, tierPanel.transform, "Choose your opponent");
        var tiers = (BotTier[])System.Enum.GetValues(typeof(BotTier));
        var tierButtons = new Button[tiers.Length];
        for (int i = 0; i < tiers.Length; i++)
        {
            tierButtons[i] = CloneButton(play, tierPanel.transform, tiers[i] + "Button", TierLabel(tiers[i]));
            if (tierButtons[i].transform is RectTransform rect)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x * TierButtonGrow.x, rect.sizeDelta.y * TierButtonGrow.y);
        }
        firstTier = tierButtons[0];
        CloneButton(play, tierPanel.transform, "BackButton", "Back").onClick.AddListener(CloseTiers);
        var panel = tierPanel.AddComponent<SoloMenuPanel>();
        panel.menu = menu;
        panel.tierButtons = tierButtons;
        KeepClearOfSummary(group as RectTransform);
    }

    void StartRanked()
    {
        if (SceneFader.IsFading || !menu || !menu.enabled) return;
        menu.PlayRankedSolo(PlayerRankStore.GetProfile().rating);
    }

    static string TierLabel(BotTier tier) =>
        $"<b>{tier}</b>  ·  {BotTiers.Difficulty(tier)}\n<size=60%>{BotTiers.Blurb(tier)}</size>";

    static void AddHeading(Button style, Transform parent, string content)
    {
        var go = new GameObject("TierHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
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
            ((RectTransform)go.transform).sizeDelta = new Vector2(styleRect.sizeDelta.x * TierButtonGrow.x, 44f);
        go.transform.SetAsFirstSibling();
    }

    static Button CloneButton(Button source, Transform parent, string name, string label)
    {
        var button = Instantiate(source, parent);
        button.name = name;
        button.onClick = new Button.ButtonClickedEvent();
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text) text.text = label;
        return button;
    }

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
        if (tierPanel && tierPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseTiers();
    }

    void OpenTiers()
    {
        if (SceneFader.IsFading) return;
        menu.enabled = false;
        mainButtons.SetActive(false);
        if (summary) summary.SetActive(false);
        tierPanel.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(firstTier.gameObject);
    }

    void CloseTiers()
    {
        tierPanel.SetActive(false);
        mainButtons.SetActive(true);
        if (summary) summary.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(EnableMenuNextFrame());
    }

    IEnumerator EnableMenuNextFrame()
    {
        yield return null;
        if (menu) menu.enabled = true;
    }
}
