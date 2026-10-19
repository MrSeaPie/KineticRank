using System;
using UnityEngine;
using KineticRank.Logic;

public class PlayerController : MonoBehaviour
{
    public BoardManager board;

    [Header("Normal tokens")]
    public GameObject redTokenPrefab;
    public GameObject blueTokenPrefab;

    [Header("Power-ups (visual discs that drop)")]
    public GameObject redQueenPrefab;
    public GameObject blueQueenPrefab;
    public GameObject redCleanerPrefab;
    public GameObject blueCleanerPrefab;

    bool isRedTurn = true;

    bool redQueenUsed = false, redCleanerUsed = false;
    bool blueQueenUsed = false, blueCleanerUsed = false;

    enum PieceType { Normal, Queen, Cleaner }
    PieceType currentPiece = PieceType.Normal;

    // Read-only state for HUD display
    public bool IsRedTurn => isRedTurn;
    public string CurrentPieceName => currentPiece.ToString();
    public bool QueenAvailable(bool red)   => red ? !redQueenUsed   : !blueQueenUsed;
    public bool CleanerAvailable(bool red) => red ? !redCleanerUsed : !blueCleanerUsed;

    // Puzzle/tutorial entry: take side to move and power-up availability from a loaded position (pair with BoardManager.LoadPosition)
    public void LoadTurnState(GameState s)
    {
        if (s == null) return;
        isRedTurn = s.RedToMove;
        redQueenUsed = !s.QueenAvailable(true);
        redCleanerUsed = !s.CleanerAvailable(true);
        blueQueenUsed = !s.QueenAvailable(false);
        blueCleanerUsed = !s.CleanerAvailable(false);
        currentPiece = PieceType.Normal;
    }

    // In Solo mode the bot's side ignores mouse/keys; SoloBotDriver submits through SubmitBotMove instead
    bool IsBotTurn => SoloSettings.Enabled && isRedTurn == SoloSettings.BotPlaysRed;

    // Puzzle mode: the defender side ignores mouse/keys; the puzzle runtime submits through SubmitPuzzleReply instead
    bool IsPuzzleDefenderTurn => PuzzleSettings.Enabled && isRedTurn != PuzzleSettings.AttackerIsRed;

    Func<int, MoveKind, bool> botSink;

    void OnEnable()
    {
        botSink = SubmitBotMove;
        SoloBotDriver.MoveSink = botSink;
    }

    void OnDisable()
    {
        if (SoloBotDriver.MoveSink == botSink) SoloBotDriver.MoveSink = null;
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsInputLocked || GameManager.Instance.IsMatchOver))
            return;
        if (IsBotTurn || IsPuzzleDefenderTurn)
            return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            currentPiece = PieceType.Normal;
            if (AudioController.Instance != null) AudioController.Instance.PlayTokenSelect();
        }
        if (Input.GetKeyDown(KeyCode.Q)) TrySelect(PieceType.Queen);
        if (Input.GetKeyDown(KeyCode.C)) TrySelect(PieceType.Cleaner);

        if (Input.GetMouseButtonDown(0))
        {
            if (board == null || Camera.main == null) return;

            var world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int col = BoardManager.WorldToColumn(world.x);

            TryPlace(col, currentPiece);
        }
    }

    // Solo bot entry: same guards, placement, turn flip and SFX as a mouse click
    bool SubmitBotMove(int col, MoveKind kind)
    {
        if (board == null || !IsBotTurn) return false;
        if (GameManager.Instance != null && (GameManager.Instance.IsInputLocked || GameManager.Instance.IsMatchOver))
            return false;

        currentPiece = kind == MoveKind.Queen ? PieceType.Queen
                     : kind == MoveKind.Cleaner ? PieceType.Cleaner
                     : PieceType.Normal;
        return TryPlace(col, currentPiece);
    }

    // Puzzle defender entry: only on the defender's turn in puzzle mode; same placement path as a mouse click
    public bool SubmitPuzzleReply(int col, MoveKind kind)
    {
        if (board == null || !IsPuzzleDefenderTurn) return false;
        if (GameManager.Instance != null && (GameManager.Instance.IsInputLocked || GameManager.Instance.IsMatchOver))
            return false;

        currentPiece = kind == MoveKind.Queen ? PieceType.Queen
                     : kind == MoveKind.Cleaner ? PieceType.Cleaner
                     : PieceType.Normal;
        return TryPlace(col, currentPiece);
    }

    // Shared admission path for human and bot moves; returns true if the move was placed
    bool TryPlace(int col, PieceType piece)
    {
        bool placed = false;

        switch (piece)
        {
            case PieceType.Normal:
                placed = board.PlaceNormalToken(col,
                            isRedTurn ? redTokenPrefab : blueTokenPrefab,
                            isRedTurn);
                break;

            case PieceType.Queen:
                // one per player (selection already rejects a used queen; kept as a safety net)
                if (!QueenAvailable(isRedTurn))
                {
                    currentPiece = PieceType.Normal;
                    if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
                    return false;
                }

                placed = board.PlaceQueen(col,
                            isRedTurn ? redQueenPrefab : blueQueenPrefab,    // visual disc that drops
                            isRedTurn ? redTokenPrefab : blueTokenPrefab,    // the normal piece that stays
                            isRedTurn);

                if (placed)
                    if (isRedTurn) redQueenUsed = true; else blueQueenUsed = true;
                break;

            case PieceType.Cleaner:
                if (!CleanerAvailable(isRedTurn))
                {
                    currentPiece = PieceType.Normal;
                    if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
                    return false;
                }

                placed = board.PlaceCleaner(col,
                            isRedTurn ? redCleanerPrefab : blueCleanerPrefab);

                if (placed)
                    if (isRedTurn) redCleanerUsed = true; else blueCleanerUsed = true;
                break;
        }   // end switch

        if (placed)
        {
            // consume turn for ANY move
            isRedTurn = !isRedTurn;
            // always go back to Normal after a drop
            currentPiece = PieceType.Normal;
        }
        else
        {
            // rejected move (full column or no valid target)
            if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
        }

        return placed;
    }

    // Only select a power-up the current player still has; otherwise stay on Normal
    void TrySelect(PieceType piece)
    {
        bool available = piece == PieceType.Queen ? QueenAvailable(isRedTurn) : CleanerAvailable(isRedTurn);
        if (!available)
        {
            Debug.Log($"{(isRedTurn ? "Red" : "Blue")} {piece.ToString().ToLower()} already used.");
            currentPiece = PieceType.Normal;
            if (AudioController.Instance != null) AudioController.Instance.PlayInvalidMove();
            return;
        }
        currentPiece = piece;
        if (AudioController.Instance != null) AudioController.Instance.PlayTokenSelect();
    }
}
