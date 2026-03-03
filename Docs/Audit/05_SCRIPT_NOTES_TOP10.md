# 05 SCRIPT NOTES TOP10
<!-- BEGIN_CONTENT -->

## 1. PlayerController.cs

**Path:** `Assets/Controller/Scripts/PlayerController.cs` (1043 lines)

**Summary:** Monolithic player controller implementing `IPlayerController` and `IPhysicsObject`.
Handles ground detection, wall climbing, ladder climbing, jumping (coyote time, air jumps,
wall jumps), dashing, crouching, and slippery surface detection.

**Key Invariants:**
- Requires `Rigidbody2D`, `BoxCollider2D`, `CapsuleCollider2D` (via `RequireComponent` — `Assets/Controller/Scripts/PlayerController.cs:L7`)
- Registers with `PhysicsSimulator.Instance.AddPlayer(this)` in `Awake` (`Assets/Controller/Scripts/PlayerController.cs:L94`)
- Unregisters via `PhysicsSimulator.Instance.RemovePlayer(this)` in `OnDestroy` (`Assets/Controller/Scripts/PlayerController.cs:L98`)
- `PlayerStats` ScriptableObject must be assigned; all tuning values read from it

**I/O:**
- Input: `PlayerInput.Gather()` → `FrameInput` struct (Move, JumpDown, JumpHeld, DashDown)
- Output: Events consumed by `PlayerAnimator` (`Jumped`, `GroundedChanged`, `DashChanged`, `WallGrabChanged` — `Assets/Controller/Scripts/PlayerController.cs:L32-L35`)
- Physics: `Rigidbody2D.linearVelocity` set directly each `TickFixedUpdate` (`Assets/Controller/Scripts/PlayerController.cs:L110`)

**Dependencies:**
- Serialized: `PlayerStats` (`Assets/Controller/Scripts/PlayerController.cs:L30`)
- Runtime lookup: `PhysicsSimulator.Instance` (`Assets/Controller/Scripts/PlayerController.cs:L94-L98`), `GetComponent<PlayerHealth>()` (`Assets/Controller/Scripts/PlayerController.cs:L95`)
- Globals: None

**Gotchas:**
- `CompareTag("Blue")` for slippery surfaces (`Assets/Controller/Scripts/PlayerController.cs:L317`) — stringly-typed
- 1043 lines; single-responsibility principle violated
- `TickUpdate` only gathers input (`Assets/Controller/Scripts/PlayerController.cs:L102-L108`); all physics in `TickFixedUpdate`

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| REFACTOR | 1043-line god class | High | Split into sub-state machines (ground, air, wall, ladder, dash) | `Assets/Controller/Scripts/PlayerController.cs:L1-L1043` |
| BUG | `CompareTag("Blue")` — tag may not exist in TagManager | Med | Replace with `LayerMask` or const string | `Assets/Controller/Scripts/PlayerController.cs:L317` |
| DESIGN | 20 public properties expose internal state | Med | Reduce to interface; hide setters | `Assets/Controller/Scripts/PlayerController.cs:L30-L47` |
| REFACTOR | `TickUpdate` only calls `GatherInput`; could be inlined | Low | Merge into `TickFixedUpdate` or document why split | `Assets/Controller/Scripts/PlayerController.cs:L102-L108` |
| TEST | No unit tests for jump/dash/wall state transitions | High | Add PlayMode tests for each movement state | `Assets/Controller/Scripts/PlayerController.cs:L110-L260` (state logic spans these lines) |
| PERF | Ground-check raycasts run every `TickFixedUpdate` | Low | Profile; consider caching or reducing ray count | `Assets/Controller/Scripts/PlayerController.cs:L260-L296` |

---

## 2. PlayerAnimator.cs

**Path:** `Assets/Controller/Scripts/PlayerAnimator.cs` (444 lines)

**Summary:** Drives all player visual feedback: sprite animation, particle systems,
audio clips, squish/stretch effects. Subscribes to PlayerController events.

