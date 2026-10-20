# Fairness tester round 2: style round-robin

Task: tsk_0mvg54t6u22af8c59 (brief: `Docs/FAIRNESS_BRIEF_ROUND2.md`). Round 1 results: `Docs/FAIRNESS_REPORT.md`.

Harness: `Assets/Scripts/Logic/StyleRoundRobin.cs` (built on `StyledSelfPlay`, which is unchanged). Tests: `Assets/Tests/EditMode/StyleRoundRobinTests.cs`.

## Method

- Styles: `BotStyle.All` = Default, Positional, Hoarder, Spender (see `BotStyle.cs`). The brief's Attacker style is not implemented yet.
- Every unordered pair of styles, plus each style against itself (mirror). Non-mirror pairings play every opening twice, once with each style as Red. Red always moves first, so Red wins = first-mover wins.
- Both sides search at the same depth. Ties between equal-best moves are broken by a seeded `System.Random` (game i uses seed 1000 + i; both colour orders reuse the same seeds). Same inputs give identical results.
- Openings: depth-2 run uses `StyledSelfPlay.TwoPlyOpenings()` (Normal/Queen/Cleaner, up to 441). Depth-3 run uses `ThreePlyNormalOpenings()` (up to 343).
- Ply cap 160. Capped games count as draws and are reported separately. "Repetition games" = games where some position occurred 3 or more times.
- Bot: uncached `StyledBot`. `CachedStyledBot` picks the same moves (see TranspositionTableTests) and can be swapped in later for speed.

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
