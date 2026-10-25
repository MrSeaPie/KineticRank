# Manual scene-play checklist: H. Music (MusicPlayer.cs)

Scope: task tsk_0mvdjd77536c2273c (persistent menu/match music with fades).
This is section H of the manual checklist. It lives in its own file so Docs/SCENE_PLAY_CHECKLIST.md is not rewritten.
Run in the Unity Editor (2022.3.62f1), starting from Assets/Scenes/MainMenu.unity unless noted.
Record each result as PASS / FAIL / NOT RUN, with the commit hash. Do not mark this checklist done from reasoning or tests alone.
Automated coverage: MusicPlaylistTests (EditMode) cover track rotation only. Playback, fades and scene persistence need manual checks.

Commit under test: ________  Tester: ________  Date: ________

## H. Music
- [ ] H1. Enter Play mode in MainMenu: menu music (menu_1 or menu_2) starts and loops. [NOT RUN]
- [ ] H2. Click Play: the menu track crossfades (about 0.75 s) into a gameplay track with no silence gap and no audible click. [NOT RUN]
- [ ] H3. Let a gameplay track finish: the next gameplay track starts, cycling gameplay_1..6, and wraps around after the last one. [NOT RUN]
- [ ] H4. Press M during a match: the gameplay track crossfades back to menu music. [NOT RUN]
- [ ] H5. Rematch (Enter) after a win: music continues without restarting from the beginning and without a second music source. [NOT RUN]
- [ ] H6. Switch scenes MainMenu -> Match -> MainMenu -> Match: the Hierarchy shows exactly one MusicPlayer under DontDestroyOnLoad. [NOT RUN]
- [ ] H7. Music volume is about 0.45 and sits under SFX: ui_click, win_red and win_blue stay clearly audible over the music. [NOT RUN]
- [ ] H8. Play Match.unity directly (no MainMenu first): gameplay music starts and the Console shows no errors. [NOT RUN]
- [ ] H9. Temporarily rename one gameplay clip: that track is skipped, a single warning is logged (not one per frame), and rotation continues. Restore the clip afterwards. [NOT RUN]
- [ ] H10. Observation only, no expected count: after several switches (menu -> match, let a gameplay track end, M back to menu, Play again), take a Profiler Memory module sample (Detailed view) and record which Audio/Music AudioClips are loaded and how many. Also note any frame hitch during a crossfade. MusicPlayer does not unload clips explicitly; this check gathers evidence for whether that is needed. [NOT RUN]
