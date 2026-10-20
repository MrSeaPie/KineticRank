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
