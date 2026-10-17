# Manual scene-play checklist: Input/HUD hardening (Agent-02)

Scope: task tsk_0mvays0ca325a0b14. Run in the Unity Editor (2022.3.62f1), starting from Assets/Scenes/MainMenu.unity unless noted.
Record each result as PASS / FAIL / NOT RUN, with the commit hash. Do not mark this checklist done from reasoning or tests alone.

Commit under test: ________  Tester: ________  Date: ________

## A. Scene fader and menu navigation (in merge mrg_0mvb7amea199ad9ea)
- [ ] A1. From Match, press M once: one click sound, one fade, MainMenu loads.
- [ ] A2. From Match, press M rapidly 5+ times: still exactly one fade and one load, no extra click sounds after the first.
- [ ] A3. From MainMenu, start a match, then mash the start input: Match loads once.
- [ ] A4. Play the Match scene directly (no MainMenu first; AudioController may be absent): pressing M produces no NullReferenceException in the Console.

## B. Rematch (Enter)
- [ ] B1. After a win, press Enter once: Match reloads once and the board is empty.
- [ ] B2. After a win, press Enter rapidly: only one reload.
- [ ] B3. (Pending GameManager.IsMatchOver on main) Mid-match, press Enter: nothing happens. Until that wiring lands, record the current behaviour (reloads mid-match) as KNOWN GAP, not FAIL.

## C. Ranked reset (Backspace / Reset button)
- [ ] C1. Press Backspace once: toast asks for confirmation; Elo is unchanged.
- [ ] C2. Press Backspace again within 2s: Elo resets; toast confirms; the result text (if a win banner is showing) is not overwritten.
- [ ] C3. Press Backspace, wait more than 2s, press again: re-arms (confirmation toast again), no reset.
- [ ] C4. Repeat C1-C3 with Time.timeScale = 0 (pause, if available): the window still expires on real time.
- [ ] C5. In MainMenu, Backspace and the Reset button share one confirmation: one Backspace then one button click within 2s resets once.
- [ ] C6. (Pending GameManager.IsMatchOver on main) Mid-match, the reset is refused with a toast. Until then, record as KNOWN GAP.
- [ ] C7. Restart the game after a reset: Elo stays reset (persistence via SaveManager).

## D. Power-up selection
- [ ] D1. Select Queen, drop it: selection returns to Normal after the drop.
- [ ] D2. Try to select the same side's Queen again: selection stays/falls back to Normal.
- [ ] D3. Same as D1-D2 for Cleaner.
- [ ] D4. Red's spent Queen does not affect Blue's Queen availability.
- [ ] D5. A normal drop after a spent power-up attempt places a Normal token and changes turn exactly once.

## E. Power-up HUD (PowerUpHud.cs, follow-up merge, not yet committed)
- [ ] E1. In Match, the HUD shows whose turn it is, the selected piece, and Queen/Cleaner status for both sides.
- [ ] E2. Values update immediately after selection and after each drop.
- [ ] E3. HUD does not overlap the win banner, Elo HUD or toast line at 16:9 and 4:3 Game view sizes.
- [ ] E4. HUD hides after a win (PlayerController disabled).
- [ ] E5. HUD does not appear in MainMenu.
- [ ] E6. Reloading Match (Enter) results in exactly one HUD, not duplicates.

## F. Input during move resolution (pending Agent-01 IsInputLocked on main)
- [ ] F1. During a Queen flip or Cleaner clear animation, clicks/keys do not place a token or change selection.
- [ ] F2. During a full-column downshift animation, input is ignored until all tokens settle.
- [ ] F3. After a win, no further tokens can be placed.

## G. Move resolution and outcome recording (pending Agent-01 serialized resolution, task tsk_0mvayxrjkcb10b805)
Run only on a commit that contains Agent-01's resolution integration; before that, record every G item as NOT RUN.
Write down the board state (screenshot or column-by-column) before and after each step.
- [ ] G1. Normal drop into an empty column: the token lands in the bottom row, the turn changes exactly once, and the HUD shows the new side.
- [ ] G2. Drop into a full column: the column downshift completes fully before the turn changes; the token count on the board is as expected (no lost or duplicated tokens).
- [ ] G3. Queen drop: every flip completes before the next move is admitted; the turn changes exactly once after the last flip.
- [ ] G4. Cleaner drop: the clear completes, remaining tokens settle, and only then does the turn change (exactly once).
- [ ] G5. A move that creates a line for the mover: the win is declared only after resolution finishes, with the correct side on the win banner.
- [ ] G6. A move whose resolution (flip/clear/downshift) creates a line for the opponent, or for both sides: record which side the game declares and whether it matches the rule Agent-01 documents. Record any mismatch as FAIL with the board state.
- [ ] G7. After a win, exactly one Elo update is recorded: the Elo HUD changes once, and after restarting the game the persisted values match what the HUD showed.
- [ ] G8. Win, then press Enter and M rapidly: still exactly one Elo update for that match.
- [ ] G9. Fill the board with no line (if reachable): record what happens. SaveManager currently has no draw outcome, so record any missing draw handling as KNOWN GAP unless Agent-01's merge adds one.
- [ ] G10. Play the same move sequence twice from an empty board: the resulting board and outcome are identical both times (no randomness in resolution).

## Notes / failures
(Record Console errors verbatim with the step number.)
