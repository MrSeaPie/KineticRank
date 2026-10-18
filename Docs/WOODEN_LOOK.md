# Wooden tabletop look (owner's choice)

The owner chose a **wooden board game on a dark walnut table**. Two rules from the owner:

* **Blocks stay square.** The owner likes the retro shape of the original placeholders. Don't make them round.
* **Red and blue stay.** They're now painted wooden blocks, so "Red wins" and "Blue wins" still make sense.

A mock match built from the new art is in `Docs/wooden_look_preview.png`. It's for the owner; it shows the intended result.

## Already done, no code needed

`Assets/Art/TokenRed.png`, `TokenBlue.png` and `BoardSquare.png` were repainted in place with the same GUIDs, so every prefab already uses the new art. They're still 256 px per cell at 256 pixels per unit. Filtering changed from Point to Bilinear with mipmaps so the wood grain scales cleanly.

* Each block is drawn at 216 of 256 px, with a soft shadow in the transparent margin.
* `BoardSquare` is honey wood with a recessed square slot. It tiles seamlessly, so the board reads as one wooden panel.

## New art to wire up (`Assets/Art/Wood/`)

All new sprites are single sprites, so reference them as `{fileID: 21300000, guid: <guid>, type: 3}`.

| File | Size | Pixels per unit | 9-slice border | Use |
|---|---|---|---|---|
| `table_background.png` | 1920x1080 | 108 (17.8 x 10 units) | none | Full-screen background in **MainMenu and Match**. Use a SpriteRenderer at sorting order -100, scaled to cover the whole camera view at 16:9 and 4:3 (cover, don't letterbox). Set the camera background colour to `#26150B` as a fallback. |
| `board_frame.png` | 256x256 | 256 | 64 all sides | Walnut frame around the board. Use a SpriteRenderer with Draw Mode **Sliced**, size `(columns + 0.5) x (rows + 0.5)` = 7.5 x 6.5 units, centred on the board (3.5, 3.0), sorting order -20 (tiles are -10). The outer 14 px are drop shadow. |
| `queen_mark.png` | 256x256 | 256 | none | Ivory crown. Replace the "Q" TextMeshPro letter on the Queen prefabs with a child SpriteRenderer using this, at the same position, sorting order 50. |
| `cleaner_mark.png` | 256x256 | 256 | none | Ivory broom. Replace the "C" letter on the Cleaner prefabs the same way. |
| `ui_button.png` | 384x128 | 100 | 48 all sides | uGUI Image, type **Sliced**, for every menu and HUD button. |
| `ui_panel.png` | 256x256 | 100 | 64 all sides | uGUI Image, type **Sliced**, for HUD panels, the power-up panel, the result banner and the main-menu box. |

GUIDs:

| Path | GUID |
|---|---|
| `Assets/Art/TokenRed.png` | `b9829aeba9d47164a8d15d476131cce7 (unchanged)` |
| `Assets/Art/TokenBlue.png` | `248a6e88d1ca5d84496693378367d6e9 (unchanged)` |
| `Assets/Art/BoardSquare.png` | `e4e1e2b066991d84fa93a6a715f5eec2 (unchanged)` |
| `Assets/Art/Wood/table_background.png` | `35bc5112d89842c397c696a99a36616f` |
| `Assets/Art/Wood/board_frame.png` | `21cd5a7047bc4b82a35e01de69622b86` |
| `Assets/Art/Wood/queen_mark.png` | `65711df8d82f41bfb3877444592e6f9f` |
| `Assets/Art/Wood/cleaner_mark.png` | `534d2f498d5a4b5c909cbd32901d77eb` |
| `Assets/Art/Wood/ui_button.png` | `6d4ee2c61aa448b6a2cc9c600053a648` |
| `Assets/Art/Wood/ui_panel.png` | `78e1bee7f3a04ee6a6794fbe5a7abd64` |

## Colours (replace the default Unity blue everywhere)

| Name | Hex | Use |
|---|---|---|
| Walnut dark | `#26150B` | camera fallback, darkest UI |
| Walnut | `#5B3A22` | panels, secondary text on light wood |
| Honey | `#E2AD72` | highlights, hover strip tint |
| Honey dark | `#A8672F` | accents |
| Ivory | `#F3E8D2` | all text on buttons and panels |
| Ink | `#2A1709` | text outline or shadow under ivory text |
| Block red | `#B8292C` | red player accents (turn indicator, "Red wins") |
| Block blue | `#2D55A8` | blue player accents |

## Sound

`Assets/Resources/Audio/` now has `token_drop`, `queen_flip`, `cleaner_clear`, `column_shift`, `invalid_move`, `token_select` and `match_draw` (WAV). AudioController already loads these names.

New: **`column_hover.wav`**, a very soft wooden tap. Play it when the pointer moves into a *different* column. Don't play it on every frame, or while a move is resolving. The clip is already mixed quiet.

## Motion, after the art is wired (this is what makes it feel "kinetic")

1. **Drop.** Make the block accelerate like gravity instead of falling at constant speed. Add a small settle bounce when it lands (about 6% overshoot), with `token_drop` at the moment of contact.
2. **Hover.** Light the hovered column with a soft, warm, translucent strip (ivory at about 12% alpha). Show the next block as a faint ghost above that column.
3. **Win.** Pulse the four winning blocks (scale 1.0, then 1.08, then 1.0, repeating) with a warm outline.

Visuals and animation must not change move-resolution order, timing guards, or ranked results.

## Suggested order (small reviewed merges; the owner play-tests each one)

1. Background, board frame, and the Queen/Cleaner marks.
2. Wooden UI buttons and panels, plus text colours.
3. The `column_hover` hook.
4. Motion polish.
