# KineticRank  
*A kinetic twist on Connect 4*

<p align="center">
  <img src="Docs/screenshot.gif" width="450" alt="Animated screenshot – column shifts downward">
</p>

---

## 📦  What’s implemented (v 0.2)

| Feature | Details |
|---------|---------|
| **Board autogeneration** | 7 × 6 grid of `BoardTile` prefabs at runtime. |
| **Column-shift rule** | If you drop into a full column, the bottom piece pops out and the rest slide down. |
| **Token drop animation** | Each `Token` prefab animates from spawn-above to final slot. |
| **Turn logic** | Automatic RED → BLUE → RED alternation. |
| **Win detection** | 4-in-a-row checked **after every move** (including after a column shift). |
| **TMP win banner** | Giant “BLUE wins!” / “RED wins!” TextMeshPro banner centered on screen. |
| **Basic reset** | Press **R** to reload scene (quick-restart while prototyping). |

## 🛠  How to play / test

1. **Unity 2023.2.6f1** or later.  
2. Open project → `Scenes/Match.unity`.  
3. Click the ▶ **Play** button.  
4. **Left-click** inside a column to drop your token.

## 🗺 Roadmap

* “Queen” power-up &mdash; flips the colours in a column.  
* “Cleaner” power-up &mdash; removes every token in a column.  
* Blitz mode (smaller board + turn timer).  
* Local Elo ranking & stats panel.  
* Sprites & SFX polish.

## 📝  Controls (prototype)

| Action | Input |
|--------|-------|
| Drop token | **Left mouse click** in board area |
| Restart scene | **R** key |

## 🎨  Credits / License

All code © 2024 MrSeaPie — MIT License.  
Art placeholders are simple coloured squares (CC0).  
Text rendered with **TextMeshPro** (© Unity).

