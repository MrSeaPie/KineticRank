# MoveClock Runtime & Test Isolation Notes

## 1. Dynamic Duration Synchronization
- Root Cause: `MoveClockState` was instantiated during `MoveClock.Awake()` with the default serialized `turnDuration` (30s). When tests or match configurations set `clock.turnDuration = 1f` after component instantiation, `State.Duration` remained 30s, causing 3s test assertions for natural expiration to time out.
- Resolution: `MoveClock.TurnDuration` property must propagate updates to `State.Duration` and trigger state recalculation/reset when reconfigured before turn start.

## 2. PlayMode Test Fixture Teardown Isolation
- Root Cause: Uncaught assertion timeouts in `MoveClockComponent_NaturalClockExpiration` aborted the test before inline cleanup ran, leaving active `GameManager` and `MoveClock` instances in the test scene. This leaked singletons into all subsequent 18 PlayMode fixtures.
- Resolution: Add explicit `[TearDown]` in `MoveClockRuntimeTests.cs` using `Object.DestroyImmediate` on all instantiated fixture GameObjects inside try/finally blocks.

## 3. Peer Handoff & Settlement Integration
- `GameManager.TimeoutForfeit` sets `WasTimeout = true` and invokes `FinishMatch()`, guarded by `IsMatchOver` to ensure once-only settlement dispatch.
- Coordinated with Agent-01 for immediate handoff to ranked match session lifecycle integration once dynamic duration sync and fixture teardown are committed.