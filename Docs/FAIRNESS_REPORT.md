# First-player fairness self-play report (Design 3)

Task: tsk_0mveb7hcr30809184. Harness: `Assets/Scripts/Logic/SelfPlay.cs` (KineticRank.Logic), test: `Assets/Tests/EditMode/SelfPlayTests.cs`.

## Method

- Bot plays bot using `Bot.ChooseMove` (negamax + alpha-beta) over `GameState`. No RNG; every run gives identical results.
- Variety comes only from forced openings: all 49 two-ply openings (Red drops a Normal token in any column, then Blue does the same).
- Red always moves first, so Red wins = first-player wins.
- Ply cap 120. A game that hits the cap is counted as a draw and reported as capped.
- Raw output is written to `TestResults/fairness_report.txt` by the EditMode run.

## Observed results (EditMode run, commit 34c5fbd)

| Red depth | Blue depth | Games | First-player (Red) wins | Second-player (Blue) wins | Draws (capped) | Avg plies |
|---|---|---|---|---|---|---|
| 1 | 1 | 49 | 23 (46.9 %) | 26 (53.1 %) | 0 (0.0 %, 0 capped) | 17.4 |
| 2 | 2 | 49 | 24 (49.0 %) | 6 (12.2 %) | 19 (38.8 %, 0 capped) | 20.9 |
| 1 | 2 | 49 | 21 (42.9 %) | 24 (49.0 %) | 4 (8.2 %, 4 capped) | 27.0 |
| 2 | 1 | 49 | 48 (98.0 %) | 1 (2.0 %) | 0 (0.0 %, 0 capped) | 22.7 |

## Inferences (not proven)

- At equal depth 1 the split is close to even. At equal depth 2 the first player wins 4x as often as the second (24 vs 6), which suggests a first-player advantage once the bot looks ahead.
- The deeper bot as first player wins 98.0 %; the deeper bot as second player wins only 49.0 %. Moving first appears to amplify the stronger side's edge.
- Limits: only 49 games per pairing, shallow depths (1-2), and openings restricted to Normal tokens. Bot play is not human play. Deeper pairings (3v3, 4v4) and three-ply openings (343) would strengthen or overturn these readings.

## Options (ruleset changes need the owner's OK)

1. Alternating starts: in ranked/rematch series, the starting colour alternates each game. Cheapest change.
2. Best-of-2 (or even-length series): each player starts once; a 1-1 split is a draw.
3. Swap rule: after the first move, the second player may take over that position. Strongest balancing, but adds a decision new players must learn.

Recommendation: alternating starts (option 1) now, then re-measure with deeper pairings before considering a swap rule.

## Round 2 (bot styles, larger runs)

See `Docs/FAIRNESS_REPORT_ROUND2.md` (task tsk_0mvg54t6u22af8c59). In short: across 5 bot styles, Red won 56.0 % of decisive games at depth 2 (11025 games) and 58.6 % at depth 3 (8575 games). The first-move edge did not shrink with deeper search, which still supports option 1 or 2 above.

## Traditional mode (no power-ups) vs standard, same 49 openings

Task: tsk_0mvh8zd5o3c208a07 (Docs/RANKED_PHASE1_BRIEF.md section 6). No rules code or harness baseline changed.

### Method

- Style round-robin (Default, Positional, Hoarder, Spender, Attacker), every ordered pairing, equal depth for both sides.
- Openings: the same 49 two-ply Normal-drop openings for both modes. seedBase 1000, ply cap 160 (capped games count as draws). 1225 games per run.
- Traditional starts from `GameState.FromGrid` with all four power-up flags unavailable, so both bots can only play Normal moves.
- All four runs are `[Explicit]` tests in `StyleRoundRobinTests` (category FairnessLong), run on branch commit e93253f; each run Passed with failed=0.
  - Standard: `RoundRobin_Depth2_TwoPlyNormalOpenings`, `RoundRobin_Depth3_TwoPlyNormalOpenings`
  - Traditional: `RoundRobin_Traditional_Depth2_TwoPlyNormalOpenings`, `RoundRobin_Traditional_Depth3_TwoPlyNormalOpenings`
- Run one with: `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.StyleRoundRobinTests.<name>" -testResults TestResults/<file>.xml -logFile TestResults/<file>.log`

### Observed results (all styles combined)

| Mode | Depth | Games | First-mover wins | Second-mover wins | Draws (capped) | Repetition games | First mover's share of decisive games |
|---|---|---|---|---|---|---|---|
| Standard | 2 | 1225 | 549 (44.8 %) | 395 (32.2 %) | 281 (7) | 8 | 58.2 % |
| Traditional | 2 | 1225 | 466 (38.0 %) | 423 (34.5 %) | 336 (48) | 63 | 52.4 % |
| Standard | 3 | 1225 | 772 (63.0 %) | 450 (36.7 %) | 3 (0) | 0 | 63.2 % |
| Traditional | 3 | 1225 | 582 (47.5 %) | 492 (40.2 %) | 151 (0) | 0 | 54.2 % |

For reference, standard depth 4 on the same 49 openings gave the first mover 64.6 % of decisive games (Round 2 report).

Other observations:

- In Traditional, the Default, Hoarder and Spender rows are identical at both depths. That fits: these styles differ only in how they use power-ups, so with none available they play the same moves.
- Standard mode at depth 3 almost never draws (3 of 1225). Traditional at depth 3 draws 12.3 % of games.
- Traditional depth 2 has many capped and repetition games (48 capped, 63 repetitions); standard depth 2 has few (7, 8).

### Inferences (not proven)

- The first-move edge exists in both modes and grows from depth 2 to depth 3 in both, but it is clearly smaller without power-ups (about 52-54 % vs 58-63 % of decisive games). Power-ups seem to amplify the first mover's edge rather than create it.
- Rough significance (binomial, treating games as independent): standard d2 z = 5.0, standard d3 z = 9.2, Traditional d2 z = 1.4 (not significant at 5 %), Traditional d3 z = 2.7. The games are not truly independent: three styles collapse to one in Traditional and all runs share the same 49 openings, so the effective sample is smaller than 1225 and Traditional's numbers are weaker evidence than they look.
- Limits: bot play, shallow depths, Normal-only two-ply openings. Plain Connect Four is a known first-player win under perfect play, so deeper Traditional searches would likely show a larger edge.
- Practical reading: alternating starts (Owner decision 1, already merged as RematchTracker) suit both modes. Traditional does not need a different fairness rule.
