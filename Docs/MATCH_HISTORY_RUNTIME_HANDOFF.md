# Match history runtime integration handoff

Status: runtime integration incomplete; interactive acceptance NOT RUN.

## Observed contracts

- Current GameManager.cs has no MatchHistoryCapture field, initialization, move subscription or completion call.
- MatchHistoryCapture constructs replay state from an empty standard board, starting side and power-up availability. Its caller must exclude puzzles, tutorials and nonstandard fixture boards.
- RecordMove(column, kind, moverWasRed) validates side and legality, advances deterministic state and appends the move. Divergence permanently invalidates the capture.
- Complete(result, reason) is idempotent and delegates persistence to MatchRecordStore. A failed completion does not retry.
- GameManager.CompleteMove evaluates the resolved board. FinishMatch guards duplicate completion and settles the admitted ranked session before displaying the outcome.
- PlayerController.MovePlaced reports successful placement with explicit mover and kind after the turn flips, as verified in earlier source inspection.
- Agent-03's pending clock proposal adds TimeoutForfeit, WasTimeout and MatchResult to GameManager. Those changes are not present on this branch.

## Integration requirements

1. Confirm the actual starter, tutorial exclusion and power-up admission APIs before editing runtime code.
2. Capture match identity and metadata once at admission; do not reconstruct them from mutable menu settings at completion.
3. Subscribe to successful placements before input can begin and unsubscribe on destruction.
4. Record the admitted move before asynchronous board resolution finishes; persist only after the terminal outcome is known.
5. Exclude seeded and nonstandard boards. A board becoming nonempty is not by itself evidence that it was admitted correctly.
6. Add focused runtime verification using isolated persistence: standard match completion, duplicate completion, unfinished match, exclusions and side alternation.
7. Integrate timeout recording after the clock proposal is merged or explicitly reconcile the shared GameManager changes during review. Do not claim timeout support from the existing helper tests.

## Outstanding review

Ranked transition and presentation acceptance documentation remain in open proposal mrg_0mvicttyk66f8d7e2. Agent-03's clock proposal mrg_0mvi8nvv56dbac9a6 also awaits independent review. Neither proposal is evidence of integration into main.

This document records inspected contracts and remaining work; it does not establish runtime recording, fresh test success or interactive acceptance.
