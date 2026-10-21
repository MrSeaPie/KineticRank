# Ranked Phase 1 acceptance

Record PASS / FAIL / NOT RUN with the tested commit and tester. This checklist defines acceptance; it does not establish that any feature is implemented or verified. Automated results and interactive results must be recorded separately.

Commit: ________  Tester: ________  Date: ________

## Automated verification

- [ ] EditMode suite passes. Record totals and results file.
- [ ] PlayMode suite passes. Record totals and results file.
- [ ] Rating tests cover band edges, five placements, tier promotion, three-game promotion protection and demotion after protection expires.
- [ ] Replay tests reproduce final boards and results for Queen, Cleaner, full-column shifts and timeout.
- [ ] History tests cover the last-50 limit, disk reload, invalid records, admitted moves and once-only completion.
- [ ] Runtime tests verify ranked timeout loss and match-history integration. Helper-only tests do not establish runtime integration.
- [ ] Bot calibration measurements and final ratings are recorded in RANKED_RATINGS.md; Traditional first-mover measurements are in the fairness report.

## Interactive checks

Use a disposable test profile or back up saves before testing. Record each result separately.

- [ ] MainMenu rank panel remains readable without overlap at 16:9 and 4:3, during placement and after placement. Shield, division, rating and progress bar agree with the profile.
- [ ] Ranked match button identifies the nearest-rated bot, choosing the stronger bot on a tie.
- [ ] Ranked Solo displays the player's rank and bot's rating in the match.
- [ ] Completing a ranked game changes the player rating once and shows the result, delta and any promotion, demotion or protection message.
- [ ] Restarting preserves the new player profile separately from legacy hot-seat and casual Solo progression.
- [ ] Rematches alternate the starting side, including ranked Solo and Traditional.
- [ ] Casual Solo, hot-seat, puzzles, tutorial, daily puzzle and Traditional leave the new player rank unchanged.
- [ ] Ranked human turn starts with 30 seconds; the active side's bar drains and turns red for the last 10 seconds.
- [ ] Opening How to play pauses the offline clock; closing it resumes the remaining time.
- [ ] Human timeout ends the match as a loss, blocks further moves and records a timeout exactly once. Bot thinking never causes a bot timeout.
- [ ] The clock is absent or inactive in every non-ranked mode.
- [ ] Completed match history survives restart, contains the correct mode, starter, labels, ratings, ordered moves, result, end reason, plies and UTC timestamps, and retains only the newest 50 records.
- [ ] Traditional launches as hot-seat and against every bot tier; only Normal is offered, Q/C do nothing, and its help text excludes power-ups.
- [ ] The result/rematch hint is readable and constrained at 4:3, and Match's Back To Menu button matches the wooden menu style.
- [ ] Returning to MainMenu and rematching introduce no duplicate panels, timers or history writes.

## Completion handoff

List the tested commit, automated totals, manual results and remaining defects in a short owner inbox message. Do not report Phase 1 complete while required checks remain unresolved.

Phase 2 requires owner-provisioned backend access and a separately authorized online design, including authoritative move replay, server timing, online matchmaking and online-only Master progression. Phase 1 does not authorize networking, package installation, accounts, publishing or spending.
