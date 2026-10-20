using KineticRank.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lives inside the main button group so MenuLayoutFit includes its height and
// submenu visibility follows the existing group. Legacy reset prompts are untouched.
public sealed class PlayerRankMenuPresenter : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var menu = FindObjectOfType<MainMenuController>();
        if (!menu || !menu.playButton || !menu.playButton.transform.parent) return;
        var group = menu.playButton.transform.parent;
        if (group.GetComponentInChildren<PlayerRankMenuPresenter>(true)) return;
        var root = new GameObject("PlayerRank", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        root.transform.SetParent(group, false);
        root.GetComponent<LayoutElement>().preferredHeight = 116f;
        root.GetComponent<LayoutElement>().minHeight = 116f;
        var background = root.GetComponent<Image>();
        background.color = new Color(0.16f, 0.09f, 0.045f, 0.96f);
        background.raycastTarget = false;
        root.AddComponent<PlayerRankMenuPresenter>();
    }

    void Start()
    {
        var profile = PlayerRankStore.GetProfile();
        var badge = new GameObject("Shield", typeof(RectTransform), typeof(RankShieldGraphic));
        badge.transform.SetParent(transform, false);
        var badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 0.5f);
        badgeRect.pivot = new Vector2(0f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(12f, 0f);
        badgeRect.sizeDelta = new Vector2(48f, 62f);
        var graphic = badge.GetComponent<RankShieldGraphic>();
        graphic.color = profile.IsPlacement ? new Color(0.7f, 0.65f, 0.56f) : TierColour(profile.currentTier);
        graphic.raycastTarget = false;

        var labelObject = new GameObject("RankText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(transform, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(72f, 10f);
        rect.offsetMax = new Vector2(-12f, -10f);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        label.color = new Color(1f, 0.92f, 0.77f);
        label.fontSize = 23f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 23f;
        label.enableWordWrapping = true;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "PLAYER RANK  •  " + profile.DisplayRank + "\n" + Detail(profile);
    }

    static string Detail(PlayerRankProfile profile)
    {
        if (profile.IsPlacement)
            return "Complete " + profile.placementGamesLeft + " more placement " +
                (profile.placementGamesLeft == 1 ? "game" : "games");
        if (profile.currentTier == RankTier.Master)
            return profile.rating + " rating  •  Highest tier";
        int start = RankBands.TierMinimums[(int)profile.currentTier] +
            (3 - profile.currentDivision) * RankBands.DivisionWidth;
        int next = start + RankBands.DivisionWidth;
        int remaining = Mathf.Max(0, next - profile.rating);
        return profile.rating + " rating  •  " + remaining + " to next division";
    }

    static Color TierColour(RankTier tier)
    {
        switch (tier)
        {
            case RankTier.Silver: return new Color(0.76f, 0.81f, 0.86f);
            case RankTier.Gold: return new Color(1f, 0.76f, 0.22f);
            case RankTier.Platinum: return new Color(0.42f, 0.86f, 0.79f);
            case RankTier.Diamond: return new Color(0.42f, 0.76f, 1f);
            case RankTier.Master: return new Color(0.8f, 0.46f, 0.95f);
            default: return new Color(0.77f, 0.46f, 0.25f);
        }
    }
}
