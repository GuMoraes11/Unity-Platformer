# Level Creation Pipeline — Start to End Product

> Generated: 2026-03-10 — read-only investigation of how levels are authored, wired, and shipped.

## Executive Summary

There is **no custom level editor tool**. Levels are hand-built as individual Unity scenes using the
standard Unity Scene editor (tilemaps, prefabs, GameObjects). The "editor" you may be remembering is
the **ControllerSetupEditorWindow** — a one-time layer-setup wizard for the Tarodev character
controller, not a level editor.

The pipeline is: **Scene (hand-authored) → Build List (ordered) → LevelGradingData asset (per-level) → Runtime scripts glue it together.**

---

## 1) Authoring: How a Level Is Built

Each level is a standalone `.unity` scene file, authored directly in the Unity Scene view.

**Scene files:**
```
Assets/Scenes/Levels/Level 0.unity
Assets/Scenes/Levels/Level 1.unity
...
Assets/Scenes/Levels/Level 8.unity
```
Evidence: `Assets/Scenes/Levels/` — 9 scene files (Level 0 through Level 8).

**What goes into a scene (by hand):**

| Element | How it's placed | Key script/component |
|---|---|---|
| Terrain/platforms | Tilemaps or sprite GameObjects with BoxCollider2D | None (static colliders) |
| Player | Drag `Assets/Prefabs/Player.prefab` into scene | `PlayerController`, `PlayerInput`, `PlayerAnimator`, `PlayerHealth`, `LevelTimer` |
| Win trigger | Place a trigger collider (e.g., Flag prefab) | `LevelWin` (OnTriggerEnter2D) |
| Hazards | Drag `Saw.prefab`, `Spikes.prefab`, or custom objects | `Hazard` (trigger damage) |
| Color platforms | Group objects under a parent, wire to `ColorManager` | `ColorManager`, `Orange`, `Yellow`, `RadialToggleable`, `RadialWorldSwapController` |
| Slippery surfaces | Tag colliders as "Blue" | Detected by `PlayerController.ToggleGrounded()` |
| Level manager | Empty GO with `LevelManager` + `EndLevelMenuManager` | Wires grading data + end-level UI |
| Timer UI | Canvas with TMP_Text, referenced by `LevelTimer` | `LevelTimer` (on player or in scene) |
| Grade display | Canvas with TMP_Text per level (main menu only) | `GradeDisplay` |

Evidence: `Assets/Prefabs/` contains `Player.prefab`, `Flag.prefab`, `Saw.prefab`, `Spikes.prefab`.

**There is no procedural generation, no tile-palette-to-code pipeline, no serialized level format
beyond the Unity scene file itself.**

---

## 2) Per-Level Data: LevelGradingData

Each level has a companion `LevelGradingData` ScriptableObject asset that defines time-based grade
thresholds (S, A, B, C, D; anything slower = F).

**Assets:**
```
Assets/Scenes/LevelData/level0.asset  →  S ≤ 1.4s, A ≤ 1.6s, B ≤ 1.8s, C ≤ 2.0s, D ≤ 2.2s
Assets/Scenes/LevelData/level1.asset
...
Assets/Scenes/LevelData/level8.asset
```

**ScriptableObject definition:**
```csharp
[CreateAssetMenu(menuName = "Level/Grading Data")]
public class LevelGradingData : ScriptableObject
{
    public GradeThreshold[] thresholds;
    public string GetGrade(float time) { ... }
}
```
Evidence: `Assets/Scripts/LevelGradingData.cs`:L1-L24

**Wiring:** The scene's `LevelManager` component holds a reference to the grading asset. At `Start()`,
it finds the `LevelTimer` in the scene and calls `timer.SetGradingData(gradingData)`.

Evidence: `Assets/Scripts/LevelManager.cs`:L5-L15

---

## 3) Build List: Scene Ordering

Scenes are registered in `EditorBuildSettings` with a strict index order:

| Build Index | Scene |
|---|---|
| 0 | `(00) MainMenu.unity` |
| 1 | `Level 0.unity` |
| 2 | `Level 1.unity` |
| 3 | `Level 2.unity` |
| 4 | `Level 3.unity` |
| 5 | `Level 4.unity` |
| 6 | `Level 5.unity` |
| 7 | `Level 6.unity` |
| 8 | `Level 7.unity` |

Note: `Level 8.unity` exists on disk but is **not in the build list** — it won't ship.
`(99)Sampler.unity` is also excluded (likely a test/demo scene).

Evidence: `ProjectSettings/EditorBuildSettings.asset`

**Build index is the backbone of the entire navigation system.** Scene loading, level unlocking,
and "next level" all use `buildIndex` arithmetic.

---

## 4) Runtime Flow: Main Menu → Level → Win → Next

### 4a. Main Menu (`MenuManager`)

- `PlayFirstLevel()` → `SceneManager.LoadScene(1)` (Level 0)
- `OpenLevel(int levelId)` → `SceneManager.LoadScene("Level " + levelId)`
- Level buttons are gated by `PlayerPrefs.GetInt("UnlockedLevel", 2)` — starts at 2, meaning
  scene index 1 (Level 0) is always unlocked.

Evidence: `Assets/Scripts/MenuManager.cs`:L17-L31, L65-L90

### 4b. In-Level Runtime

