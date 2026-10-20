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

The long runs are `[Explicit]` and do not run in the normal EditMode pass. From the project root (expected to work via the test filter; not yet verified on this machine):

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.StyleRoundRobinTests.RoundRobin_Depth2_TwoPlyOpenings" -testResults TestResults/fairness_round2.xml -logFile TestResults/fairness_round2.log
```

For depth 3, use `RoundRobin_Depth3_ThreePlyNormalOpenings` instead (much slower). Output: `TestResults/fairness_round2_depth2.txt` / `fairness_round2_depth3.txt`, a Markdown table that can be pasted below.

## Observed results

Not run yet. Running the long tests needs a new allow_command entry (owner approval of the exact command above).

## Inferences

None yet. Any conclusion drawn from these tables will be labelled as an inference: bot-vs-bot play at depth 2-3 does not prove anything about perfect play or human play. Only the forced-win probe (separate subtask) can prove a result.
