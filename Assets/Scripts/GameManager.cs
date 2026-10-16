using UnityEngine;
using System.Collections;
using KineticRank.Logic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene references — drag in Inspector")]
    public BoardManager board;
    public PlayerController player;
    public WinBanner winBanner;
    [Header("UI (optional)")]
    public GameObject backToMenuButton;

    public bool IsMatchOver { get; private set; }
    public bool IsResolvingMove { get; private set; }
    public bool IsInputLocked => IsMatchOver || IsResolvingMove;

    void Awake()
    {
        Instance = this;
        if (backToMenuButton != null) backToMenuButton.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryBeginMove()
    {
        if (IsInputLocked) return false;
        IsResolvingMove = true;
        return true;
    }

    public void CompleteMove()
    {
        if (IsMatchOver || !IsResolvingMove) return;
        IsResolvingMove = false;
        EvaluateBoard();
    }

    // Compatibility callback while BoardManager integration is in progress.
    public void OnTokenPlaced(int x, int y, bool isRed)
    {
        if (IsMatchOver) return;
        IsResolvingMove = false;
        EvaluateBoard();
    }

    void EvaluateBoard()
    {
        if (board == null) return;
        var grid = new int[board.columns, board.rows];
        for (int x = 0; x < board.columns; ++x)
        for (int y = 0; y < board.rows; ++y)
            grid[x, y] = board.IsToken(x, y, true) ? 1 : board.IsToken(x, y, false) ? 2 : 0;

        var result = BoardEvaluator.Evaluate(grid);
        if (result == EvaluationResult.InProgress) return;
        FinishMatch(result);
    }

    void FinishMatch(EvaluationResult result)
    {
        if (IsMatchOver) return;
        IsMatchOver = true;
        IsResolvingMove = false;
        if (player != null) player.enabled = false;
        bool draw = result == EvaluationResult.Draw;
        bool redWins = result == EvaluationResult.RedWin;
        if (draw) SaveManager.RecordDraw();
        else SaveManager.RecordMatch(redWins);
        EloHud.RefreshAll();
        if (!draw && AudioController.Instance != null) AudioController.Instance.PlayWin(redWins);
        if (winBanner != null)
            winBanner.Show(draw ? "DRAW — both players have four!" : redWins ? "RED wins!" : "BLUE wins!",
                draw ? Color.white : redWins ? Color.red : Color.cyan);
        if (backToMenuButton != null) backToMenuButton.SetActive(true);
        StartCoroutine(ShowRematchHint());
    }

    IEnumerator ShowRematchHint()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (winBanner != null) winBanner.AppendHint("Press Enter for rematch");
    }
}
