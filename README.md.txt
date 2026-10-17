# KineticRank

A competitive local two-player Connect Four variant with column shifts, Queen and Cleaner tokens, and persistent local Elo ratings.

## Open the project

Use Unity **2022.3.62f1**, the editor version recorded in `ProjectSettings/ProjectVersion.txt`.

Open `Assets/Scenes/MainMenu.unity` for the menu or `Assets/Scenes/Match.unity` for the match scene, then enter Play mode.

## Gameplay

Players alternate RED and BLUE turns on a 7 × 6 board. Dropping into a full column removes its bottom token and shifts the remaining tokens down. Queen tokens flip colours in a column; Cleaner tokens clear a column.

Moves resolve before another move can begin. Four-in-a-row is evaluated after resolution. If both colours have a winning line, the match is a draw. A terminal result locks further moves and records one ranked result for the match.

## Controls

| Action | Input |
|--------|-------|
| Drop selected token | Left-click a column |
| Rematch | Enter |
| Toggle rating HUD | H |
| Reset local ranked statistics | Backspace |

Ratings and statistics are stored locally through `SaveManager`. These local ratings do not constitute an online competitive ranking service.

## Verification status

The integrated move-resolution changes previously passed 36 EditMode tests and 5 PlayMode tests. After adding match-recording and disk-persistence regressions, the latest observed PlayMode XML reports 10 passed and 0 failed. These results describe separate observed runs, not a fresh run of both suites against the current revision.

Interactive scene acceptance remains **NOT RUN**. Automated test results do not establish that scene presentation, controls, audio, and complete player flows work correctly in interactive play. Follow `Docs/SCENE_PLAY_CHECKLIST.md` and record actual results before release.

## Remaining work

Complete interactive scene acceptance, address any resulting defects, and review release readiness. Publishing, spending, and external account actions require owner approval.
