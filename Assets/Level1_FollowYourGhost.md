# Level 1 – Follow Your Ghost

First real level after the tutorial. Teaches that the ghost can **hold** a plate while the player runs somewhere else.

---

## Design goal

- **Layout:** Two pressure plates far apart; door opens only when **both** are pressed; player cannot reach both at once.
- **Solution:** Stand on plate A → rewind → ghost stays on A → player runs to plate B → door opens.

---

## What to place

| Element      | Purpose |
|-------------|---------|
| **Player start** | Left side, on a platform. |
| **Plate A**      | First pressure plate (e.g. left or lower area). Player stands here first. |
| **Plate B**      | Second pressure plate, far from A (e.g. right or upper area). Player runs here after rewind. |
| **Door**         | Blocks the exit. Opens only when **both** plates are pressed. |
| **Exit / goal**  | Level end trigger (LevelEnd) past the door. |

**Spacing rule:** From any one plate, the player must **not** be able to reach the other plate and the door in one run without rewinding. Use gaps, height, or obstacles so that:

- Reaching plate B (or the door) from plate A (or vice versa) in a single life is impossible, **or**
- Reaching both plates at the same time is impossible (they’re too far apart).

So the only way to open the door is: stand on A → rewind → ghost holds A → player goes to B.

---

## Inspector setup

### Door

1. Select the **Door** GameObject.
2. In the **Door** component:
   - **Plates** → Size: **2**
   - **Plates** → Element 0: drag **Plate A**
   - **Plates** → Element 1: drag **Plate B**
   - **Require All Plates**: **checked** (door opens only when both are pressed).

### Plates

- Each plate: **Pressure Plate** component → **Pressing Layers** = **Default** + **Ghost** (so both player and ghost count).
- Plates on **Default** layer so the ghost can trigger them (see Tutorial design doc for Ghost ↔ layer rules).

### Optional: Level1Layout helper

- Create an empty GameObject named **Level1Layout**, add the **Level1Layout** script.
- Assign **Plate A**, **Plate B**, and **Door**. This doesn’t wire the Door for you; it just keeps the level’s key objects in one place. You still assign both plates to the Door as above.

---

## Reference layout (world units)

You can copy this loosely or use it as a starting point. Exact numbers are flexible.

- **Player start:** e.g. (0, -2) on a long platform.
- **Plate A:** e.g. (6, -1.5) on its own small platform (left/mid).
- **Gap or hazard** so the player cannot walk from A to the right without rewinding (e.g. pit, or plate B is too high/far).
- **Plate B:** e.g. (18, 0) on a platform on the right (reachable only after leaving ghost on A).
- **Door:** e.g. (24, 1) blocking the path to the exit.
- **Level end trigger:** just past the door, e.g. (28, 1).

Adjust so that:

- From start, player can reach plate A, rewind, then have ghost on A.
- From there, player can reach plate B but **not** plate A at the same time (too far or blocked).
- When both A and B are pressed (ghost on A, player on B), the door opens and the player can reach the level end.

---

## Solution flow (for playtest)

1. Player runs to **plate A** and stands on it.
2. Player **rewinds** (hold Shift). Ghost spawns and replays; ghost stands on plate A.
3. Player **releases** rewind and runs to **plate B** and stands on it.
4. Both plates are pressed (ghost on A, player on B) → **door opens**.
5. Player walks through the door to the **level end** trigger.

---

## Checklist

- [ ] Two pressure plates in the scene (Plate A, Plate B).
- [ ] Door has **Plates** = [Plate A, Plate B], **Require All Plates** = true.
- [ ] Player cannot reach both plates at once (layout / gaps / height).
- [ ] Respawn point at start; DeathZone below pits if needed.
- [ ] LevelEnd trigger past the door.
- [ ] Plate pressing layers include Default and Ghost.
