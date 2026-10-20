# Fairness tester round 2: style round-robin

Task: tsk_0mvg54t6u22af8c59 (brief: `Docs/FAIRNESS_BRIEF_ROUND2.md`). Round 1 results: `Docs/FAIRNESS_REPORT.md`.

## Summary for the owner (depth 2 and depth 3; the forced-win probe and 4v4 are still pending)

Everything below comes from bot-vs-bot play at depths 2 and 3. It describes these bots, not perfect play or human play. Nothing here is proven; the forced-win probe is the only part of the brief that can prove a result.

- **Is there a first-player edge?** Yes, a small one for these bots, and it did not shrink with deeper search. Red (who always moves first) won 56.0 % of decisive games at depth 2 (4763 of 8502) and 58.6 % at depth 3. Both are above your 45-55 % target.
- **Is there a dominant style?** Technically yes at both depths: Hoarder (saves Queen and Cleaner) beat every other style head-to-head, and we saw no rock-paper-scissors cycle. But at depth 3 its margin over Default, Positional and Attacker shrank to about 52-53 % (from roughly 67-72 % at depth 2), and its overall score fell from 68.2 % to 56.0 %. Spender (uses power-ups early) lost to everyone at both depths (26.8 % and 34.2 %). My reading is that spending power-ups early is a weak policy, not that holding them is overpowered. This still misses your "no style beats every other style" target, but only narrowly at depth 3.
- **Do Queen or Cleaner decide too many games?** Probably not directly. At depth 2 a Queen drop completed the winning line in 585 of 8502 decisive games (6.9 %). Almost every side used a Queen at some point, so "the winner used a Queen" says little. What matters is *when* power-ups are used.
- **Smallest change I'd recommend:** alternate who starts, or play ranked as best-of-2 with each player starting once (Design 3). This removes the first-move edge from ranked results without changing any rule. I would not change the power-ups based on this data. Draws are rare at depth 3 (45 of 8575, 0.5 %), and only 4 games hit the ply cap. Real matches still have no move limit, and whether that should change is your decision.

Harness: `Assets/Scripts/Logic/StyleRoundRobin.cs` (built on `StyledSelfPlay`, which is unchanged). Tests: `Assets/Tests/EditMode/StyleRoundRobinTests.cs`.

Per-game stats: `Assets/Scripts/Logic/StyledGameRecorder.cs`. Tests: `Assets/Tests/EditMode/StyledGameRecorderTests.cs`.

Extended per-game CSV rows: `Assets/Scripts/Logic/StyledGameCsv.cs`. Tests: `Assets/Tests/EditMode/StyledGameCsvTests.cs`.

CSV run and power-up/draw aggregation: `Assets/Tests/EditMode/FairnessCsvRunTests.cs` (`PowerUpSummary`).

## Method

- Styles: `BotStyle.All` = Default, Positional, Hoarder, Spender, Attacker (see `BotStyle.cs`).
  - Default adds nothing to `Bot.Evaluate`, so it plays exactly like `Bot`.
  - Positional: +6 per own token in the centre column, minus the opponent's.
  - Hoarder: +25 per own unused power-up, and a -20 root bias against Queen and Cleaner moves.
  - Spender: -15 per own unused power-up, and a +20 root bias towards Queen and Cleaner moves.
  - Attacker: +4 per open-2/open-3 threat point (`StyledBot.ThreatPoints`), minus the opponent's.
- Every unordered pair of styles, plus each style against itself (mirror). Non-mirror pairings play every opening twice, once with each style as Red. Red always moves first, so Red wins = first-mover wins.
- Both sides search at the same depth. Ties between equal-best moves are broken by a seeded `System.Random` (game i uses seed 1000 + i; both colour orders reuse the same seeds). Same inputs give identical results.
- Openings: depth-2 run uses `StyledSelfPlay.TwoPlyOpenings()` (Normal/Queen/Cleaner, up to 441). Depth-3 run uses `ThreePlyNormalOpenings()` (up to 343). The opening moves are fixed; the bots only choose from the first ply after the opening.
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

Depth 3 (verified on 2026-10-20: `result="Passed"`, about 168 s):

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.StyleRoundRobinTests.RoundRobin_Depth3_ThreePlyNormalOpenings" -testResults TestResults/fairness_round2_depth3.xml -logFile TestResults/fairness_round2_depth3.log
```

Output: `TestResults/fairness_round2_depth2.txt` / `fairness_round2_depth3.txt`, a Markdown table that can be pasted below.

Per-game CSV plus summary (round-robin table and power-up/draw tables), depth 2. Verified on 2026-10-20 (exit 0, summary written):

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.FairnessCsvRunTests.Csv_Depth2_TwoPlyOpenings" -testResults TestResults/fairness_round2_csv.xml -logFile TestResults/fairness_round2_csv.log
```

Batch runs may touch `ProjectSettings/ProjectSettings.asset`; restore it with `git restore -- ProjectSettings/ProjectSettings.asset` before committing.

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
- Head-to-head, Hoarder won more games than its opponent in all 4 of its cross-style pairings, and Spender lost all 4 of its cross-style pairings. Default, Positional and Attacker each beat Spender and lost to Hoarder; among themselves the margins are small (Default-Positional 341-343, Default-Attacker 342-321, Positional-Attacker 358-329).

