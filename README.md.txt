# KineticRank — a tactical Connect-4 variant

<p align="center">
  <img src="Docs/banner.png" width="600" alt="KineticRank mock banner"/>
</p>

> **Status:** Classic mode playable • Win-banner working • Column-push & power-ups queued  
> **Last checkpoint:** `Baseline / Classic mode working, banner added`&nbsp;— commit `d15d9da`

---

## 🕹️  Core Idea
KineticRank starts with familiar Connect-4 rules (drop tokens, line up four in a row)  
and layers **“kinetic” mechanics** on top:

| Mechanic | What happens | Purpose |
|----------|--------------|---------|
| **Column-push** | If a column already holds 6 tokens, dropping a new token ejects the **bottom** piece and shifts everything down. | The board never locks; enables deeper combos. |
| **Power-ups** (1 per player / game) | • **Queen** – flips every token in that column<br>• **Cleaner** – removes all tokens in that column | Comeback potential & creative traps. |
| **Modes** | • **Classic** – 7 × 6 board, no timer (implemented)<br>• **Blitz** – smaller board, 10 s per move (road-map)<br>• **Bullet** – 4 × 4, 5 s per move (road-map) | Different skill tests. |

---

## ✨  Current Features (Baseline)

- Classic Connect-4 with win check in all directions.  
- Column generation and token-drop animation.  
- Turn banner (“RED wins!” / “BLUE wins!”) using TextMesh Pro.  
- Clean Git history & GitHub Actions placeholder.

> Want to see it quickly?  
> 1. `git clone https://github.com/…/KineticRank.git`  
> 2. Open the **Unity** project (2022.3 LTS or newer).  
> 3. Run **Scene → `Game`** and click on columns.

---

## 🛠️  How to Build / Run

| Step | Notes |
|------|-------|
| 1. Install **Unity 2022.3 LTS** + **TextMesh Pro** (package comes pre-installed). | Older versions may compile but aren’t tested. |
| 2. Clone repo → open folder in Unity Hub. | |
| 3. Open scene **`Assets/Scenes/Game.unity`**. | |
| 4. Press **Play**. Left-click on a column to place a token. | |

---

## 🗺️  Road-map

- [ ] **Column-push** logic (shift & eject token).  
- [ ] **Queen** / **Cleaner** power-ups & selection UI.  
- [ ] Blitz & Bullet timers + smaller boards.  
- [ ] Local **Elo** rating table (JSON) → later server sync.  
- [ ] Online matchmaking prototype (Mirror / Fish-Net).  
- [ ] Sound FX & polished sprites.  
- [ ] Steam / itch .io release checklist.

---

## 🤝  Contributing

Pull-requests are welcome once the project stabilises.  
Until then, feel free to open Issues for ideas or bug reports—especially around gameplay balance.

---

## 📄  License

> _Pick one_: MIT, Apache-2.0, GPL-3.0, etc.  
> Replace this block with the actual license text before public launch.

