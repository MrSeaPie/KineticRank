# Fairness round 2: depth-4 style round-robin

Source: `TestResults/fairness_round2_depth4.txt` (exit code 0).
Run command: `[Explicit]` test `KineticRank.Tests.EditMode.StyleRoundRobinTests.RoundRobin_Depth4_TwoPlyNormalOpenings` (EditMode, `-testFilter`, results to `TestResults/fairness_round2_depth4.xml/.log`).

Settings: depth 4, 49 openings (two-ply, normal drops only), seedBase 1000, ply cap 160, 1225 games.

## Per-pairing table (copied unchanged)

| A | B | Games | A wins | B wins | Draws (capped) | First-mover wins | Second-mover wins | Repetition games |
|---|---|---|---|---|---|---|---|---|
| Default | Default | 49 | 23 | 10 | 16 (0) | 23 (46.9 %) | 10 (20.4 %) | 0 |
| Default | Positional | 98 | 39 | 24 | 35 (0) | 50 (51.0 %) | 13 (13.3 %) | 0 |
| Default | Hoarder | 98 | 27 | 53 | 18 (0) | 62 (63.3 %) | 18 (18.4 %) | 0 |
| Default | Spender | 98 | 82 | 4 | 12 (0) | 50 (51.0 %) | 36 (36.7 %) | 0 |
| Default | Attacker | 98 | 37 | 24 | 37 (0) | 38 (38.8 %) | 23 (23.5 %) | 0 |
| Positional | Positional | 49 | 26 | 10 | 13 (0) | 26 (53.1 %) | 10 (20.4 %) | 0 |
| Positional | Hoarder | 98 | 24 | 54 | 20 (1) | 54 (55.1 %) | 24 (24.5 %) | 1 |
| Positional | Spender | 98 | 83 | 7 | 8 (0) | 44 (44.9 %) | 46 (46.9 %) | 0 |
| Positional | Attacker | 98 | 24 | 40 | 34 (0) | 48 (49.0 %) | 16 (16.3 %) | 0 |
| Hoarder | Hoarder | 49 | 30 | 9 | 10 (0) | 30 (61.2 %) | 9 (18.4 %) | 0 |
| Hoarder | Spender | 98 | 88 | 2 | 8 (0) | 45 (45.9 %) | 45 (45.9 %) | 0 |
| Hoarder | Attacker | 98 | 51 | 24 | 23 (0) | 59 (60.2 %) | 16 (16.3 %) | 0 |
| Spender | Spender | 49 | 12 | 18 | 19 (0) | 12 (24.5 %) | 18 (36.7 %) | 0 |
| Spender | Attacker | 98 | 7 | 81 | 10 (0) | 52 (53.1 %) | 36 (36.7 %) | 0 |
| Attacker | Attacker | 49 | 15 | 13 | 21 (0) | 15 (30.6 %) | 13 (26.5 %) | 0 |
| All | All | 1225 | - | - | 284 (1) | 608 (49.6 %) | 333 (27.2 %) | 1 |

## Figures derived from the table

- First mover: 608 wins, second mover: 333, draws: 284 (23.2 %). The first mover takes **64.6 % of decisive games** (608 / 941).
- Cross-style score (wins + half draws, over the 392 games each style plays against the other four, mirrors excluded):

| Style | W | D | Score |
|---|---|---|---|
| Hoarder | 246 | 69 | **71.6 %** |
| Default | 185 | 102 | 60.2 % |
| Attacker | 169 | 104 | 56.4 % |
| Positional | 155 | 97 | 51.9 % |
| Spender | 20 | 38 | 9.9 % |

- Hoarder beats every other style head-to-head: Default 53-27, Positional 54-24, Attacker 51-24, Spender 88-2. The order is transitive; there is no rock-paper-scissors cycle.

## Trend across depths (different opening sets: read with care)

| Depth | Openings | First-mover share of decisive games | Top cross-style score |
|---|---|---|---|
| 2 | 441 two-ply (all move types) | 56.0 % | Hoarder 68.2 % |
| 3 | same 441 two-ply | 63.4 % | Default 57.2 % (Hoarder 53.6 %) |
| 3 | 343 three-ply, normal drops only | 58.6 % | Hoarder 56.0 % |
| 4 | 49 two-ply, normal drops only | 64.6 % | Hoarder 71.6 % |

## Inferences (not proven)

1. The first-move edge does not shrink with depth: about 63-65 % of decisive games at depths 3 and 4. This supports the alternating-starts decision already taken.
2. Hoarder pulls clearly ahead again at depth 4. The head-to-head margins (about 2:1 in decisive games against each non-Spender style, 98 games per pairing) are too large to be noise alone.
3. Possible confound: in the 441-opening sets, two thirds of openings force a power-up in plies 1-2, so a Hoarder cannot hoard there. On normal-drop-only openings (the depth-3 three-ply run and this run) Hoarder leads both times. The depth-3 drop in Hoarder's score may be an opening-set effect, not a depth effect. To separate the two, rerun depths 2 and 3 on these same 49 normal two-ply openings (cheap: under a minute at depth 2).
4. Spender's 9.9 % confirms that spending power-ups early is a losing plan at every depth tested.
5. Per the owner's rule, a power-up-holding style pulling ahead triggers a proposal to test a "use it by move N or lose it" variant in the tester only. No game change is proposed.