**Key Invariants:**
- 36 `[SerializeField]` fields for particle systems, audio clips, sprite renderer (`Assets/Controller/Scripts/PlayerAnimator.cs:L10-L264`)
- Relies on `PlayerController` being on the same or parent GameObject

**I/O:**
- Input: PlayerController events (`Jumped`, `GroundedChanged`, `DashChanged`, `WallGrabChanged` — subscribed at `Assets/Controller/Scripts/PlayerAnimator.cs:L59-L62`)
- Output: Visual/audio side effects only (no gameplay state mutation)

**Dependencies:**
- Serialized: 36 `[SerializeField]` references (particles, audio, sprites — `Assets/Controller/Scripts/PlayerAnimator.cs:L10-L264`)
- Runtime lookup: `GetComponentInParent<PlayerController>()` (implicit)
- Globals: None

**Gotchas:**
- `Update()` (`Assets/Controller/Scripts/PlayerAnimator.cs:L81`) + `LateUpdate()` (`Assets/Controller/Scripts/PlayerAnimator.cs:L100`) both run every frame
- Coroutines for squish/trail reset — potential issues if object disabled mid-coroutine
- Tightly coupled to PlayerController's public API surface

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| REFACTOR | 36 SerializeFields — inspector is unwieldy | Med | Group into sub-ScriptableObjects or nested structs | `Assets/Controller/Scripts/PlayerAnimator.cs:L10-L264` |
| BUG | Coroutine may orphan if GameObject disabled mid-squish | Med | Guard with `isActiveAndEnabled` check | `Assets/Controller/Scripts/PlayerAnimator.cs:L107-L112` |
| PERF | `Update()` runs every frame even when idle | Low | Gate behind `_player.Active` check | `Assets/Controller/Scripts/PlayerAnimator.cs:L81-L98` |
| DESIGN | Tightly coupled to `PlayerController` public API | Med | Introduce animation interface | `Assets/Controller/Scripts/PlayerAnimator.cs:L59-L62` |
| TEST | No tests for animation state transitions | Med | Add PlayMode tests verifying particle/audio triggers | `Assets/Controller/Scripts/PlayerAnimator.cs:L59-L62` (event subscriptions that need coverage) |

---

## 3. LevelTimer.cs

**Path:** `Assets/Scripts/LevelTimer.cs` (135 lines)

**Summary:** Tracks elapsed level time, computes grade from `LevelGradingData`,
persists best time and best grade to `PlayerPrefs`.

**Key Invariants:**
- `isRunning` flag gates `Update()` accumulation
- `levelKey` / `gradeKey` derived from active scene name in `Start()`
- Grade comparison uses hardcoded array `{ "S", "A", "B", "F" }`

**I/O:**
- Input: `SetGradingData(LevelGradingData)` called by `LevelManager.Start`
- Output: `StopTimer()` called by `LevelWin`; `GetCurrentTime()` / `GetCurrentGrade()` read by `EndLevelMenuManager`
- Side effects: `PlayerPrefs.SetFloat` / `SetString` / `Save` (`Assets/Scripts/LevelTimer.cs:L39-L43`, `Assets/Scripts/LevelTimer.cs:L65-L66`)

**Dependencies:**
- Serialized: `TMP_Text timerText` (`Assets/Scripts/LevelTimer.cs:L7-L8`)
- Runtime lookup: None (receives `LevelGradingData` via `SetGradingData`)
- Globals: `PlayerPrefs`, `SceneManager.GetActiveScene()`

**Gotchas:**
- `FormatTime` is duplicated in `EndLevelMenuManager` (divergence risk)
- Grade array `{ "S", "A", "B", "F" }` is hardcoded; must match `LevelGradingData` thresholds
- `PlayerPrefs.GetString(gradeKey, null)` — default `null` may behave unexpectedly on some platforms

