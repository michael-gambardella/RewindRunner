# Rewind Runner

A 2D platformer with a **one-button time rewind** mechanic. Hold Shift to rewind; when you release, your past self replays as a ghost. Built in Unity (Braid-inspired) for a game jam–style scope.

---

## What It Is

- **Core mechanic:** You can rewind the last ~6 seconds of movement. Your past self is replayed as a semi-transparent ghost to help solve jumps and puzzles.
- **Controls:** Move (WASD / Arrows), Jump (Space), Rewind (hold Left or Right Shift).
- **Scope:** 2D platformer with recording, smooth rewind, and ghost replay. Suitable for small levels and puzzle design.

---

## How to Run

1. **Requirements**
   - [Unity 6](https://unity.com/download) (tested on 6000.2.10f1). Unity 2022 LTS may work with the same 2D + Input System setup.

2. **Open the project**
   - Clone or download this repo.
   - Open Unity Hub → **Add** → select the `RewindRunner` folder (the one containing `Assets`, `ProjectSettings`, `Packages`).
   - Open the project.

3. **Play**
   - In the **Project** window go to **Assets → Scenes**.
   - Open **Level_01** (or **SampleScene**).
   - Press **Play** in the editor.
   - Use **WASD** or **Arrow keys** to move, **Space** to jump, and **hold Shift** to rewind; release Shift to spawn the ghost replay.

---

## Project Structure

| Folder / Area        | Contents                                      |
|---------------------|-----------------------------------------------|
| `Assets/Scripts`    | `PlayerController2D` (move, jump, record, rewind), `GhostReplay`, `RecordedFrame` |
| `Assets/Scenes`     | `Level_01`, `SampleScene`                     |
| `Assets/Prefabs`    | Ghost prefab for replay                       |
| `Assets/Sprites`    | Placeholder / temp art                        |
| `ProjectSettings`   | Unity project and 2D/Input System settings    |

---

## Tech Notes

- **Input:** New Input System (keyboard); no legacy Input Manager.
- **Recording:** Position and velocity are recorded every physics frame and capped at ~6 seconds for rewind.
- **Ghost:** Spawned when rewind ends; plays back the rewound segment then destroys itself.

---

## License

This is a portfolio project
