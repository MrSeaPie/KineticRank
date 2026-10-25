# Solo bot tier ratings

Per Docs/RANKED_PHASE1_BRIEF.md section 2. The ratings live in `Assets/Scripts/Logic/BotTierRatings.cs` and are on the same scale as player ratings, so SoloLadder can pick the nearest opponent.

## Method

- Adjacent tiers play each other with the existing self-play harness (`SelfPlay.Run`, no baseline changes).
- Openings: all 49 two-ply openings (`SelfPlay.TwoPlyOpenings()`). Each tier takes Red (first move) once per opening, so 98 games per pair.
- Move cap: 120 plies; a capped game counts as a draw.
- Deterministic: every run gives the same numbers.
- Score s = (wins + 0.5 * draws) / games for the stronger tier.
- Gap = 400 * log10(s / (1 - s)), rounded, capped at 400; s <= 0.5 gives 0.
- Pine is anchored at 800 and each tier adds its gap to the one below.

## Measured results

| Pair | Games | Stronger wins | Losses | Draws (capped) | Score | Gap |
|---|---|---|---|---|---|---|
| Pine (d1) vs Birch (d2) | 98 | 72 | 22 | 4 (4) | 0.7551 | 196 |
| Birch (d2) vs Oak (d4) | 98 | 72 | 10 | 16 (3) | 0.8163 | 259 |
| Oak (d4) vs Walnut (d6) | - | - | - | - | - | unmeasured (guess 200) |
| Walnut (d6) vs Ebony (d8) | - | - | - | - | - | unmeasured (guess 200) |

Source: `Calibrate_LowerTiers` in `Assets/Tests/EditMode/BotTierRatingsTests.cs` (Passed, 3.4 s). The two upper pairs are `Calibrate_OakWalnut` and `Calibrate_WalnutEbony`; until they run, their gaps keep the brief's +200 starting guess.

## Ratings

| Tier | Depth | Rating | Status |
|---|---|---|---|
| Pine | 1 | 800 | anchor |
| Birch | 2 | 996 | measured |
| Oak | 4 | 1255 | measured |
| Walnut | 6 | 1455 | provisional (unmeasured gap) |
| Ebony | 8 | 1655 | provisional (unmeasured gap) |

## Running the calibration

```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "KineticRank.Tests.EditMode.BotTierRatingsTests.Calibrate_LowerTiers" -testResults TestResults/calibration_lower.xml -logFile TestResults/calibration_lower.log
```

The result line for each pair is reported through `Assert.Pass` and appears in the test's message in the results XML. Swap the test name for `Calibrate_OakWalnut` or `Calibrate_WalnutEbony` to measure the upper pairs.

## Caveats

- 98 games per pair gives roughly +/-70 Elo of statistical noise per gap; the numbers are a starting calibration, not a final truth.
- Bots are deterministic, so the opening set is the only source of variety; human players will not match bot behaviour exactly.
