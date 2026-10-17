# Manual scene-play checklist: Input/HUD hardening & Move Resolution

Scope: tasks tsk_0mvays0ca325a0b14 (Input/HUD) and tsk_0mvayxrjkcb10b805 (Move resolution & Rank recording).
Run in the Unity Editor (2022.3.62f1), starting from Assets/Scenes/MainMenu.unity unless noted.
Record each result as PASS / FAIL / NOT RUN, with the commit hash.
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
- [ ] A3. From MainMenu, start a match, then mash the start input: Match loads once.
- [ ] A4. Play the Match scene directly (no MainMenu first; AudioController may be absent): pressing M produces no NullReferenceException in the Console.

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

## G. Move resolution and outcome recording (integrated on main via task tsk_0mvayxrjkcb10b805)
Record each item as PASS / FAIL in the editor, or note verified by automated suite:
- [ ] G1. Normal drop into an empty column: token lands in the bottom row, turn changes exactly once, HUD updates.
- [ ] G2. Drop into a full column: column downshift completes before turn changes; bottom token removed, others shift down by one, top occupied by new token.
- [ ] G3. Queen placement: transforms adjacent opposing tokens within 1 Chebyshev step to player color; turn advances.
- [ ] G4. Cleaner placement: clears all tokens in the target column; turn advances.
- [ ] G5. Winning line: 4 connected tokens triggers win banner, victory sound, disables drop input, updates Elo in SaveManager.
- [ ] G6. Simultaneous lines (Draw): board evaluates to Draw; draw banner displayed; ratings update appropriately.
- [ ] G7. Elo persistence: winning a match updates winner (+Delta) and loser (-Delta) in SaveManager; verified persisted on reload.
- [ ] G8. Match over state: once match is over, input lock prevents additional drops and piece selection.
- [ ] G9. Mid-move rejection: clicks/keystrokes during move resolution coroutine are rejected.
- [ ] G10. Rematch reset: pressing Enter after match completion reloads scene with clean board and reset power-up states.
