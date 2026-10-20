# Brief: ranked play, Phase 1 (offline)

**From:** the owner
**For:** the team. Split it however suits you, in small merges.
**Type:** pure logic in `KineticRank.Logic` with EditMode tests, plus self-attaching presentation scripts. No network code, no new packages, no accounts.

## Why

KineticRank is a competitive game, and players need a rank to climb: Bronze, Silver, Gold and up. Online ranked play comes in Phase 2, once I've set up a backend account. Phase 1 builds everything that doesn't need a server, so that Phase 2 only adds the network:

- the rating and tier system;
- a ranked Solo mode against the bots, so there's a ladder to climb today;
- rank display;
- match records as move lists (the server will replay these to check moves);
- a move clock.

## What already exists (keep it working)

- `EloCalculator` (K = 32, ratings never below 1).
- `RankedProfile`: hot-seat Elo per colour (RED / BLUE). Leave it as it is.
- `SoloProfile` / `SoloLadder`: per-tier wins, losses and draws against the bot, in `kineticrank_solo.json`. Leave it as it is.
- `PlayerController.MovePlaced` (column, kind, mover) fires after every placement.
- `MenuLayoutFit` places any MainMenu panel cloned from the button group. Use it rather than hard-coded offsets.
- The alternating-starts decision applies to ranked Solo too: you and the bot take turns going first.

## 1. Rating and tiers (pure logic)

- A new player rating profile, kept apart from `RankedProfile` and `SoloProfile`, in its own save file (for example `kineticrank_rank.json`). It holds: rating (starts at 1000), ranked games played, placement games left, peak rating, last change, current tier and division, promotion-protection games left, and a season number (1 for now, for later resets).
- Tiers, lowest to highest: Bronze, Silver, Gold, Platinum, Diamond, Master. Each has three divisions, III (lowest) to I (highest), except Master, which has none.
- Starting bands, 300 points per tier and 100 per division, tunable in one table:
  - Bronze: below 900 (III below 700, II 700-799, I 800-899)
  - Silver: 900-1199
  - Gold: 1200-1499
  - Platinum: 1500-1799
  - Diamond: 1800-2099
  - Master: 2100 and up
- Master is reserved for online play. Against bots, the highest rank you can reach is Diamond I.
- Placement: the first 5 ranked games are placement games. They use a larger K (48), and no tier is shown until they're done ("Placement 3/5").
- Promotion protection: for the 3 ranked games after you reach a new tier (not just a new division), the shown tier can't drop below it. The rating number itself stays honest and keeps moving.
- Draws count as half a win, as `EloCalculator.CalculateDraw` already does.

## 2. Ranked Solo against the bots

- Each bot tier gets a fixed rating. Only the player's rating changes.
- Calibrate the bot ratings from data, don't guess them. Use the self-play harness to play adjacent tiers against each other, alternating who starts, and set the rating gaps so Elo's expected scores match the measured ones. Anchor Pine at 800 and cap any single gap at 400. Put the measured numbers and the final ratings in `Docs/RANKED_RATINGS.md`. A reasonable starting guess before calibration: Pine 800, Birch 1000, Oak 1200, Walnut 1400, Ebony 1600.
- Solo gets a "Ranked match" button that picks the bot whose rating is closest to yours, like matchmaking. On a tie it picks the stronger bot. The button shows who you'll face, e.g. "Ranked match: Oak (1200)".
- The existing casual Solo stays as it is: you pick any bot, and your rank doesn't change.
- Puzzles, the tutorial, the daily puzzle and hot-seat never change your rank.

## 3. Rank display

- Main menu: a rank badge (tier name and division, plus a simple procedurally painted shield in the tier's colour), "Placement 3/5" during placement, and a progress bar to the next division with the rating shown.
- After each ranked game: the change and the result, e.g. "+18 - Silver II → Silver I". Call out promotion, demotion and protection clearly ("Promoted to Gold III!", "Protected: 2 games left").
- In the match: your panel shows your rank ("You - Silver II") and the bot's panel shows its rating ("Bot: Oak (1200)").
- Match the wooden look, and keep everything script-only and self-attaching.

## 4. Match records as move lists (pure logic)

- A `MatchRecord` holding:
  - a format version and the rules version;
  - the mode (hot-seat, casual Solo, ranked Solo, puzzle);
  - who started;
  - each side's label and its rating before the game;
  - every move in order (column and kind, from `MovePlaced`);
  - the result and how it ended (line, both lines at once, timeout);
  - the number of plies and the UTC start and end times.
- A compact text form for logs, e.g. `N3 N3 Q2 C5`, and JSON for saving.
- `Replay()` rebuilds the game from the moves with `GameState`. The online server will do exactly this, so it must reproduce the final board and result. Add tests that replay recorded games, including Queen, Cleaner, full-column shifts and a timeout ending.
- Keep the last 50 records in their own local file. No replay UI yet.

## 5. Move clock

- 30 seconds per move, set by one constant, so it's easy to change later.
- On in ranked games; off in hot-seat, casual Solo, puzzles and the tutorial.
- Shown as a draining bar on the side panel of the player to move. It turns red in the last 10 seconds.
- Running out of time loses the game. Record it as a timeout.
- The bot's thinking doesn't count against it, so it never times out.
- Offline, the How to play overlay pauses the clock. Online will be different, but that's Phase 2.

## Rules for this task

- Don't change the game rules (`GameState`, `BoardManager`) or the fairness harness's baseline.
- Don't change what `RankedProfile` or `SoloProfile` store. The new rating lives in its own file.
- Phase 2 isn't part of this task: no networking, no package installs, no accounts, nothing online. If something can only be done online, write it down for Phase 2 instead.
- The alternating-starts work touches the same files (`PlayerController`, `GameManager`, `PowerUpHud`), so sync main before you start and keep each merge small.
- Everything stays luck-free: no random opponents and no random first move.

## Done when

- The rating, tier, placement and protection logic has EditMode tests, including the band edges, placement, a promotion, protection, and a demotion after protection ends.
- Bot ratings are calibrated, with the numbers in `Docs/RANKED_RATINGS.md`.
- Ranked Solo can be played from the menu. Your rank and its changes show on the menu, in the match and after each game.
- Match records save, and replay tests pass.
- The move clock works in ranked games and a timeout loses the game, with a PlayMode test.
- EditMode and PlayMode pass, and there's a short note for me in the inbox saying what's done and what Phase 2 will need.
