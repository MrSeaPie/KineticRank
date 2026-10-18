# Manual scene-play checklist: Input/HUD hardening & Move Resolution

Scope: tasks tsk_0mvays0ca325a0b14 (Input/HUD) and tsk_0mvayxrjkcb10b805 (Move resolution & Rank recording).
Run in the Unity Editor (2022.3.62f1), starting from Assets/Scenes/MainMenu.unity unless noted.
Record each result as PASS / FAIL / NOT RUN, with the commit hash. Do not mark this checklist done from reasoning or tests alone.
Automated EditMode and PlayMode test suites cover core logic and regression; manual scene testing verifies interactive feel, timing, UI, and audio playback.

Commit under test: ________  Tester: ________  Date: ________

## Status Summary of Integrations on main
- **mrg_0mvb7amea199ad9ea**: AudioController, scene fader, menu navigation.
- **mrg_0mvcdn105e59093fc**: Deterministic move resolution, board downshift, power-ups, GameManager.IsMatchOver, GameManager.IsInputLocked, and SaveManager Elo integration.
- **mrg_0mvce3fgm1fb891ef**: PlayerController and RankedControls match-state guards, preventing mid-match rematch or reset, and enforcing input lock during resolution.
- **mrg_0mvckice902f44e36**: Lazy SaveManager loading, isolated disposable test scope, and live GameManager match-recording PlayMode tests.

---

## A. Scene fader and menu navigation (in merge mrg_0mvb7amea199ad9ea)
- [ ] A1. From Match, press M once: one click sound, one fade, MainMenu loads.
- [ ] A2. From Match, press M rapidly 5+ times: still exactly one fade and one load, no extra click sounds after the first.
- [ ] A3. From MainMenu, press Enter/KeypadEnter or click Play: one click sound, one fade, Match loads.
- [ ] A4. Play the Match scene directly (no MainMenu first; AudioController may be absent): pressing M produces no NullReferenceException in the Console.
- [ ] A5. From MainMenu, click Play rapidly 5+ times: still exactly one fade and one load.

## B. Rematch (Enter)
- [ ] B1. After a win, press Enter once: Match reloads once and the board is empty.
- [ ] B2. After a win, press Enter rapidly: only one reload.
- [ ] B3. Mid-match, press Enter: nothing happens. (Verified: GameManager.IsMatchOver guard integrated on main).

## C. Ranked reset (Backspace / Reset button)
- [ ] C1. Press Backspace once: toast asks for confirmation; Elo is unchanged.
- [ ] C2. Press Backspace again within 2s: Elo resets; toast confirms; the result text (if a win banner is showing) is not overwritten.
- [ ] C3. Press Backspace, wait more than 2s, press again: re-arms (confirmation toast again), no reset.
- [ ] C4. Repeat C1-C3 with Time.timeScale = 0 (pause, if available): the window still expires on real time.
- [ ] C5. In MainMenu, Backspace and the Reset button share one confirmation: one Backspace then one button click within 2s resets once.
- [ ] C6. Mid-match, reset attempt is rejected with informative toast. (Verified: GameManager.IsMatchOver guard integrated on main).
- [ ] C7. Restart the game after a reset: Elo stays reset (persistence via SaveManager).

## D. Power-up selection
- [ ] D1. Select Queen, drop it: selection returns to Normal after the drop.
- [ ] D2. Try to select the same side's Queen again: selection stays/falls back to Normal.
- [ ] D3. Same as D1-D2 for Cleaner.
- [ ] D4. Red's spent Queen does not affect Blue's Queen availability.
- [ ] D5. A normal drop after a spent power-up attempt places a Normal token and changes turn exactly once.

## E. Power-up HUD (PowerUpHud.cs)
- [ ] E1. In Match, the HUD shows whose turn it is, the selected piece, and Queen/Cleaner status for both sides.
- [ ] E2. Values update immediately after selection and after each drop.
- [ ] E3. HUD does not overlap the win banner, Elo HUD or toast line at 16:9 and 4:3 Game view sizes.
- [ ] E4. HUD hides after a win (PlayerController disabled).
- [ ] E5. HUD does not appear in MainMenu.
- [ ] E6. Reloading Match (Enter) results in exactly one HUD, not duplicates.

## F. Input during move resolution (integrated on main via GameManager.IsInputLocked)
- [ ] F1. During a Queen flip or Cleaner clear animation, clicks/keys do not place a token or change selection.
- [ ] F2. During a full-column downshift animation, input is ignored until all tokens settle.
- [ ] F3. After a win, no further tokens can be placed.

