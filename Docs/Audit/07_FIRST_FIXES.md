# 07 FIRST FIXES
<!-- BEGIN_CONTENT -->

## Unity Version Consistency Check

| Item | Value |
|------|-------|
| **Actual version** (ProjectVersion.txt) | `6000.0.40f1` |
| **Intended version** (per team) | `6000.2.7f2` |
| **Match?** | **NO — MISMATCH** |

Evidence: `ProjectSettings/ProjectVersion.txt:L1`
```
m_EditorVersion: 6000.0.40f1
```

**Risk:** Unity 6000.0.x (Unity 6 Preview) → 6000.2.x (Unity 6.2) is a minor version jump.
API changes, package compatibility shifts, and serialized asset format changes are possible.
Opening the project in 6000.2.7f2 will trigger a one-way upgrade of `.meta` files and
serialized assets.

**Suggested workflow:**
1. Back up the project (or ensure clean git state with no uncommitted changes)
2. Open in Unity 6000.2.7f2
3. Let the asset database reimport
4. Fix any compilation errors from API changes
5. Run all scenes to verify no regressions
6. Commit the upgrade as a single atomic commit with message: `chore: upgrade Unity 6000.0.40f1 → 6000.2.7f2`

---

## Top 5 High-Risk, Low-Effort Fixes

### Fix 1: Extract shared `FormatTime` utility

**Risk:** High — duplicate code will diverge silently
**Effort:** Low (~15 min)
**Files:** `Assets/Scripts/LevelTimer.cs:L103-L113`, `Assets/Scripts/EndLevelMenuManager.cs:L77-L83`

**Problem:** Two identical `FormatTime(float)` methods exist. If one is updated (e.g., to show
3 decimal places), the other will display differently.

**Fix:** Extract to a static utility class (e.g., `TimeFormatUtil.FormatTime`) and call from both.

Evidence: `Assets/Scripts/LevelTimer.cs:L103-L108`
```csharp
private static string FormatTime(float time) {
    int minutes = Mathf.FloorToInt(time / 60f);
    int seconds = Mathf.FloorToInt(time % 60f);
    int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
```

Evidence: `Assets/Scripts/EndLevelMenuManager.cs:L77-L83`
```csharp
private string FormatTime(float time) {
    int minutes = Mathf.FloorToInt(time / 60f);
    int seconds = Mathf.FloorToInt(time % 60f);
    int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
    return minutes > 0 ? $"{minutes}:{seconds:00}.{milliseconds:00}" : $"{seconds}.{milliseconds:00}";
}
```

---

### Fix 2: Replace `SceneManager.LoadScene` magic indices with constants or scene name references

**Risk:** High — build order change breaks all navigation
**Effort:** Low (~20 min)
**Files:** `Assets/Scripts/MenuManager.cs:L61,L67,L83`, `Assets/Scripts/EndLevelMenuManager.cs:L51,L56,L68,L73`

**Problem:** 7 calls use hardcoded scene indices (`0`, `1`, `currentIndex+1`). Reordering
scenes in Build Settings silently breaks navigation.

**Fix:** Define scene name constants (e.g., `const string MainMenuScene = "MainMenu"`) and
use `SceneManager.LoadScene(MainMenuScene)`. Or use an enum-to-name mapping.

Evidence: `Assets/Scripts/MenuManager.cs:L61`
```csharp
SceneManager.LoadScene(0); // Main Menu
```

Evidence: `Assets/Scripts/EndLevelMenuManager.cs:L56`
```csharp
SceneManager.LoadScene(0); // Assuming Main Menu is scene 0
```

---

### Fix 3: Replace `FindObjectOfType` calls with serialized references

**Risk:** Medium-High — silent null if object missing; O(n) scan
**Effort:** Low (~15 min)
**Files:** `Assets/Scripts/LevelManager.cs:L10`, `Assets/Scripts/LevelWin.cs:L10,L17`, `Assets/Scripts/Colors/Orange.cs:L24`

**Problem:** 4 runtime `FindObjectOfType` / `FindFirstObjectByType` calls. These are O(n)
scene scans and return null silently if the target is missing, causing downstream NullReferenceExceptions.

**Fix:** Replace with `[SerializeField] private LevelTimer timer;` (etc.) and assign in the
inspector. This makes dependencies explicit and fails loudly at edit-time if unassigned.

Evidence: `Assets/Scripts/LevelWin.cs:L10`
```csharp
LevelTimer timer = FindObjectOfType<LevelTimer>();
```

---

### Fix 4: Fix `MenuManager` legacy Input usage

**Risk:** Medium — pause menu silently broken if Input System backend set to "New" only
**Effort:** Low (~10 min)
**Files:** `Assets/Scripts/MenuManager.cs:L36`

**Problem:** `Input.GetKeyDown(KeyCode.Escape)` uses the legacy input API. The project uses
the new Input System for player controls. If the project's Active Input Handling is set to
"Input System Package (New)" only, this call will silently do nothing.

**Fix:** Add a "Pause" action to the Input System action map and read it via the new API,
or ensure Active Input Handling is set to "Both" in Player Settings.

Evidence: `Assets/Scripts/MenuManager.cs:L36`
```csharp
if (pauseMenuUI != null && Input.GetKeyDown(KeyCode.Escape))
```

---

### Fix 5: Guard `PhysicsSimulator` singleton against duplicates

**Risk:** Medium — duplicate simulators cause double-ticking of physics
**Effort:** Low (~5 min)
**Files:** `Assets/Controller/Scripts/PhysicsSimulator.cs:L72-L78`

**Problem:** The `[RuntimeInitializeOnLoadMethod]` bootstrapper creates a new
`PhysicsSimulator` GameObject every time without checking if one already exists.
The `Instance` property setter does not destroy duplicates.

**Fix:** Add a guard: `if (Instance != null) return;` at the top of `Bootstrap()`,
or add duplicate destruction in the `Instance` setter / `Awake`.

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs:L72-L78`
```csharp
[RuntimeInitializeOnLoadMethod]
private static void Bootstrap() {
    var simulator = new GameObject("PhysicsSimulator");
    simulator.AddComponent<PhysicsSimulator>();
    DontDestroyOnLoad(simulator);
}
```

---

## Summary Priority Matrix

| Fix | Risk | Effort | Priority |
|-----|------|--------|----------|
| Unity version upgrade (6000.0.40f1 → 6000.2.7f2) | High | Medium | **P0** |
| Fix 1: Extract FormatTime | High | Low | **P1** |
| Fix 2: Scene index constants | High | Low | **P1** |
| Fix 3: Replace FindObjectOfType | Medium-High | Low | **P1** |
| Fix 4: Legacy Input in MenuManager | Medium | Low | **P2** |
| Fix 5: Singleton duplicate guard | Medium | Low | **P2** |

<!-- END_CONTENT -->

