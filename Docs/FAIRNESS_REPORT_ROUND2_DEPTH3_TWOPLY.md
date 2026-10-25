# Fairness round 2: depth 3 on the two-ply openings

Part of tsk_0mvg54t6u22af8c59. This section goes with Docs/FAIRNESS_REPORT_ROUND2.md. It isolates the effect of search depth by re-running the depth-2 opening set (441 two-ply openings) at depth 3.

## How to reproduce

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.StyleRoundRobinTests.RoundRobin_Depth3_TwoPlyOpenings" -testResults TestResults/fairness_round2_depth3_twoply.xml -logFile TestResults/fairness_round2_depth3_twoply.log
```

The test is [Explicit] and writes TestResults/fairness_round2_depth3_twoply.txt. The run exited with code 0.

## Observed: per-pairing table (copied unchanged from TestResults/fairness_round2_depth3_twoply.txt)

Style round-robin: depth 3, 441 openings, seedBase 1000, ply cap 160

| A | B | Games | A wins | B wins | Draws (capped) | First-mover wins | Second-mover wins | Repetition games |
|---|---|---|---|---|---|---|---|---|
| Default | Default | 441 | 291 | 145 | 5 (0) | 291 (66.0 %) | 145 (32.9 %) | 0 |
| Default | Positional | 882 | 454 | 415 | 13 (0) | 592 (67.1 %) | 277 (31.4 %) | 0 |
| Default | Hoarder | 882 | 460 | 410 | 12 (0) | 545 (61.8 %) | 325 (36.8 %) | 0 |
| Default | Spender | 882 | 611 | 244 | 27 (0) | 525 (59.5 %) | 330 (37.4 %) | 0 |
| Default | Attacker | 882 | 461 | 411 | 10 (1) | 592 (67.1 %) | 280 (31.7 %) | 1 |
| Positional | Positional | 441 | 293 | 143 | 5 (0) | 293 (66.4 %) | 143 (32.4 %) | 0 |
| Positional | Hoarder | 882 | 453 | 416 | 13 (1) | 545 (61.8 %) | 324 (36.7 %) | 1 |
| Positional | Spender | 882 | 610 | 253 | 19 (0) | 522 (59.2 %) | 341 (38.7 %) | 0 |
| Positional | Attacker | 882 | 440 | 427 | 15 (0) | 600 (68.0 %) | 267 (30.3 %) | 0 |
| Hoarder | Hoarder | 441 | 278 | 161 | 2 (0) | 278 (63.0 %) | 161 (36.5 %) | 0 |
| Hoarder | Spender | 882 | 614 | 249 | 19 (1) | 496 (56.2 %) | 367 (41.6 %) | 1 |
| Hoarder | Attacker | 882 | 423 | 446 | 13 (0) | 550 (62.4 %) | 319 (36.2 %) | 0 |
| Spender | Spender | 441 | 224 | 209 | 8 (0) | 224 (50.8 %) | 209 (47.4 %) | 0 |
| Spender | Attacker | 882 | 249 | 612 | 21 (0) | 523 (59.3 %) | 338 (38.3 %) | 0 |
| Attacker | Attacker | 441 | 299 | 137 | 5 (1) | 299 (67.8 %) | 137 (31.1 %) | 1 |
| All | All | 11025 | - | - | 187 (4) | 6875 (62.4 %) | 3963 (35.9 %) | 4 |

## Observed: derived figures (computed from the table)

- Decisive games: 10838. The first mover won 63.4 % of them.
- Cross-style score (wins + half draws over the 3528 non-mirror games per style): Default 57.2 %, Positional 55.2 %, Attacker 54.6 %, Hoarder 53.6 %, Spender 29.4 %.
- First-mover win rate in mirror matches: Attacker 67.8 %, Positional 66.4 %, Default 66.0 %, Hoarder 63.0 %, Spender 50.8 %.
- The order is transitive (Default > Positional > Attacker > Hoarder > Spender, head to head). There is no rock-paper-scissors cycle.

## Observed: same openings, depth 2 vs depth 3

| Measure | Depth 2 (s670) | Depth 3 (this run) |
|---|---|---|
| Draws | 22.9 % | 1.7 % |
| First mover, share of decisive games | 56 % | 63.4 % |
| Top style (cross-style score) | Hoarder 68.2 % | Default 57.2 % |
| Hoarder | 68.2 % | 53.6 % |
| Spender | 26.8 % | 29.4 % |

The openings, seeds and ply cap are the same, so only the search depth changed.

## Inferences (not proven)

1. Hoarder's dominance at depth 2 was a depth artefact. At depth 3 the top four styles are within about 4 points of each other. Default's 51-53 % head-to-head edges are about 1-1.5 sigma on roughly 870 decisive games each, so they are not clearly significant.
2. Deeper search turns depth-2 draws into decisive games, and most of those go to the first mover. If this trend continues at depth 4, the first-move edge is a structural property of the ruleset and not a weakness of the bots. That would strengthen the case for alternating starts or best-of-2 matches in ranked play. The depth-4 run (apr_0mvgze5eb85398a72) would test this and is waiting on owner approval.
3. Spender is weak at both depths (about 27-29 %). Possible responses: rebalance its weights, use it only as the entry tier of the solo ladder (Pine, as Agent-03 plans), or document it as a deliberately weak style.
4. Spender mirrors are nearly even (50.8 % vs 47.4 %). Inference: spending power-ups early may blunt the first-move edge. This is untested and needs its own probe before it shapes any rule.
