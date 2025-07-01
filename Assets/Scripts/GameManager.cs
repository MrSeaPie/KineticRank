using UnityEngine;
using TMPro;                         // ←  TextMesh Pro

public class GameManager : MonoBehaviour
{
    /* ------------------------------------------------
     *  SINGLETON  (optional, but handy)
     * ------------------------------------------------*/
    public static GameManager Instance { get; private set; }

    /* ------------------------------------------------
     *  Scene references – drag these in the Inspector
     * ------------------------------------------------*/
    [Header("Scene references")]
    public BoardManager     board;
    public PlayerController player;

    [Header("UI")]
    public TMP_Text winBanner;       // ←  Banner text (TextMesh Pro – Text UI)

    /* ------------------------------------------------
     *  Game state
     * ------------------------------------------------*/
    public bool redTurn = true;

    void Awake()
    {
        Instance = this;             // assume only one in the scene
        HideBanner();                // banner invisible at start
    }

    /* ------------------------------------------------
     *  Called by BoardManager each time a token lands
     * ------------------------------------------------*/
    public void OnTokenPlaced(int x, int y, bool isRed)
    {
        if (CheckWin(x, y, isRed))
        {
            // Announce the winner
            if (isRed)
                ShowBanner("RED wins!",  Color.red);
            else
                ShowBanner("BLUE wins!", Color.cyan);

            // TODO:  disable further input or offer a “Play Again” button
            return;
        }

        // Switch turns
        redTurn = !redTurn;
    }

    /* ================================================================
     *  Win detection helpers
     * ================================================================*/
    bool CheckWin(int x, int y, bool isRed)
    {
        // Directions to check:  horizontal, vertical, 2 diagonals
        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1)
        };

        foreach (var d in dirs)
        {
            int n = 1                         // the token just placed
                    + CountDir(x, y,  d.x,  d.y, isRed)
                    + CountDir(x, y, -d.x, -d.y, isRed);

            if (n >= 4) return true;          // four (or more) in a row
        }
        return false;
    }

    int CountDir(int x, int y, int dx, int dy, bool isRed)
    {
        int c = 0;
        while (true)
        {
            x += dx; y += dy;

            if (!board.WithinBounds(x, y) || !board.IsTokenColor(x, y, isRed))
                break;

            c++;
        }
        return c;
    }

    /* ================================================================
     *  Banner UI helpers
     * ================================================================*/
    void ShowBanner(string text, Color colour)
    {
        if (winBanner == null) return;

        winBanner.text  = text;
        winBanner.color = colour;
        winBanner.gameObject.SetActive(true);
    }

    public void HideBanner()
    {
        if (winBanner != null)
            winBanner.gameObject.SetActive(false);
    }
}






