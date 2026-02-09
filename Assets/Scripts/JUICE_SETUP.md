# Juice Setup (camera shake, ghost trail, screen tint, whoosh, squish/stretch)

Wire these into the rewind and jump flow step by step. All references are **optional**—leave blank to skip a feature.

---

## 1. Camera shake

- **Add:** On **Main Camera**, add the **CameraShake** component.
- **Order:** So the shake applies on top of follow, set **Script Execution Order**:  
  **Edit → Project Settings → Script Execution Order** → add `CameraFollow2D` = `0`, `CameraShake` = `10`.
- **Wire:** Add **GameJuice** (see step 6) and leave **Camera Shake** unassigned; GameJuice will use the one on the same object, or assign the Main Camera’s CameraShake there.

---

## 2. Screen tint (rewind = blue, jump = subtle flash)

- **Add:** Add the **ScreenTint** component to the same GameObject as **GameJuice** (e.g. Main Camera or a “GameJuice” empty).  
  If **Tint Image** is left empty, it will create a full-screen Canvas + Image at runtime.
- **Optional:** Create your own Canvas with a full-screen Image and assign it to ScreenTint’s **Tint Image**.
- **Wire:** Assign that **ScreenTint** in GameJuice’s **Screen Tint** field (or leave null to auto-find on same object).

---

## 3. Whoosh (audio)

- **Add:** Add an **AudioSource** to the Main Camera (or to the GameJuice object). Disable **Play On Awake**.
- **Clips:** In **GameJuice**, assign:
  - **Jump Clip** – short whoosh on jump
  - **Rewind Start Clip** – when player holds Shift (rewind begins)
  - **Rewind Stop Clip** – when rewind ends / ghost spawns
- **Wire:** Assign that **Audio Source** in GameJuice’s **Audio Source** field.

---

## 4. Squish / stretch (jump = stretch up, land = squish)

- **Add:** On the **Player**, use a **child** Transform for the **visual** (sprite) so only the art squashes, not the collider.  
  If your player is one object with a sprite on the same Transform, create an empty child, move the SpriteRenderer to it (or use the player’s Transform and accept full-body squish).
- **Wire:** In **GameJuice**, set **Squish Transform** to that visual Transform.  
  Tune **Jump Scale** (e.g. 0.85, 1.2) and **Land Scale** (e.g. 1.15, 0.85) and **Squish Recover Time** to taste.

---

## 5. Ghost trail (trail only while rewinding)

- **Add:** On the **Player** (or a child), add a **Trail Renderer**:
  - **Time** ≈ 0.3–0.5  
  - **Start Width** / **End Width** to taste (e.g. 0.3 → 0)  
  - **Material:** Default-Line or any unlit material  
  - **Emitting:** Leave **off**; GameJuice will turn it **on** during rewind and **off** when rewind stops.
- **Wire:** In **GameJuice**, assign **Player Trail** to that TrailRenderer.

---

## 6. GameJuice (orchestrator)

- **Add:** Create an empty GameObject (e.g. **GameJuice**) or use **Main Camera**. Add the **GameJuice** component.
- **Optional refs:** Assign any of: **Camera Shake**, **Screen Tint**, **Audio Source**, **Squish Transform**, **Player Trail**.  
  If Camera Shake / Screen Tint are on the same object, they can be left null and will be auto-found.
- **Flow:** PlayerController2D already calls `GameJuice.Instance?.OnJump()`, `OnRewindStart()`, `OnRewindStop()`, and `OnLand()`; no extra wiring needed once GameJuice is in the scene.

---

## Minimal scene checklist

| Feature       | Add to scene                          | Assign in GameJuice      |
|---------------|----------------------------------------|---------------------------|
| Camera shake  | CameraShake on Main Camera             | (auto or) Camera Shake    |
| Screen tint   | ScreenTint on same object as GameJuice | (auto or) Screen Tint     |
| Whoosh        | AudioSource + 3 clips                   | Audio Source + clips      |
| Squish        | Child Transform on player (visual)     | Squish Transform          |
| Ghost trail   | TrailRenderer on Player                | Player Trail              |
| Orchestrator  | GameJuice component (e.g. on Camera)   | —                         |
