# Fairness tester round 2: style round-robin

Task: tsk_0mvg54t6u22af8c59 (brief: `Docs/FAIRNESS_BRIEF_ROUND2.md`). Round 1 results: `Docs/FAIRNESS_REPORT.md`.

Harness: `Assets/Scripts/Logic/StyleRoundRobin.cs` (built on `StyledSelfPlay`, which is unchanged). Tests: `Assets/Tests/EditMode/StyleRoundRobinTests.cs`.

Per-game stats: `Assets/Scripts/Logic/StyledGameRecorder.cs`. Tests: `Assets/Tests/EditMode/StyledGameRecorderTests.cs`.

Extended per-game CSV rows: `Assets/Scripts/Logic/StyledGameCsv.cs`. Tests: `Assets/Tests/EditMode/StyledGameCsvTests.cs`.

## Method

- Styles: `BotStyle.All` = Default, Positional, Hoarder, Spender, Attacker (see `BotStyle.cs`).
  - Default adds nothing to `Bot.Evaluate`, so it plays exactly like `Bot`.
  - Positional: +6 per own token in the centre column, minus the opponent's.
  - Hoarder: +25 per own unused power-up, and a -20 root bias against Queen and Cleaner moves.
  - Spender: -15 per own unused power-up, and a +20 root bias towards Queen and Cleaner moves.
  - Attacker: +4 per open-2/open-3 threat point (`StyledBot.ThreatPoints`), minus the opponent's.
- Every unordered pair of styles, plus each style against itself (mirror). Non-mirror pairings play every opening twice, once with each style as Red. Red always moves first, so Red wins = first-mover wins.
- Both sides search at the same depth. Ties between equal-best moves are broken by a seeded `System.Random` (game i uses seed 1000 + i; both colour orders reuse the same seeds). Same inputs give identical results.
- Openings: depth-2 run uses `StyledSelfPlay.TwoPlyOpenings()` (Normal/Queen/Cleaner, up to 441). Depth-3 run uses `ThreePlyNormalOpenings()` (up to 343).
- Ply cap 160. Capped games count as draws and are reported separately. "Repetition games" = games where some position occurred 3 or more times.
- Bot: uncached `StyledBot`. `CachedStyledBot` picks the same moves (see TranspositionTableTests) and can be swapped in later for speed.

## Per-game stats (brief items 6 and 7)

`StyledGameRecord` replays a game with the same opening, styles and seed as `StyledSelfPlay`, and its Result/Plies/Capped match `PlayGame` and `Run` (pinned by `StyledGameRecorderTests`). Each record can be written as one CSV row; the header is `StyledGameRecord.CsvHeader`:

| Column | Meaning |
| --- | --- |
| `red_style`, `blue_style` | Style names |
| `seed` | Tie-break seed (empty when unseeded) |
| `result` | `EvaluationResult` of the final position |
| `plies` | Total plies, including the opening moves |
| `draw_kind` | `None` (someone won), `Cap` (ply limit reached) or `Rules` (a draw the rules declared) |
| `final_move_kind` | Kind of the move that completed the winning line (empty for draws) |
| `red_queen_plies`, `red_cleaner_plies`, `blue_queen_plies`, `blue_cleaner_plies` | 1-based plies where that side used the power-up, separated by `;` |
| `winner_used_queen`, `winner_used_cleaner` | `1` if the winner used that power-up at least once, else `0` |

### Extended rows (`StyledGameCsv`)

`StyledGameCsv.Header` puts four columns in front of `StyledGameRecord.CsvHeader`; the rest of each row is exactly `record.ToCsvRow()`. `StyledGameCsv.Row(game, record, redDepth, blueDepth)` throws if the `StyledGame` and the record describe different games; `PlayRow` plays and records in one call.

| Column | Meaning |
| --- | --- |
| `opening` | Opening moves as `Kind@Column`, separated by `;` (`StyledGameCsv.OpeningCell`; `ParseOpening` reads it back) |
| `red_depth`, `blue_depth` | Search depth of each side |
| `max_repeats` | Highest number of times any position occurred in the game (`StyledGame.MaxRepeats`) |

## How to run

