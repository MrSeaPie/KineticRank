# Brief: KineticRank fairness tester, round 2

**From:** the owner
**For:** the team (Agent-02 wrote `GameState`, `Bot` and `SelfPlay`, so is the natural first holder)
**Type:** pure logic + EditMode tests, script-only. No scene or prefab changes.

## Why

I want KineticRank to play like chess, not like solved Connect Four: several strategies should work, the
better player should usually win, and nobody should have one opening or power-up trick that always wins.
`Docs/FAIRNESS_REPORT.md` was a good first look, but it only used 49 games per pairing, bots that look
1-2 moves ahead, and openings without Queen or Cleaner. At depth 2 vs 2 it already hints at a first-player
edge (Red 24, Blue 6, 19 draws). This round should confirm or overturn that and test whether different
styles of play are viable.

## What already exists (build on it, don't replace it)

- `Assets/Scripts/Logic/GameState.cs`: the rules, matching `BoardManager` (recycling, Queen, Cleaner).
- `Assets/Scripts/Logic/Bot.cs`: negamax + alpha-beta, deterministic. Solo tiers use it (depths 1/2/4/6/8).
- `Assets/Scripts/Logic/SelfPlay.cs` + `Assets/Tests/EditMode/SelfPlayTests.cs`: the bot-vs-bot harness
  and the report test that writes `TestResults/fairness_report.txt`.

## What to build

1. **Bot styles.** Add a few evaluation profiles as a new code path, for example:
   - *Balanced*: today's weights.
   - *Hoarder*: values unused Queen/Cleaner highly, so it saves them for a critical moment.
   - *Spender*: values them low, so it uses them early.
   - *Centre*: bonus for tokens in the middle columns.
   - *Attacker*: weights its own open 2s/3s above blocking the opponent's.
2. **Seeded variety.** An option to pick at random among moves within a small margin of the best score,
   using a seeded `System.Random`. Log the seed so any game can be replayed exactly.
3. **Bigger opening sets.** Keep the 49 normal two-move openings, and add:
   - all 441 two-move openings where either move may be Normal, Queen or Cleaner;
   - the 343 normal three-move openings.
4. **Deeper pairings.** Equal depth 3v3 and 4v4, plus 6v6 if the time per move allows. Record the average
   time per move. If it is too slow, a transposition table is fine, as long as results stay identical.
5. **Style round-robin.** Every style against every other style at the same depth, each style playing as
   both Red and Blue, with seeded variety switched on. This is the key question: does one style beat
   everything, or do styles beat each other in a cycle (rock-paper-scissors)?
6. **Power-up stats per game.** The move number when each side used Queen and Cleaner, whether the winner
   used them, and whether the winning line was completed by a Queen move, a Cleaner move, a recycling pop
   or a plain drop.
7. **Draw and loop breakdown.** Split draws into "hit the ply cap" and "both players got a line at once".
   Count games where the same position (cells + power-ups + side to move) occurs 3 or more times.
8. **Forced-win probe.** From the start position, search as deep as is practical and report each first
   move's score. If any score is a win, report "forced win for Red within N moves". If none are, report
   "no forced win found within N moves". Do the same for the 49 normal two-move openings at a smaller depth.

## Rules for this task

- Do not change how `Bot.ChooseMove(state, depth)` behaves. Solo mode and the determinism tests rely on
  it. Styles and randomness go in new overloads or a new class.
- Do not change the game rules in `GameState` or `BoardManager`. Rule changes are my decision (see below).
- Keep everything in `KineticRank.Logic` with no `UnityEngine` references.
- Keep the merge gate fast: the long runs must be `[Explicit]` tests (or a separate category). Write down
  the exact command to start them in `Docs/FAIRNESS_REPORT.md`.
- Label conclusions as inferences unless the forced-win probe actually proves them.

## Done when

- The new fast tests pass, and the long runs can be started with one documented command.
- `TestResults/` holds a per-game CSV (opening, styles, depths, seed, result, length, power-up stats) and
  a summary.
- `Docs/FAIRNESS_REPORT.md` has a new section with:
  - first-player win rate (excluding draws) at each equal depth, for each opening set;
  - the style round-robin table;
  - power-up stats, the draw breakdown and the repeated-position count;
  - the forced-win probe result.
- A short plain-English summary for me at the top: is there a first-player edge, is there a dominant style,
  do Queen or Cleaner decide too many games, and what is the smallest change you'd recommend.

Reference targets, not pass/fail: first-player wins 45-55% (excluding draws) between equal bots, and no style
that beats every other style.

## Decisions I'll make after seeing the results (don't implement these)

- A repetition or move-limit draw rule. Right now only the tester stops games, at 120 plies, and real matches
  can recycle forever.
- Alternating who starts, or a swap rule after the first move.
- Power-up changes, and board size (last resort).
