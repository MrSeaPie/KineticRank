# MoveClock Lifecycle & Turn Handoff Specification

## 1. Resolution Pause Guard
In `MoveClock.Update()`:
- Checks `GameManager.Instance.IsResolvingMove`.
- When true (during kinetic shift / token drop animations), `State.Tick(Time.deltaTime)` is bypassed, pausing the move timer.
- Once animation resolves, clock ticking resumes automatically for the active side.

## 2. Turn Transition Handoff
- `PlayerController.MovePlaced` notifies listeners when a legal move is placed, passing `(int column, MoveKind kind, bool moverWasRed)`.
- On turn transition, `MoveClock.StartTurn(incomingIsRed)` resets `SettledTimeout = false`, re-initializes `MoveClockState` with fresh 30s turn duration and 10s warning threshold, and rebinds the active timer bar.
- Timer bars reflect active player draining state, switching color to red during the final 10 seconds.
