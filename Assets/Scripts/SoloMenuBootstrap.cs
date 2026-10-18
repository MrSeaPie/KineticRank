// File: Assets/Scripts/SoloMenuBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KineticRank.Logic;

// Adds a Solo button and a tier panel to MainMenu at runtime, so the scene file stays untouched.
// Buttons are clones of PlayButton; no art is loaded here.
public class SoloMenuBootstrap : MonoBehaviour
{
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

        // A scene-wired panel takes priority over the runtime one
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

        // Tier panel: a copy of the button group (same layout and position) with its buttons removed
        tierPanel = Instantiate(mainButtons, group.parent);
        tierPanel.name = "SoloTierPanel";
        tierPanel.SetActive(false);
        for (int i = tierPanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = tierPanel.transform.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        // Solo button sits right below Play
        var solo = CloneButton(play, group, "SoloButton", "Solo");
        solo.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
        solo.onClick.AddListener(OpenTiers);

        string[] names = System.Enum.GetNames(typeof(BotTier));
        var tierButtons = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
            tierButtons[i] = CloneButton(play, tierPanel.transform, names[i] + "Button", names[i]);
        firstTier = tierButtons[0];

        CloneButton(play, tierPanel.transform, "BackButton", "Back").onClick.AddListener(CloseTiers);

        // Panel is still inactive, so SoloMenuPanel.Awake wires the tier buttons on first open
        var panel = tierPanel.AddComponent<SoloMenuPanel>();
        panel.menu = menu;
        panel.tierButtons = tierButtons;
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

    void Update()
    {
        if (SceneFader.IsFading) return;
        if (tierPanel && tierPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseTiers();
    }

    void OpenTiers()
    {
        if (SceneFader.IsFading) return;

        // Stop the menu's key handling so Enter can't start a ranked match behind the panel
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

        // Re-enable next frame so the Escape or Enter that closed the panel doesn't also quit or start ranked
        StartCoroutine(EnableMenuNextFrame());
    }

    IEnumerator EnableMenuNextFrame()
    {
        yield return null;
        if (menu) menu.enabled = true;
    }
}
