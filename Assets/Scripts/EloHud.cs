using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EloHud : MonoBehaviour
{
    static readonly HashSet<EloHud> _instances = new HashSet<EloHud>();

    [Tooltip("Leave empty to use TMP on this same GameObject")]
    public TMP_Text label;

    RankedMatchSession rankedSession;

    void OnEnable()
    {
        _instances.Add(this);
        if (!label) label = GetComponent<TMP_Text>();
        Refresh();
    }

    void OnDisable() => _instances.Remove(this);

    public void BindRankedSession(RankedMatchSession session)
    {
        rankedSession = session;
        Refresh();
    }

    public void Refresh()
    {
        if (!label) return;
        if (rankedSession != null)
        {
            label.text = RankedMatchPresentation.Format(rankedSession, PlayerRankStore.GetProfile());
            return;
        }

        int red = SaveManager.GetRedElo();
        int blue = SaveManager.GetBlueElo();
        int rw = SaveManager.GetRedWins();
        int bw = SaveManager.GetBlueWins();
        label.text = $"RED {red} (W{rw})  |  BLUE {blue} (W{bw})";
    }

    public static void RefreshAll()
    {
        foreach (var hud in _instances)
            hud.Refresh();
    }
}