Evidence: `Assets/Scripts/LevelTimer.cs:L62`
```csharp
string bestGrade = PlayerPrefs.GetString(gradeKey, null);
```

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| REFACTOR | Duplicate `FormatTime` in `EndLevelMenuManager` | Med | Extract to shared static utility class | `Assets/Scripts/LevelTimer.cs:L103-L113` vs `Assets/Scripts/EndLevelMenuManager.cs:L77-L83` |
| BUG | `PlayerPrefs.GetString(gradeKey, null)` — null default | Med | Use empty string `""` as default | `Assets/Scripts/LevelTimer.cs:L62` |
| DESIGN | Grade ordering hardcoded as string array | Med | Move to `LevelGradingData` or enum | `Assets/Scripts/LevelTimer.cs:L85-L95` |
| REFACTOR | `Debug.Log` left in production path | Low | Wrap in `#if UNITY_EDITOR` or remove | `Assets/Scripts/LevelTimer.cs:L59-L67` |
| TEST | No tests for grade comparison logic | High | Unit test `IsGradeBetter` with edge cases | `Assets/Scripts/LevelTimer.cs:L85-L95` |
| DESIGN | `PlayerPrefs` persistence not abstracted | Med | Introduce save-data interface | `Assets/Scripts/LevelTimer.cs:L39-L43` |

---

## 4. MenuManager.cs

**Path:** `Assets/Scripts/MenuManager.cs` (104 lines)

**Summary:** Handles main menu, pause menu, level select, quit, and full game reset.

**Key Invariants:**
- `Time.timeScale` toggled between 0 and 1 for pause
- Level unlock gating via `PlayerPrefs.GetInt("UnlockedLevel", 2)` (`Assets/Scripts/MenuManager.cs:L25`)
- `levelButtons[i].interactable = (i + 1) < unlockedLevel`

**I/O:**
- Input: UI button callbacks + `Input.GetKeyDown(KeyCode.Escape)` (legacy — `Assets/Scripts/MenuManager.cs:L36`)
- Output: `SceneManager.LoadScene` by index (0, 1) and by name (`"Level " + levelId`)
- Side effects: `PlayerPrefs.DeleteAll()` in `ResetGame()` (`Assets/Scripts/MenuManager.cs:L82`)

**Dependencies:**
- Serialized: `GameObject` refs for menu panels, `Button[]` for level buttons
- Runtime lookup: None
- Globals: `PlayerPrefs`, `SceneManager`, `UnityEngine.Input` (legacy)

**Gotchas:**
- Mixes legacy `Input.GetKeyDown` with project's new Input System
- `SceneManager.LoadScene(0)` / `LoadScene(1)` — fragile if build order changes
- `ResetGame()` calls `PlayerPrefs.DeleteAll()` — nuclear option, no confirmation
- `OpenLevel(int levelId)` uses `"Level " + levelId` — breaks if scene naming convention changes

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| BUG | Legacy `Input.GetKeyDown` will break if Input System set to "New" only | High | Port to Input System action | `Assets/Scripts/MenuManager.cs:L36` |
| REFACTOR | Magic scene indices `0`, `1` | Med | Use const or SceneReference asset | `Assets/Scripts/MenuManager.cs:L61-L83` |
| DESIGN | `ResetGame` calls `PlayerPrefs.DeleteAll()` with no confirmation | High | Add confirmation dialog | `Assets/Scripts/MenuManager.cs:L82` |
| REFACTOR | `"Level " + levelId` string concatenation for scene names | Med | Use scene name constants or SceneReference | `Assets/Scripts/MenuManager.cs:L89` |
| TEST | No tests for level unlock gating logic | Med | Unit test button interactable state | `Assets/Scripts/MenuManager.cs:L25-L30` |

---

## 5. ColorManager.cs

**Path:** `Assets/Scripts/Colors/ColorManager.cs` (132 lines)

**Summary:** Manages color-coded platform groups. Each group has a `platformGroup`
and `backgroundGroup` of GameObjects. Toggles visibility (alpha) and collider state.

**Key Invariants:**
- `colorPlatforms` list defines all color groups (serialized in inspector)
- `activeAlpha` / `inactiveAlpha` control visibility (public fields)
- `currentIndex` tracks which group is currently active