The long runs are `[Explicit]` and do not run in the normal EditMode pass. From the project root (verified for depth 2 on 2026-10-20: the result XML shows the test case with `runstate="Explicit"`, `result="Passed"`, about 31 s, and the table below in its output):

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.StyleRoundRobinTests.RoundRobin_Depth2_TwoPlyOpenings" -testResults TestResults/fairness_round2.xml -logFile TestResults/fairness_round2.log
```

For depth 3, use `RoundRobin_Depth3_ThreePlyNormalOpenings` instead (much slower). Output: `TestResults/fairness_round2_depth2.txt` / `fairness_round2_depth3.txt`, a Markdown table that can be pasted below.

## Observed results

### Depth 2, two-ply openings (run 2026-10-20)

Copied unchanged from the test output in `TestResults/fairness_round2.xml`.

Style round-robin: depth 2, 441 openings, seedBase 1000, ply cap 160

| A | B | Games | A wins | B wins | Draws (capped) | First-mover wins | Second-mover wins | Repetition games |
|---|---|---|---|---|---|---|---|---|
| Default | Default | 441 | 197 | 136 | 108 (5) | 197 (44.7 %) | 136 (30.8 %) | 7 |
| Default | Positional | 882 | 341 | 343 | 198 (8) | 391 (44.3 %) | 293 (33.2 %) | 8 |
| Default | Hoarder | 882 | 215 | 454 | 213 (10) | 403 (45.7 %) | 266 (30.2 %) | 12 |
| Default | Spender | 882 | 525 | 169 | 188 (9) | 362 (41.0 %) | 332 (37.6 %) | 13 |
| Default | Attacker | 882 | 342 | 321 | 219 (13) | 393 (44.6 %) | 270 (30.6 %) | 15 |
| Positional | Positional | 441 | 188 | 150 | 103 (2) | 188 (42.6 %) | 150 (34.0 %) | 2 |
| Positional | Hoarder | 882 | 221 | 460 | 201 (10) | 412 (46.7 %) | 269 (30.5 %) | 11 |
| Positional | Spender | 882 | 554 | 188 | 140 (12) | 364 (41.3 %) | 378 (42.9 %) | 12 |
| Positional | Attacker | 882 | 358 | 329 | 195 (11) | 390 (44.2 %) | 297 (33.7 %) | 11 |
| Hoarder | Hoarder | 441 | 171 | 104 | 166 (6) | 171 (38.8 %) | 104 (23.6 %) | 8 |
| Hoarder | Spender | 882 | 648 | 89 | 145 (10) | 383 (43.4 %) | 354 (40.1 %) | 12 |
| Hoarder | Attacker | 882 | 452 | 207 | 223 (16) | 404 (45.8 %) | 255 (28.9 %) | 18 |
| Spender | Spender | 441 | 154 | 168 | 119 (8) | 154 (34.9 %) | 168 (38.1 %) | 9 |
| Spender | Attacker | 882 | 167 | 520 | 195 (8) | 353 (40.0 %) | 334 (37.9 %) | 11 |
| Attacker | Attacker | 441 | 198 | 133 | 110 (4) | 198 (44.9 %) | 133 (30.2 %) | 5 |
| All | All | 11025 | - | - | 2523 (132) | 4763 (43.2 %) | 3739 (33.9 %) | 154 |

Derived from the table (arithmetic only, cross-style games only, 3528 games per style, score = wins + half of draws):

| Style | Wins | Losses | Draws | Score |
|---|---|---|---|---|
| Hoarder | 2014 | 732 | 782 | 68.2 % |
| Positional | 1476 | 1318 | 734 | 52.2 % |
| Default | 1423 | 1287 | 818 | 51.9 % |
| Attacker | 1377 | 1319 | 832 | 50.8 % |
| Spender | 613 | 2247 | 668 | 26.8 % |

Other figures from the table:

- Decisive games: 8502 of 11025. Red (first mover) won 4763 of them (56.0 %).
- Draws: 2523 (22.9 %), of which 132 hit the ply cap. 154 games had a position repeated 3 or more times.
- In 4 of 5 mirror pairings Red won more than Blue. The exception is Spender vs Spender (154 Red, 168 Blue).

### Depth 3, three-ply Normal openings

Not run yet. Needs owner approval of the exact depth-3 command (allow_command).

## Inferences

These are inferences from bot-vs-bot play at depth 2. They do not prove anything about perfect play or human play. Only the forced-win probe (separate subtask) can prove a result. The puzzle checks in `PuzzleVerifier` cover only the 10 `PuzzlePack` scenarios and are not an opening probe.

1. **First-move edge (likely real for these bots, size unknown for humans).** Red won 56 % of decisive games across every style pairing and in 4 of 5 mirrors. This supports Design 3's alternating starts / best-of-2 for ranked play, regardless of whether the edge survives deeper search.
2. **Power-ups are not a free win.** The style that spends Queen and Cleaner eagerly (Spender) scored worst against every other style, and the style that holds them (Hoarder) scored best. At depth 2, using a power-up early seems to cost more than it gains. This argues against power-ups being a luck-like or dominant mechanic, but it may also mean the bots time them badly; a human may do better.
3. **Style differences outweigh evaluation tweaks.** Default, Positional and Attacker are within about 1.5 points of each other; only the power-up policy moved the score a lot.
4. **Few endless games.** Only 132 of 11025 games (1.2 %) reached the 160-ply cap, so the shift rule does not commonly produce endless loops at this depth.

Open questions: whether inferences 1 and 2 hold at depth 3 (pending run), and whether the opening position is a forced win (forced-win probe).
