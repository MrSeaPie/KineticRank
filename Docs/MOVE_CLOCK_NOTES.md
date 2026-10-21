# MoveClock Implementation & Diagnostics Notes

- MoveClock logic: 30s per-move timer active in ranked games, warning threshold at 10s.
- Test isolation: NaturalClockExpiration test requires dynamic turnDuration configuration and explicit TearDown destruction of all created GameObjects to prevent SetUp isolation cascades.
- Timeout forfeit: directly wired to GameManager.TimeoutForfeit.