**I/O:**
- Input: Public `SetIndexWithoutApplying(int)`, `ApplyImmediate()` called by `RadialWorldSwapController`
- Output: Sets alpha on `SpriteRenderer` + enables/disables `Collider2D` per group (`Assets/Scripts/Colors/ColorManager.cs:L94-L109`)
- Side effects: `GetComponentsInChildren<SpriteRenderer>` and `<Collider2D>` per swap (`Assets/Scripts/Colors/ColorManager.cs:L98-L105`)

**Dependencies:**
- Serialized: `List<ColorPlatform> colorPlatforms`
- Runtime lookup: None
- Globals: None (self-contained manager)

**Gotchas:**
- `CompareTag` usage for platform identification in `GetAlphaForTag` (`Assets/Scripts/Colors/ColorManager.cs:L118`)
- Public `activeAlpha` / `inactiveAlpha` fields read directly by `Orange.cs` (tight coupling)
- `GetComponentsInChildren` called per swap — allocates arrays each time

Evidence: `Assets/Scripts/Colors/ColorManager.cs:L111-L119`
```csharp
public float GetAlphaForTag(string tag) {
    for (int i = 0; i < colorPlatforms.Count; i++) {
        if (platform.platformGroup.CompareTag(tag))
            return i == currentIndex ? activeAlpha : inactiveAlpha;
    }
}
```

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| PERF | `GetComponentsInChildren` allocates per swap | Low | Cache child renderers/colliders on init | `Assets/Scripts/Colors/ColorManager.cs:L98-L105` |
| DESIGN | `activeAlpha`/`inactiveAlpha` are public fields | Med | Expose via read-only property or method | `Assets/Scripts/Colors/ColorManager.cs:L111` and `Assets/Scripts/Colors/Orange.cs:L58` |
| REFACTOR | `CompareTag(tag)` in `GetAlphaForTag` — stringly-typed | Med | Use index-based lookup instead | `Assets/Scripts/Colors/ColorManager.cs:L118` |
| TEST | No tests for color swap state transitions | Med | Unit test `SetPlatformActive` with mock groups | `Assets/Scripts/Colors/ColorManager.cs:L94-L109` |
| DESIGN | No event fired on swap — dependents must poll | Med | Add `OnColorSwapped` event | `Assets/Scripts/Colors/ColorManager.cs:L60-L92` (swap logic with no event emission) |

---

## 6. LevelWin.cs

**Path:** `Assets/Scripts/LevelWin.cs` (39 lines)

**Summary:** Trigger-based win condition. On player contact: stops timer, unlocks
next level via PlayerPrefs, shows end-level menu.

**Key Invariants:**
- Requires a `Collider2D` set to trigger on the GameObject
- Player must have tag `"Player"` (`Assets/Scripts/LevelWin.cs:L8`)

**I/O:**
- Input: `OnTriggerEnter2D` with player collider
- Output: `FindObjectOfType<LevelTimer>().StopTimer()` (`Assets/Scripts/LevelWin.cs:L10`), `FindObjectOfType<EndLevelMenuManager>().ShowEndLevelMenu()` (`Assets/Scripts/LevelWin.cs:L17`)
- Side effects: `PlayerPrefs.SetInt("ReachedIndex", ...)`, `PlayerPrefs.SetInt("UnlockedLevel", ...)` (`Assets/Scripts/LevelWin.cs:L31-L34`)

**Dependencies:**
- Serialized: None
- Runtime lookup: `FindObjectOfType<LevelTimer>()` (`Assets/Scripts/LevelWin.cs:L10`), `FindObjectOfType<EndLevelMenuManager>()` (`Assets/Scripts/LevelWin.cs:L17`)
- Globals: `PlayerPrefs`, `SceneManager`