### Depth 2: power-ups and draw breakdown (run 2026-10-20)

From `PowerUpSummary` over the per-game CSV of the same 11025 games (`FairnessCsvRunTests.Csv_Depth2_TwoPlyOpenings`). Percentages are arithmetic on the counts.

| Draw kind | Games |
|---|---|
| None (decisive) | 8502 |
| Rules (declared by the rules) | 2391 |
| Cap (160-ply limit) | 132 |

| Decisive games (8502) | Games | Share |
|---|---|---|
| Winning line completed by a Queen drop | 585 | 6.9 % |
| Winner used a Queen at least once | 7421 | 87.3 % |
| Winner used a Cleaner at least once | 6370 | 74.9 % |

| Style | Sides that used a Queen | Mean first Queen ply |
|---|---|---|
| Spender | 100 % | 3.3 |
| Attacker | 97.5 % | about 7.4 |
| Default | 97.1 % | about 7.4 |
| Positional | 96.4 % | about 7.4 |
| Hoarder | 76.7 % | 10.3 |

- Of 22050 sides (2 per game), 14700 (66.7 %) first used a power-up in plies 1-2, and 659 (3.0 %) never used one. These plies 1-2 uses are all forced by the openings (see inference 5), and the mean first-Queen plies above include them.
- Repetition games (some position 3 or more times): 154 (1.4 %), from the round-robin table above.

### Depth 3, three-ply Normal openings (run 2026-10-20)

`RoundRobin_Depth3_ThreePlyNormalOpenings`: `TestResults/fairness_round2_depth3.xml` shows `result="Passed"` (total 1, passed 1, 167.5 s). 343 openings, seedBase 1000, ply cap 160, 8575 games. The figures below come from the test output (`fairness_round2_depth3.txt`). The per-pairing table is not copied here yet.

- Draws: 45 (0.5 %), of which 4 hit the ply cap.
- First mover won 5002 games (58.3 % of all games, 58.6 % of decisive games); second mover won 3528.
- First-mover win share in the mirror pairings ranges from 47.8 % (Positional vs Positional) to 73.5 % (Spender vs Spender).

Cross-style scores (win = 1, draw = 0.5, mirrors excluded, 2744 games per style):

| Style | Score (depth 3) | Score (depth 2, for comparison) |
|---|---|---|
| Hoarder | 56.0 % | 68.2 % |
| Default | 54.8 % | 51.9 % |
| Positional | 53.1 % | 52.2 % |
| Attacker | 51.9 % | 50.8 % |
| Spender | 34.2 % | 26.8 % |

Head-to-head for Hoarder at depth 3: 52.6 % vs Default, 51.7 % vs Positional, 53.0 % vs Attacker, 66.5 % vs Spender. Spender scores 33-36 % against every style. So again there is no rock-paper-scissors cycle, but Hoarder's edge over the three middle styles is narrow.

**Caveat:** depth 2 and depth 3 use different opening sets (two-ply openings that include forced power-up drops vs three-ply Normal-only openings). Changes between the two runs (for example, draws falling from 22.9 % to 0.5 %) cannot be attributed to search depth alone.

## Inferences

These are inferences from bot-vs-bot play at depths 2 and 3. They do not prove anything about perfect play or human play. Only the forced-win probe (separate subtask) can prove a result. The puzzle checks in `PuzzleVerifier` cover only the 10 `PuzzlePack` scenarios and are not an opening probe.

1. **First-move edge (likely real for these bots, size unknown for humans).** Red won 56.0 % of decisive games at depth 2 and 58.6 % at depth 3. The edge did not shrink with deeper search. This supports Design 3's alternating starts / best-of-2 for ranked play.
2. **Power-ups are not a free win.** The style that spends Queen and Cleaner eagerly (Spender) scored worst against every style at both depths. Hoarder's lead over the middle styles shrank from about 68 % to 56 % overall at depth 3. That fits "early spending is a mistake" better than "holding power-ups is overpowered". Bots may still time power-ups badly, and a human may do better.
3. **Style differences outweigh evaluation tweaks.** Default, Positional and Attacker stay within about 3 points of each other at both depths. Only the power-up policy moved the score a lot.
4. **Few endless games.** 132 of 11025 games (1.2 %) at depth 2 and 4 of 8575 at depth 3 reached the 160-ply cap. The shift rule does not commonly produce endless loops for these bots.
5. **The plies 1-2 power-up count at depth 2 is forced by the openings (verified from code).** `StyledSelfPlay.TwoPlyOpenings()` fixes plies 1-2 in every game, and the bot never chooses those plies. With 7 columns x 3 kinds per ply, 14 of 21 choices per side are Queen or Cleaner. That gives 441 openings x 25 pairings = 11025 games and exactly 14700 of 22050 sides.
6. **"Winner used a Queen" is not evidence that the Queen decides games.** Nearly every side used one, and Hoarder used it least yet won most. Only 6.9 % of depth-2 wins were completed by the Queen drop itself.

Open questions: whether the opening position is a forced win (forced-win probe), and whether the depth-3 trends continue at 4v4 (still to do from the brief, plus 6v6 if time allows). A depth-3 run with the two-ply power-up openings would separate the depth effect from the opening-set effect.
