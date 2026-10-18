using UnityEngine;
using UnityEngine.UI;
using KineticRank.Logic;

// Solo entry for MainMenu: one button per bot tier. Sets the solo choice and starts the match through MainMenuController.
// No styling of its own; the wooden button/panel art is applied in the scene.
public class SoloMenuPanel : MonoBehaviour
{
    [Tooltip("MainMenuController that loads the Match scene")]
    public MainMenuController menu;

    [Tooltip("Tier buttons in order: Pine, Birch, Oak, Walnut, Ebony (any may be left empty)")]
    public Button[] tierButtons = new Button[5];

    static readonly BotTier[] Tiers = { BotTier.Pine, BotTier.Birch, BotTier.Oak, BotTier.Walnut, BotTier.Ebony };

    void Awake()
    {
        for (int i = 0; i < tierButtons.Length && i < Tiers.Length; i++)
        {
            if (!tierButtons[i]) continue;
            BotTier tier = Tiers[i];
            tierButtons[i].onClick.AddListener(() => StartTier(tier));
        }
    }

    public void StartTier(BotTier tier)
    {
        if (!menu)
        {
            Debug.LogWarning("[SoloMenu] menu is not assigned.");
            return;
        }
        menu.PlaySolo(tier);
    }
}