**Gotchas:**
- Two `FindObjectOfType` calls per win event — fragile if objects missing
- `UnlockNewLevel` compares `buildIndex >= PlayerPrefs.GetInt("ReachedIndex")` — default 0 if key missing
- No guard against double-trigger (player re-entering trigger)

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| REFACTOR | Two `FindObjectOfType` calls per win | Med | Inject via `[SerializeField]` references | `Assets/Scripts/LevelWin.cs:L10-L17` |
| BUG | No double-trigger guard | Med | Add `hasTriggered` bool flag | `Assets/Scripts/LevelWin.cs:L7-L18` |
| DESIGN | `PlayerPrefs` keys are magic strings | Med | Centralize key constants | `Assets/Scripts/LevelWin.cs:L31-L34` |
| REFACTOR | `UnlockedLevel` default `2` is magic number | Low | Define as named constant | `Assets/Scripts/LevelWin.cs:L33` |
| TEST | No test for unlock progression logic | High | Unit test `UnlockNewLevel` with mock PlayerPrefs | `Assets/Scripts/LevelWin.cs:L26-L35` |

---

## 7. PlayerStats.cs

**Path:** `Assets/Controller/Scripts/PlayerStats.cs` (191 lines)

**Summary:** ScriptableObject holding 50 public tuning fields (`Assets/Controller/Scripts/PlayerStats.cs:L13-L80`) for player movement,
jumping, dashing, wall climbing, ladder climbing, crouching, and collision.

**Key Invariants:**
- `CharacterSize` nested class with `GenerateCharacterSize()` for collider dimensions (`Assets/Controller/Scripts/PlayerStats.cs:L95-L167`)
- `OnValidate()` calls `FindObjectsByType<PlayerController>` to refresh all instances (`Assets/Controller/Scripts/PlayerStats.cs:L84`)

**I/O:**
- Input: Inspector / ScriptableObject asset edits
- Output: Read by `PlayerController` every physics tick

**Dependencies:**
- Serialized: None (is itself a serialized asset)
- Runtime lookup: `FindObjectsByType<PlayerController>` in `OnValidate` (`Assets/Controller/Scripts/PlayerStats.cs:L84`)
- Globals: None

**Gotchas:**
- 50 public fields — massive API surface; any rename breaks `PlayerController`
- `OnValidate` uses `FindObjectsByType` — editor-only perf concern with many instances
- No grouping beyond `[Header]` attributes; easy to set conflicting values

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| DESIGN | 50 public fields — flat structure | Med | Group into nested structs (MovementStats, JumpStats, etc.) | `Assets/Controller/Scripts/PlayerStats.cs:L13-L80` |
| PERF | `FindObjectsByType` in `OnValidate` scans all objects | Low | Cache or use event-based refresh | `Assets/Controller/Scripts/PlayerStats.cs:L84` |
| REFACTOR | `double` type for `HorizontalDeadZoneThreshold` (rest are `float`) | Low | Unify to `float` | `Assets/Controller/Scripts/PlayerStats.cs:L20` |
| REFACTOR | `double` type for `LadderCooldownTime` (rest are `float`) | Low | Unify to `float` | `Assets/Controller/Scripts/PlayerStats.cs:L68` |
| TEST | No validation tests for field ranges | Med | Add editor tests for min/max constraints | `Assets/Controller/Scripts/PlayerStats.cs:L13-L80` |

---

## 8. EndLevelMenuManager.cs

**Path:** `Assets/Scripts/EndLevelMenuManager.cs` (85 lines)

**Summary:** End-of-level UI panel showing current/best time, current/best grade,
with retry, next level, and main menu buttons.

**Key Invariants:**
- `Time.timeScale = 0f` when menu shown (`Assets/Scripts/EndLevelMenuManager.cs:L29`); `1f` on next level (`Assets/Scripts/EndLevelMenuManager.cs:L61`)
- Scene navigation by `buildIndex` (retry = current `Assets/Scripts/EndLevelMenuManager.cs:L51`, next = current+1 `Assets/Scripts/EndLevelMenuManager.cs:L68`, fallback = 0 `Assets/Scripts/EndLevelMenuManager.cs:L73`)

