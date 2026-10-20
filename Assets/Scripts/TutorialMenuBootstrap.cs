// File: Assets/Scripts/TutorialMenuBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

// Adds a Tutorial button and a step list panel to MainMenu at runtime, so the scene file stays untouched.
// Steps come from TutorialPack and play through MainMenuController.PlayPuzzle like any other puzzle.
public class TutorialMenuBootstrap : MonoBehaviour
{
    const float ListPanelDrop = 70f;
    const float SummaryGap = 16f;

    MainMenuController menu;
    GameObject mainButtons;
    GameObject listPanel;
    GameObject summary;
    Button tutorialButton;
    Button[] stepButtons;
    Button firstStep;

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
        if (owner.GetComponent<TutorialMenuBootstrap>()) return;

        // No tutorial steps: no button at all
        if (TutorialPack.Steps.Count == 0) return;

        owner.gameObject.AddComponent<TutorialMenuBootstrap>().menu = owner;
    }

    // Waits one frame so the Solo and Puzzles buttons (built in their own Start) already exist
    IEnumerator Start()
    {
        yield return null;
        if (!menu || !menu.playButton || !menu.playButton.transform.parent)
        {
            Destroy(this);
            yield break;
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
        listPanel.name = "TutorialListPanel";
        listPanel.SetActive(false);
        for (int i = listPanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = listPanel.transform.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        // Tutorial button sits right below Puzzles (or below Solo / How to play without it)
        tutorialButton = CloneButton(play, group, "TutorialButton", "Tutorial");
        tutorialButton.transform.SetSiblingIndex(InsertIndex(group, play));
        tutorialButton.onClick.AddListener(OpenList);

        if (listPanel.transform is RectTransform panelRect)
            panelRect.anchoredPosition -= new Vector2(0f, ListPanelDrop);
        AddHeading(play, listPanel.transform, "Learn the moves");

        // One column: four steps fit without the puzzle grid
        var steps = TutorialPack.Steps;
        stepButtons = new Button[steps.Count];
        for (int i = 0; i < steps.Count; i++)
        {
            var def = steps[i];
            var entry = new PuzzleEntry(def.Id, def.Title, def.Hint, def.Start, def.MovesN);
            stepButtons[i] = CloneButton(play, listPanel.transform, "TutorialStep" + (i + 1), StepLabel(i, def));
            stepButtons[i].onClick.AddListener(() => menu.PlayPuzzle(entry));
        }
        firstStep = stepButtons.Length > 0 ? stepButtons[0] : null;

        CloneButton(play, listPanel.transform, "BackButton", "Back").onClick.AddListener(CloseList);

        KeepClearOfSummary(group);
    }

    void Update()
    {
        if (listPanel && listPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseList();
    }

    static string StepLabel(int index, PuzzleDef def)
    {
        var label = (index + 1) + ". " + def.Title;
        return PuzzleProgress.IsSolved(def.Id) ? label + " (done)" : label;
    }

    void RefreshLabels()
    {
        var steps = TutorialPack.Steps;
        for (int i = 0; i < stepButtons.Length && i < steps.Count; i++)
        {
            var label = stepButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = StepLabel(i, steps[i]);
        }
    }

    void OpenList()
    {
        RefreshLabels();
        menu.enabled = false;
        mainButtons.SetActive(false);
        if (summary) summary.SetActive(false);
        listPanel.SetActive(true);
        if (EventSystem.current && firstStep) EventSystem.current.SetSelectedGameObject(firstStep.gameObject);
    }

    void CloseList()
    {
        listPanel.SetActive(false);
        mainButtons.SetActive(true);
        if (summary) summary.SetActive(true);
        if (EventSystem.current && tutorialButton) EventSystem.current.SetSelectedGameObject(tutorialButton.gameObject);
        StartCoroutine(EnableMenuNextFrame());
    }

    // Same frame re-enable would let the closing key press reach the menu shortcuts
    IEnumerator EnableMenuNextFrame()
    {
        yield return null;
        if (menu) menu.enabled = true;
    }

    static int InsertIndex(Transform group, Button play)
    {
        var puzzles = group.Find("PuzzlesButton");
        int index = puzzles ? puzzles.GetSiblingIndex() + 1 : play.transform.GetSiblingIndex() + 1;
        if (!puzzles)
        {
            for (int i = 0; i < group.childCount; i++)
            {
                var name = group.GetChild(i).name;
                if (name.StartsWith("SoloButton") || name.Contains("HowTo")) index = Mathf.Max(index, i + 1);
            }
        }
        return Mathf.Clamp(index, 0, group.childCount - 1);
    }

    // The extra button lengthens the group; push the ranked summary down if they now overlap
    void KeepClearOfSummary(Transform group)
    {
        if (!summary || !(group is RectTransform groupRect) || !(summary.transform is RectTransform summaryRect)) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(groupRect);

        var groupCorners = new Vector3[4];
        var summaryCorners = new Vector3[4];
        groupRect.GetWorldCorners(groupCorners);
        summaryRect.GetWorldCorners(summaryCorners);

        var canvas = summaryRect.GetComponentInParent<Canvas>();
        float gap = SummaryGap * (canvas ? canvas.transform.lossyScale.y : 1f);
        float overlap = summaryCorners[1].y - (groupCorners[0].y - gap);
        if (overlap > 0f) summaryRect.position -= new Vector3(0f, overlap, 0f);
    }

    static Button CloneButton(Button template, Transform parent, string name, string text)
    {
        var button = Instantiate(template, parent);
        button.name = name;
        button.onClick = new Button.ButtonClickedEvent();
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label) label.text = text;
        return button;
    }

    static void AddHeading(Button template, Transform parent, string text)
    {
        var source = template.GetComponentInChildren<TMP_Text>(true);
        var go = new GameObject("Heading", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        if (source)
        {
            label.font = source.font;
            label.fontSize = source.fontSize;
            label.color = source.color;
        }
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        if (template.transform is RectTransform templateRect)
            ((RectTransform)go.transform).sizeDelta = templateRect.sizeDelta;
    }
}
