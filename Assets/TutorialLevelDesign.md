# Tutorial Level Design – Rewind Runner

A short level to teach **move → jump → rewind** and let players feel the controls.

---

## Teaching Order

| Section | Goal | What the player does |
|--------|------|-----------------------|
| **1. Move** | Get used to left/right | Start on a wide platform. Move with WASD or Arrows. |
| **2. Jump** | Land a jump | Cross a small gap by jumping (Space). |
| **3. Rewind** | See rewind + ghost | On a safe platform, hold Shift to rewind, release to spawn the ghost. Try a few times. |
| **4. Optional** | Combine skills | Slightly higher platform or longer gap so they naturally use jump + rewind. |

No text or UI required for the first pass—layout alone can teach the flow.

---

## Reference Layout (World Units)

Use this as a guide when placing platforms (by hand or with **TutorialLevelBuilder**).

- **Player spawn:** around `(0, -1.5)` so feet are on the first platform.
- **Camera:** Main Camera with CameraFollow2D; Z = -10.

### Platforms

| # | Purpose   | Position (center) | Scale (width, height) | Notes |
|---|-----------|-------------------|------------------------|-------|
| 1 | Start     | (-1, -2)          | (8, 1)                 | Wide; player spawns here. |
| 2 | After gap | (6, -2)           | (4, 1)                 | Reach by jumping. |
| 3 | Rewind    | (11, -2)          | (6, 1)                 | Safe area to practice rewind. |
| 4 | Optional  | (14, 0)           | (4, 1)                 | Higher; encourages jump then rewind. |

- **Gap 1→2:** about 2 units (e.g. platform 1 ends ~x=3, platform 2 starts ~x=4).
- **Gap 2→3:** small or none; focus is on reaching platform 2, then walking to 3.
- **Gap 3→4:** vertical; jump up to platform 4, then rewind to see the ghost replay.

All platforms: **Layer = Ground**, with a **Box Collider 2D** (not trigger) so the player can land and jump.

---

## Building the Level

### Option A – By hand

1. Open your tutorial scene (e.g. duplicate Level_01 → "TutorialLevel").
2. Place the **Player** at the spawn position.
3. For each platform: create a **Quad** or **Sprite** (Square), add **Box Collider 2D**, set **Layer** to **Ground**, then set **Transform** position and scale to match the table above.

### Option B – TutorialLevelBuilder

1. Create a **Platform** prefab: one platform with Sprite Renderer + Box Collider 2D, **Layer = Ground**, then drag it into `Assets/Prefabs`.
2. In the scene, create an empty GameObject, add the **TutorialLevelBuilder** script, assign the platform prefab, and use the default platform list (or edit positions/scales in the Inspector).
3. Press Play; the builder spawns platforms at runtime. When the layout feels good, you can replace them with manually placed platforms in the scene if you prefer.

---

## Quick Checklist

- [ ] Player spawns on first platform.
- [ ] Camera follows (CameraFollow2D on Main Camera, target = Player).
- [ ] Gap between platforms 1 and 2 is jumpable but not trivial.
- [ ] Platform 3 is a clear “safe zone” to practice rewind.
- [ ] All platforms are on the **Ground** layer so jump works.