**I/O:**
- Input: `ShowEndLevelMenu(float currentTime, string currentGrade)` called by `LevelWin`
- Output: `SceneManager.LoadScene` by index (`Assets/Scripts/EndLevelMenuManager.cs:L51-L73`)
- Side effects: Reads `PlayerPrefs.GetFloat(levelKey)` (`Assets/Scripts/EndLevelMenuManager.cs:L38`), `PlayerPrefs.GetString(gradeKey)` (`Assets/Scripts/EndLevelMenuManager.cs:L45`)

**Dependencies:**
- Serialized: `TMP_Text` refs for time/grade display, `GameObject endLevelMenuUI` (`Assets/Scripts/EndLevelMenuManager.cs:L7-L13`)
- Runtime lookup: None
- Globals: `PlayerPrefs`, `SceneManager`, `Time.timeScale`

**Gotchas:**
- Duplicate `FormatTime` method (same as `LevelTimer`) — `Assets/Scripts/EndLevelMenuManager.cs:L77-L83`
- `NextLevel()` sets `Time.timeScale = 1f` but `RetryLevel()` and `BackToMainMenu()` do not
- Scene index arithmetic assumes contiguous level ordering

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| REFACTOR | Duplicate `FormatTime` — identical to `LevelTimer` | Med | Extract to shared static utility | `Assets/Scripts/EndLevelMenuManager.cs:L77-L83` vs `Assets/Scripts/LevelTimer.cs:L103-L113` |
| BUG | `RetryLevel`/`BackToMainMenu` don't reset `Time.timeScale` | High | Add `Time.timeScale = 1f` before scene load | `Assets/Scripts/EndLevelMenuManager.cs:L49-L57` |
| REFACTOR | Magic scene index `0` for main menu | Med | Use const or SceneReference | `Assets/Scripts/EndLevelMenuManager.cs:L56-L73` |
| DESIGN | `levelKey`/`gradeKey` duplicated from `LevelTimer` | Med | Centralize key generation | `Assets/Scripts/EndLevelMenuManager.cs:L26-L27` |
| TEST | No test for next-level boundary (last level → fallback) | Med | Test `NextLevel` when at max scene index | `Assets/Scripts/EndLevelMenuManager.cs:L66-L74` |

---

## 9. PlayerHealth.cs

**Path:** `Assets/Scripts/PlayerHealth.cs` (114 lines)

**Summary:** Health system with knockback, invincibility blink, and death → scene reload.

**Key Invariants:**
- `maxHealth` default 3; `currentHealth` set in `Awake` (`Assets/Scripts/PlayerHealth.cs:L30`)
- `isInvincible` flag prevents damage during blink coroutine (`Assets/Scripts/PlayerHealth.cs:L40`)
- Death triggers `RestartLevel()` coroutine (0-second delay, then scene reload — `Assets/Scripts/PlayerHealth.cs:L86-L90`)

**I/O:**
- Input: `TakeDamage(int damage, Vector2 sourcePosition)` called by `Hazard` (`Assets/Scripts/PlayerHealth.cs:L38`)
- Output: `SceneManager.LoadScene(activeScene.buildIndex)` on death (`Assets/Scripts/PlayerHealth.cs:L89`)
- Visual: Blinks `SpriteRenderer` during invincibility; updates `healthSprites` colors

**Dependencies:**
- Serialized: `SpriteRenderer spriteRenderer`, `SpriteRenderer[] healthSprites`, `Rigidbody2D` (via RequireComponent `Assets/Scripts/PlayerHealth.cs:L5`)
- Runtime lookup: None
- Globals: `SceneManager`

