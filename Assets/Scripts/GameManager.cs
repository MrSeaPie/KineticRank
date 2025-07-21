using UnityEngine;
using UnityEngine.UI;      // only for Color

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene references — drag in Inspector")]
    public BoardManager     board;      // the BoardManager in the Hierarchy
    public PlayerController player;     // the PlayerController in the Hierarchy
    public WinBanner        winBanner;  // the WinBanner (child of Canvas)

    void Awake() => Instance = this;

    /*====================================================================
     *  CALLED BY BoardManager **EVERY TIME** a piece is spawned
     *===================================================================*/
    public void OnTokenPlaced(int x, int y, bool isRed)
    {
        // Quick local check (around the brand-new piece) – catches 95 % of wins
        if (ScanAround(x, y, isRed))
        {
            DeclareWinner(isRed);
            return;
        }

        // Full sweep – catches wins formed by the column push
        if (ScanWholeBoard(out bool redWon))
            DeclareWinner(redWon);
    }

    /*──────────────────────────────────────────── helpers ───────────────*/

    #region   win detection ------------------------------------------------------------------

    bool ScanAround(int x, int y, bool red)
    {
        Vector2Int[] dirs = { new(1,0), new(0,1), new(1,1), new(1,-1) };
        foreach (var d in dirs)
        {
            int n = 1 + CountDir(x,y, d.x, d.y, red)
                      + CountDir(x,y,-d.x,-d.y, red);
            if (n >= 4) return true;
        }
        return false;
    }

    bool ScanWholeBoard(out bool redWon)
    {
        for (int x = 0; x < board.columns; ++x)
        for (int y = 0; y < board.rows;    ++y)
        {
            if (board.IsToken(x,y,true)  && ScanAround(x,y,true))  { redWon = true;  return true; }
            if (board.IsToken(x,y,false) && ScanAround(x,y,false)) { redWon = false; return true; }
        }
        redWon = false;
        return false;
    }

    int CountDir(int x,int y,int dx,int dy,bool red)
    {
        int c = 0;
        while (true)
        {
            x += dx; y += dy;
            if (!board.InBounds(x,y) || !board.IsToken(x,y,red)) break;
            ++c;
        }
        return c;
    }
    #endregion

    /*──────────────────────────────────────────── UI / state ────────────*/

    void DeclareWinner(bool red)
    {
        winBanner.Show(red ? "RED wins!" : "BLUE wins!",
                       red ? Color.red  : Color.cyan);

        // stop further clicks
        player.enabled = false;
    }
}
