# KineticRank handoff (shelved 2026-10-25)

Where we left off, for whoever picks KineticRank up next. For what isn't built yet, read `IDEAS_BACKLOG.md` (Part 1 first).

## State at shelving

- **Main:** `56e4d22`. Nothing is unmerged: every agent branch is level with main or contains only work already on it.
- **Tests,** run by the builder on the final main:
  - EditMode: 289 passed, 0 failed, 14 skipped. The skipped ones are the explicit long fairness and calibration runs, which run only on request.
  - PlayMode: 98 of 98 passed.
- **Visual check** at 1600x900 and 1062x425: the main menu (with the rank panel and Daily Puzzle streak) and a Solo match lay out correctly.
- **The team copy** lives at `~\.artificial-society\workspaces\mis_0mvay1ep99306a0b2\` (main plus each agent's branch). The same history is on GitHub as the `ai-team` branch of MrSeaPie/KineticRank.

## What the game has

- **Standard mode:** a 7x6 board, four in a row, and the full-column shift. Queen and Cleaner are once per match.
- **Traditional mode:** the shift without power-ups.
- **Play modes:** hot-seat ranked, casual Solo against 5 bot tiers (Pine to Ebony), and Ranked Solo. Ranked Solo picks the closest bot, has Bronze to Diamond ranks with placement and protection, and runs a 30-second move clock.
- **Alternating starts.**
- **Puzzles and practice:** 22 puzzles, a daily puzzle with a streak, and a 4-step tutorial.
- **Match records:** saved as move lists that replay exactly (the last 50 are kept).
- **Look:** a wooden look, music, and sound effects.

## Notes from the agents

- **Agent-03:** the move clock is complete with full test coverage. See `MOVE_CLOCK_HANDOFF.md`.
- **Agent-01:** match history is recorded and replays correctly, but it isn't wired into real matches yet. The exact remaining steps are in `MATCH_HISTORY_RUNTIME_HANDOFF.md`, with task `tsk_0mvji6wuh4659b8fb`. The rank screens' manual checklist is in `RANK_PRESENTATION_ACCEPTANCE.md`.
- **Agent-02:** reviewed and merged the move clock. No open work.

## Before resuming

1. **Reopen this mission** rather than starting from a result copy. The workspace, task notes and agent memories carry on.
2. **Give the mission a test command** (the Unity EditMode and PlayMode batch runs). It currently has none, so merges aren't tested automatically.
3. **Make the power-up decision** in `IDEAS_BACKLOG.md`: the hoarding edge came back at depth 4, 71.6%.