## G. Move resolution, outcomes, and Elo recording (Verbatim Original Criteria)
Scope: task tsk_0mvayxrjkcb10b805. Run in the Unity Editor (2022.3.62f1), starting from Assets/Scenes/Match.unity.
Record each result as PASS / FAIL / NOT RUN, with the commit hash. Do not mark this checklist done from reasoning or automated tests alone.

- [ ] G1. Normal drop into an empty column: the token lands in the bottom row, the turn changes exactly once, and the HUD shows the new side. [NOT RUN]
- [ ] G2. Drop into a full column: the column downshift completes fully before the turn changes; the token count on the board is as expected (no lost or duplicated tokens). [NOT RUN]
- [ ] G3. Queen drop: every flip completes before the next move is admitted; the turn changes exactly once after the last flip. [NOT RUN]
- [ ] G4. Cleaner drop: the clear completes, remaining tokens settle, and only then does the turn change (exactly once). [NOT RUN]
- [ ] G5. A move that creates a line for the mover: the win is declared only after resolution finishes, with the correct side on the win banner. [NOT RUN]
- [ ] G6. A move whose resolution (flip/clear/downshift) creates a line for the opponent, or for both sides: record which side the game declares and whether it matches the rule Agent-01 documents. Record any mismatch as FAIL with the board state. [NOT RUN]
- [ ] G7. After a win, exactly one Elo update is recorded: the Elo HUD changes once, and after restarting the game the persisted values match what the HUD showed. [NOT RUN]
- [ ] G8. Win, then press Enter and M rapidly: still exactly one Elo update for that match. [NOT RUN]
- [ ] G9. Fill the board with no line (if reachable): record what happens. SaveManager currently has no draw outcome, so record any missing draw handling as KNOWN GAP unless Agent-01's merge adds one. [NOT RUN]
  *(Clarification: Draw outcome recording is now implemented on main via SaveManager and BoardEvaluator; simultaneous 4-in-a-row lines evaluate to Draw and adjust Elo accordingly. A full board with no lines remains InProgress because column downshift allows further play).*
- [ ] G10. Play the same move sequence twice from an empty board: the resulting board and outcome are identical both times (no randomness in resolution). [NOT RUN]

---

## Appendix: Automated Regression Mappings and Verification Boundaries
Automated tests provide regression safety but do not replace interactive scene execution. Test scopes are strictly separated into:

### 1. In-Memory Match Recording & Input Lock (PlayMode Integration)
- **Fixture**: `MatchRecordingTests` in `Assets/Tests/PlayMode/MatchRecordingTests.cs`
- **Scope & Boundary**: Uses runtime reflection and `SaveManager.BeginIsolatedTestScope()`. Validates live `GameManager` move completion, turn state transitions, and single-write Elo invocation in memory (`RedLineRecordsExactlyOneWin`, `BlueLineRecordsExactlyOneWin`).
- **Relevant Manual Criteria**: Automated backing for single outcome triggers (G5, G7, G8 input locking), pending interactive verification of audio, visual banners, and UI HUD.

### 2. Disk Persistence & Cache Invalidation (EditMode Unit Tests)
- **Fixture**: `RankPersistenceTests` / `SaveManagerDiskPersistenceTests` in `Assets/Tests/EditMode/RankPersistenceTests.cs`
- **Scope & Boundary**: Uses temporary isolated directory scopes. Validates atomic write, disk round-trip reload (`RecordMatchOutcome_PersistsToDiskAndReloads`), cache discarding, corrupted JSON fallback, and Elo delta calculations across process/disk reloads without launching Unity scenes.
- **Relevant Manual Criteria**: Automated backing for disk reload integrity (G7 reload persistence, C7 reset persistence).

### 3. Board Resolution, Power-ups & Admission Guards (PlayMode / EditMode)
- **Fixtures**: `BoardResolutionTests`, `MoveAdmissionTests`, `BoardEvaluatorTests`, `TokenSettlingTests`
- **Scope & Boundary**: Validates board token array state, Chebyshev flip radius for Queen, column clear for Cleaner, animated downshift settling, and input admission rejection during animations.
- **Relevant Manual Criteria**: Automated backing for deterministic logic (G1-G4, G6, G10), pending interactive scene validation.
