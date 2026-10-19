using System;
using UnityEngine;
using KineticRank.Logic;

// Plays the bot's side in Solo mode. The move goes through MoveSink, the same admission path as mouse input,
// so input lockout, animations, SFX and the outcome banner behave exactly as in a human-vs-human match.
public class SoloBotDriver : MonoBehaviour
{
    public BoardManager board;
    public PlayerController player;

    [Tooltip("Pause before the bot moves so the previous drop reads clearly.")]
    public float thinkDelay = 0.35f;

    // Assigned by PlayerController once it exposes a public submit entry; returns true if the move was placed.
    public static Func<int, MoveKind, bool> MoveSink;

    float readyAt = -1f;

    // Puzzles drive their own forced-reply defender, so the solo bot stays out of them
    public bool IsBotTurn => !PuzzleSettings.Enabled && SoloSettings.Enabled && player != null
        && player.IsRedTurn == SoloSettings.BotPlaysRed;

    void Update()
    {
        if (!IsBotTurn || board == null) { readyAt = -1f; return; }
        if (GameManager.Instance != null && (GameManager.Instance.IsInputLocked || GameManager.Instance.IsMatchOver))
        {
            readyAt = -1f;
            return;
        }
        // Overlays such as the how-to-play panel pause the match by disabling the player's input
        if (!player.enabled) { readyAt = -1f; return; }

        // Wait one think delay after the board is unlocked, then search once.
        if (readyAt < 0f) { readyAt = Time.time + thinkDelay; return; }
        if (Time.time < readyAt) return;
        readyAt = -1f;

        var state = BoardSnapshot.Capture(board.IsToken, player.IsRedTurn,
            player.QueenAvailable(true), player.CleanerAvailable(true),
            player.QueenAvailable(false), player.CleanerAvailable(false));
        SubmitBotMove(Bot.ChooseMove(state, SoloSettings.Depth));
    }

    void SubmitBotMove(Move move)
    {
        if (MoveSink == null)
        {
            Debug.LogWarning("SoloBotDriver: no move sink wired yet.");
            return;
        }
        if (!MoveSink(move.Column, move.Kind))
            Debug.LogWarning($"SoloBotDriver: bot move {move.Kind} in column {move.Column} was rejected.");
    }
}
