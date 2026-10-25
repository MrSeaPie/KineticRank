using System;
using KineticRank.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Puzzle runtime: plays a PuzzleSession on the live board. The human is the attacker; the defender's forced reply
// goes through PlayerController.SubmitPuzzleReply. Runs only while PuzzleSettings.Enabled and a puzzle was begun.
public class PuzzleMatchDriver : MonoBehaviour
{
    static GameState pendingStart;
    static int pendingMovesN;

    // Entry point for the menu / pack adapter; static so it survives the load into Match (and Retry reloads)
    public static void Begin(GameState start, int movesN)
    {
        if ((object)start == null) throw new ArgumentNullException(nameof(start));
        pendingStart = start;
        pendingMovesN = movesN;
    }

    public static void Clear()
    {
        pendingStart = null;
        pendingMovesN = 0;
    }

    public static bool HasPuzzle => PuzzleSettings.Enabled && (object)pendingStart != null;

    public PuzzleSession Session { get; private set; }
    public bool AttemptFailed { get; private set; }

    BoardManager board;
    PlayerController player;
    bool loaded;
    Move? pendingReply;

    void OnEnable() { PlayerController.MovePlaced += OnMovePlaced; }
    void OnDisable() { PlayerController.MovePlaced -= OnMovePlaced; }

    void Update()
    {
        if (!HasPuzzle) return;
        if (board == null) board = FindObjectOfType<BoardManager>();
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (board == null || player == null) return;

        var manager = GameManager.Instance;
        if (manager != null && (manager.IsInputLocked || manager.IsMatchOver)) return;

        if (!loaded)
        {
            if (Session == null) Session = new PuzzleSession(pendingStart, pendingMovesN);
            if (!board.LoadPosition(Session.Start)) return;
            player.LoadTurnState(Session.Start);
            loaded = true;
            return;
        }

        // Defender reply waits until the attacker's drop has resolved, then uses the normal placement path
        if (pendingReply.HasValue && player.SubmitPuzzleReply(pendingReply.Value.Column, pendingReply.Value.Kind))
            pendingReply = null;
    }

    void OnMovePlaced(int column, MoveKind kind, bool moverIsRed)
    {
        if (!loaded || Session == null || AttemptFailed) return;
        if (Session.Status != PuzzleStatus.InProgress) return;
        if (moverIsRed != Session.AttackerIsRed) return;   // our own defender reply

        PuzzleStatus status;
        try
        {
            status = Session.SubmitAttackerMove(new Move(column, kind));
        }
        catch (ArgumentException)
        {
            // board and session disagree on legality; treat as a failed attempt rather than continue out of sync
            status = PuzzleStatus.Failed;
        }

        if (status == PuzzleStatus.Failed) AttemptFailed = true;
        else if (status == PuzzleStatus.InProgress) pendingReply = Session.LastDefenderReply;
        // Solved: the board's own win ends the match and GameManager.FinishMatch records PuzzleProgress
    }

    // Reset in place: fresh session, and Update reloads the start position once the board is idle.
    // Only a finished match needs the scene reload (Begin's static state rebuilds the same start position).
    public void Retry()
    {
        var manager = GameManager.Instance;
        if (manager != null && manager.IsMatchOver)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        Session?.Restart();
        AttemptFailed = false;
        pendingReply = null;
        loaded = false;
    }

    void OnGUI()
    {
        if (!loaded || Session == null) return;

        GUI.Label(new Rect(20, 20, 300, 24), $"Puzzle: win in {Session.MovesLeft}");
        if (!AttemptFailed) return;

        var panel = new Rect(Screen.width / 2f - 140, Screen.height / 2f - 50, 280, 100);
        GUI.Box(panel, "Not the winning line");
        if (GUI.Button(new Rect(panel.x + 90, panel.y + 45, 100, 32), "Retry")) Retry();
    }
}
