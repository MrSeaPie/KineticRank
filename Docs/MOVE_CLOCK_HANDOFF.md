# MoveClock Turn Handoff and Resolution Lifecycle

## 1. Overview
The MoveClock component enforces a 30s configurable per-move clock in ranked matches, pausing during move resolution and overlays, and resetting to fresh duration upon authoritative turn transitions.

## 2. Authoritative Turn Transition
- **Trigger**: Turn transitions occur strictly upon completion of board resolution (post-animation physics and line checks) rather than on initial input placement.
- **Call Site**: Authoritative handoff invokes `MoveClock.Instance.StartTurn(incomingIsRed)`.
- **State Reset**: `StartTurn(bool isRed)` updates `ActiveIsRed`, clears `SettledTimeout`, sets `RemainingSeconds = turnDuration` (30.0s), and updates active side panel UI bars.

## 3. Resolution Pause Guard
- In `MoveClock.Update()`, ticking is bypassed while `GameManager.Instance != null && GameManager.Instance.IsResolvingMove`.
- Ensures move drop/pop physics and line-check animations do not deplete the player's clock allotment.

## 4. Concurrent Edits Reservation
- `Assets/Scripts/RankedMatchSession.cs` and `Assets/Scripts/RankedMatchPresentation.cs` are strictly reserved for Agent-01's settlement snapshot and UI presentation work.
- MoveClock maintains zero overlap with match presentation files.