1. **LevelManager.Start()** finds `LevelTimer`, injects `LevelGradingData`.
2. **LevelTimer.Update()** accumulates `Time.deltaTime` and updates the timer UI.
3. **Player plays the level** — physics driven by `PhysicsSimulator` → `PlayerController`.
4. **Player hits win trigger** → `LevelWin.OnTriggerEnter2D()`:
   - Calls `timer.StopTimer()` — saves best time to `PlayerPrefs`, computes grade.
   - Calls `UnlockNewLevel()` — increments `PlayerPrefs["ReachedIndex"]` and `["UnlockedLevel"]`.
   - Calls `EndLevelMenuManager.ShowEndLevelMenu(time, grade)` — freezes time, shows results UI.

Evidence: `Assets/Scripts/LevelWin.cs`:L6-L37
Evidence: `Assets/Scripts/LevelTimer.cs`:L35-L73

### 4c. End-Level Menu (`EndLevelMenuManager`)

- Shows current time, best time, current grade, best grade.
- **RetryLevel()** → `SceneManager.LoadScene(currentBuildIndex)`
- **NextLevel()** → `SceneManager.LoadScene(currentBuildIndex + 1)`
- **BackToMainMenu()** → `SceneManager.LoadScene(0)`

Evidence: `Assets/Scripts/EndLevelMenuManager.cs`:L27-L75

### 4d. Grade Display on Main Menu (`GradeDisplay`)

On the main menu, `GradeDisplay` reads `PlayerPrefs` for each level's saved grade and renders
letter grades with color coding (S = animated gold, A = silver, B = bronze, F = red).

Evidence: `Assets/Scripts/GradeDisplay.cs`:L23-L57

---

## 5) Persistence Model

All persistence is **PlayerPrefs only** — no save files, no cloud saves.

| Key pattern | Type | Purpose |
|---|---|---|
| `BestTime_{sceneName}` | float | Best completion time per level |
| `Grade_{sceneName}` | string | Best grade letter per level |
| `ReachedIndex` | int | Highest build index reached |
| `UnlockedLevel` | int | Number of unlocked levels (starts at 2) |

`ResetGame()` in `MenuManager` calls `PlayerPrefs.DeleteAll()`.

Evidence: `Assets/Scripts/LevelTimer.cs`:L19-L21, L39-L68
Evidence: `Assets/Scripts/LevelWin.cs`:L29-L36
Evidence: `Assets/Scripts/MenuManager.cs`:L80-L84

---

## 6) The "Editor" That Exists (It's Not a Level Editor)

`ControllerSetupEditorWindow` is a one-time setup wizard that:
- Pops up on first import of the Tarodev controller package.
- Offers to auto-create Unity layers (Player=7, Climbable=8, Ladders=9).
- Provides setup instructions for the `PlayerStats` ScriptableObject.

It has **nothing to do with level creation**.

Evidence: `Assets/Controller/Scripts/Editor/ControllerSetupEditorWindow.cs`:L1-L105

---

## 7) Color/World System (Level Mechanic, Not Level Editor)

Some levels use a color-cycling mechanic where platforms belong to color groups. This is a
**gameplay mechanic**, not a level-building tool:

- `ColorManager` holds a list of `ColorPlatform` entries (colorName + platformGroup GO).
- `RadialWorldSwapController` handles Q/E input to cycle colors with a radial wave effect.
- `RadialToggleable` on individual objects stores which `worldIndex` they belong to.
- `Orange` platforms disappear on contact and respawn based on active color.
- `Yellow` platforms increase player glow intensity.

These are all placed by hand in the scene.

Evidence: `Assets/Scripts/Colors/ColorManager.cs`, `RadialWorldSwapController.cs`, `Orange.cs`, `Yellow.cs`

---

## 8) Step-by-Step: How to Create a New Level

Based on the existing patterns, here's the implied workflow:

1. **Duplicate** an existing level scene (e.g., `Level 8.unity` → `Level 9.unity`).
2. **Edit** the scene in Unity: place tilemaps, prefabs, hazards, color groups.
3. **Ensure** the scene contains: Player prefab, LevelWin trigger, LevelManager GO, EndLevelMenuManager + UI canvas, LevelTimer (on player or in scene).
4. **Create** a `LevelGradingData` asset: right-click → Create → Level → Grading Data. Set time thresholds.
5. **Assign** the grading asset to the `LevelManager.gradingData` field in the scene.
6. **Add** the scene to Build Settings (File → Build Settings → drag scene in). Order matters — it must be the next sequential index.
7. **Update** the main menu's `MenuManager.levelButtons` array to include a button for the new level.
8. **Update** `GradeDisplay.levelGrades` array on the main menu to show the new level's grade.

**There is no automation for steps 6-8.** Each is a manual inspector operation.

---

## 9) Gaps and Risks

| Gap | Impact |
|---|---|
| No level editor tool | Every level is hand-placed; no rapid iteration tooling |
| Build index is hardcoded navigation | Adding/removing/reordering scenes breaks level unlock and "next level" logic |
| PlayerPrefs-only persistence | Data lost on uninstall; no cross-device sync; trivially editable by players |
| `FindObjectOfType` used at runtime | `LevelManager`, `LevelWin`, `LevelTimer` all use `FindObjectOfType` — fragile if duplicates exist |
| Level 8 not in build list | Scene exists but won't be playable in builds |
| No validation | Nothing checks that a scene has all required components (Player, LevelWin, LevelManager, etc.) |
| Grade thresholds are per-asset, not per-scene | If the wrong asset is assigned to `LevelManager`, grades will be wrong with no warning |

