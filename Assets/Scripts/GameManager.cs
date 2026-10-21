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
    public bool WasTimeout { get; private set; }
    public EvaluationResult MatchResult { get; private set; }
    public string Winner => MatchResult == EvaluationResult.RedWin ? "Red" : MatchResult == EvaluationResult.BlueWin ? "Blue" : MatchResult == EvaluationResult.Draw ? "Draw" : null;

    void Awake()
    {
        Instance = this;
        WasTimeout = false;
        MatchResult = EvaluationResult.InProgress;
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

    // Legacy notifications cannot finish a move owned by BoardManager.ResolveMove.
    public void OnTokenPlaced(int x, int y, bool isRed)
    {
        if (IsInputLocked) return;
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

    public void TimeoutForfeit(bool redTimedOut)
    {
        if (IsMatchOver) return;
        WasTimeout = true;
        FinishMatch(redTimedOut ? EvaluationResult.BlueWin : EvaluationResult.RedWin);
    }

    void FinishMatch(EvaluationResult result)
    {
        if (IsMatchOver) return;
        IsMatchOver = true;
        MatchResult = result;
        IsResolvingMove = false;
        if (player != null) player.enabled = false;
        bool draw = result == EvaluationResult.Draw;
        bool redWins = result == EvaluationResult.RedWin;
        if (PuzzleSettings.Enabled) RecordPuzzleResult(draw, redWins);
        // Traditional matches are always casual: no save stats, ranked Elo or solo ladder, even against a bot.
        else if (TraditionalSettings.Enabled) { }
        else if (SoloSettings.Enabled) RecordSoloResult(draw, redWins);
        else
        {
            if (draw) SaveManager.RecordDraw();
            else SaveManager.RecordMatch(redWins);
            EloHud.RefreshAll();
        }
        if (!draw && AudioController.Instance != null) AudioController.Instance.PlayWin(redWins);
        else if (draw && AudioController.Instance != null) AudioController.Instance.PlayDraw();
        if (winBanner != null)
        {
            string bannerText = WasTimeout
                ? (redWins ? "BLUE timed out — RED wins!" : "RED timed out — BLUE wins!")
                : (draw ? "DRAW — both players have four!" : redWins ? "RED wins!" : "BLUE wins!");
            winBanner.Show(bannerText, draw ? Color.white : redWins ? Color.red : Color.cyan);
        }
        if (backToMenuButton != null) backToMenuButton.SetActive(true);
        StartCoroutine(ShowRematchHint());
    }

    // Puzzle results only mark the puzzle solved; ranked Elo and the solo ladder are never touched.
    static void RecordPuzzleResult(bool draw, bool redWins)
    {
        if (!draw && redWins == PuzzleSettings.AttackerIsRed) PuzzleProgress.MarkSolved(PuzzleSettings.CurrentId);
    }

    // Solo matches go to the separate solo ladder; ranked Elo is never touched.
    static void RecordSoloResult(bool draw, bool redWins)
    {
        if (draw) { SoloLadder.RecordDraw(SoloSettings.Tier); return; }
        bool humanWon = redWins != SoloSettings.BotPlaysRed;
        if (humanWon) SoloLadder.RecordWin(SoloSettings.Tier);
        else SoloLadder.RecordLoss(SoloSettings.Tier);
    }

    IEnumerator ShowRematchHint()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (winBanner != null) winBanner.AppendHint("Press Enter for rematch");
    }
}