**Gotchas:**
- `RestartLevel` has `WaitForSeconds(0f)` — unnecessary coroutine overhead
- `spriteRenderer` is a `[SerializeField]` but no null-guard in `TakeDamage` path
- Empty `Start()` method (`Assets/Scripts/PlayerHealth.cs:L33-L36`)
- Scene reload on death resets all state; no checkpoint system

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| BUG | No null-guard on `spriteRenderer` in blink coroutine | Med | Add null check before color assignment | `Assets/Scripts/PlayerHealth.cs:L55-L70` |
| REFACTOR | `WaitForSeconds(0f)` — pointless delay | Low | Replace with direct `SceneManager.LoadScene` call | `Assets/Scripts/PlayerHealth.cs:L87` |
| REFACTOR | Empty `Start()` method | Low | Remove dead code | `Assets/Scripts/PlayerHealth.cs:L33-L36` |
| DESIGN | No checkpoint system — death reloads entire scene | Med | Add checkpoint/respawn system | `Assets/Scripts/PlayerHealth.cs:L86-L90` |
| DESIGN | `Debug.Log` in production damage path | Low | Wrap in `#if UNITY_EDITOR` or remove | `Assets/Scripts/PlayerHealth.cs:L44` |
| TEST | No tests for damage/invincibility/death flow | High | PlayMode test: damage → blink → death → reload | `Assets/Scripts/PlayerHealth.cs:L38-L90` |

---

## 10. RadialWorldSwapController.cs

**Path:** `Assets/Scripts/Colors/RadialWorldSwapController.cs` (99 lines)

**Summary:** Drives a radial wave animation that toggles `RadialToggleable` objects
between active/inactive states based on distance from a center point.

**Key Invariants:**
- `FindObjectsOfType<RadialToggleable>(true)` called once in `Awake` (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L29`)
- `AnimationCurve radiusCurve` controls radius interpolation over time
- Finalizes swap by calling `colorManager.SetIndexWithoutApplying` + `ApplyImmediate` (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L50-L51`)

**I/O:**
- Input: Legacy `Input.GetKeyDown(swapLeft/swapRight)` in `Update` (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L38-L39`)
- Output: Calls `RadialToggleable` methods per object per frame during animation (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L45`)
- Side effects: Mutates `ColorManager` state on swap completion (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L50-L51`)

**Dependencies:**
- Serialized: `ColorManager colorManager`, `Transform player`, `KeyCode swapLeft/swapRight`, `AnimationCurve radiusCurve`
- Runtime lookup: `FindObjectsOfType<RadialToggleable>(true)` in `Awake` (`Assets/Scripts/Colors/RadialWorldSwapController.cs:L29`)
- Globals: `UnityEngine.Input` (legacy)

**Gotchas:**
- `FindObjectsOfType` in `Awake` — O(n) scene scan; misses dynamically spawned objects
- Uses legacy `Input.GetKeyDown` — second instance of legacy input in project (alongside `MenuManager`)
- `Update()` runs wave calculation every frame during animation
- No guard against triggering new swap while one is in progress (only `!swapping` check at `Assets/Scripts/Colors/RadialWorldSwapController.cs:L36`)

Evidence: `Assets/Scripts/Colors/RadialWorldSwapController.cs:L38-L39`
```csharp
if (Input.GetKeyDown(swapLeft))  BeginSwap(-1);
if (Input.GetKeyDown(swapRight)) BeginSwap(+1);
```

**TODO Candidates:**

| Category | Problem | Risk | Suggested direction | Evidence |
|----------|---------|------|---------------------|----------|
| BUG | Legacy `Input.GetKeyDown` — breaks if Input System set to "New" only | High | Port to Input System action | `Assets/Scripts/Colors/RadialWorldSwapController.cs:L38-L39` |
| REFACTOR | `FindObjectsOfType` in `Awake` — misses late-spawned objects | Med | Use registry pattern or `FindObjectsByType` | `Assets/Scripts/Colors/RadialWorldSwapController.cs:L29` |
| PERF | Wave calculation runs every frame during swap | Low | Consider coroutine or job for large toggle counts | `Assets/Scripts/Colors/RadialWorldSwapController.cs:L42-L54` |
| DESIGN | Hardcoded `KeyCode` for swap input | Med | Use Input System action reference | `Assets/Scripts/Colors/RadialWorldSwapController.cs:L38-L39` |
| TEST | No tests for radial wave propagation logic | Med | Unit test `ApplyWave` with mock toggleables | `Assets/Scripts/Colors/RadialWorldSwapController.cs:L42-L54` |

<!-- END_CONTENT -->

