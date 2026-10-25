# Rank presentation: interactive acceptance

Record the commit, tester, resolution and PASS / FAIL / NOT RUN for each check. Automated tests do not establish visual acceptance. Use a disposable player profile for placement and promotion scenarios; preserve any real player save.

Commit: ______  Tester: ______  Date: ______

## Main menu

Repeat at 1920×1080, 1280×720 and 1024×768.

- [ ] NOT RUN — The player-rank panel has readable text, shield and progress bar without character-by-character wrapping or overlap with menu buttons.
- [ ] NOT RUN — Placement rank shows remaining placement games and an appropriate progress fill. Check both a fresh profile and one remaining game.
- [ ] NOT RUN — An established rank shows tier/division, rating and distance to the next division. Check Bronze, an intermediate tier and Master; Master displays Highest tier and a full bar.
- [ ] NOT RUN — Open and close each available submenu. Return to the main button group and confirm one rank panel, with no overlapping or orphaned elements.
- [ ] NOT RUN — Return from a completed ranked match. The menu displays the updated persisted profile.

## Ranked entry and match

- [ ] NOT RUN — Ranked Solo displays the recommended opponent and rating; entering the match presents that opponent rating and the player's rank.
- [ ] NOT RUN — Casual Solo remains accessible and completing a casual match leaves the separate player-rank profile unchanged.
- [ ] NOT RUN — Complete ranked wins, losses and a draw. Result text describes the human player's outcome and shows the before/after rank and signed rating change once.
- [ ] NOT RUN — When applicable, the result displays remaining promotion protection without clipping other HUD elements.
- [ ] NOT RUN — After the transition-label increment is integrated, exercise promotion, demotion and final placement. Confirm the corresponding transition label appears, and that protected rank retention and ongoing placement do not display an inappropriate label.
- [ ] NOT RUN — At match end, press rematch/menu controls rapidly. Confirm one settlement, readable results and normal navigation.

## Evidence and boundaries

The rank-root sizing correction has merged according to task history. Interactive rechecking of that correction remains outstanding. The transition-label increment is commit d0f95e2 in open proposal mrg_0mvibi8vlb323a020; its interactive checks require integration first. Its recorded automated run reported 91 passing PlayMode tests, including seven transition cases. These historical results do not verify a later build.

For failures record the resolution, profile state, reproduction steps, screenshot reference and commit. Leave unexecuted checks NOT RUN.
