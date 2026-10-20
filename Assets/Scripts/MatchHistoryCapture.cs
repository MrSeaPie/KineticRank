using System;
using KineticRank.Logic;
using UnityEngine;

// One runtime recording session. The caller admits only standard empty-board matches.
// Seeded puzzles, tutorials and nonstandard fixture boards must not create a session.
public sealed class MatchHistoryCapture
{
    readonly MatchRecord record;
    readonly MatchRecordStore store;
    GameState state;
    bool finished;
    bool invalid;

    public MatchHistoryCapture(string mode, bool redStarts, bool powerUpsEnabled,
        string redLabel, string blueLabel, int redRating, int blueRating,
        MatchRecordStore destination = null)
    {
        store = destination ?? new MatchRecordStore();
        record = new MatchRecord
        {
            Mode = mode,
            RedStarts = redStarts,
            PowerUpsEnabled = powerUpsEnabled,
            RedLabel = redLabel,
            BlueLabel = blueLabel,
            RedRating = redRating,
            BlueRating = blueRating,
            StartedUtc = MatchRecord.UtcTimestamp(DateTime.UtcNow)
        };
        state = GameState.FromGrid(new int[GameState.Columns, GameState.Rows], redStarts,
            powerUpsEnabled, powerUpsEnabled, powerUpsEnabled, powerUpsEnabled);
    }

    // Called on successful placement admission, before asynchronous board completion.
    public bool RecordMove(int column, MoveKind kind, bool moverWasRed)
    {
        if (finished || invalid) return false;
        var move = new Move(column, kind);
        if (state.RedToMove != moverWasRed || !state.IsLegal(move))
        {
            invalid = true;
            Debug.LogWarning("[MatchHistoryCapture] Runtime moves diverged; history discarded.");
            return false;
        }
        state = state.Apply(move);
        record.Moves.Add(new RecordedMove(move));
        record.Plies = record.Moves.Count;
        return true;
    }

    // Called once from match completion, after the terminal move has been recorded.
    // A failed or duplicate completion never writes a partial or duplicate record.
    public bool Complete(EvaluationResult result, MatchEndReason reason)
    {
        if (finished) return false;
        finished = true;
        if (invalid) return false;
        record.Result = result;
        record.EndReason = reason;
        record.EndedUtc = MatchRecord.UtcTimestamp(DateTime.UtcNow);
        try
        {
            return store.Add(record);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MatchHistoryCapture] History discarded: " + ex.Message);
            return false;
        }
    }
}
