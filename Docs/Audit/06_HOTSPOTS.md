# 06 HOTSPOTS
<!-- BEGIN_CONTENT -->

## Anti-Pattern Summary

| Pattern | Instances | Risk |
|---------|----------:|------|
| `FindObjectOfType` / `FindFirstObjectByType` / `FindObjectsOfType` | 3 + 1 + 1 = 5 | O(n) scene scan every call; fragile if object missing |
| `PlayerPrefs` for game state | 16 | No encryption, no versioning, platform-dependent |
| `SceneManager.LoadScene` by index | 7 | Breaks if build order changes |
| `SceneManager.LoadScene` by name | 2 | Breaks if scene renamed |
| `CompareTag` with string literals | 7 | Stringly-typed; silent fail if tag missing |
| `Input.GetKeyDown` (legacy input) | 1 | Mixes legacy + new Input System |
| `DontDestroyOnLoad` singleton | 1 | No duplicate-prevention on re-entry |
| Heavy `Update()` loops | 3 | Per-frame work that could be event-driven |
| Duplicate `FormatTime` methods | 2 | Copy-paste; divergence risk |

---

### H1. FindObjectOfType / FindFirstObjectByType / FindObjectsOfType (3 + 1 + 1 = 5 instances)

| File | Line | API | Call |
|------|-----:|-----|------|
| `Assets/Scripts/LevelManager.cs` | L10 | `FindObjectOfType` | `FindObjectOfType<LevelTimer>()` |
| `Assets/Scripts/LevelWin.cs` | L10 | `FindObjectOfType` | `FindObjectOfType<LevelTimer>()` |
| `Assets/Scripts/LevelWin.cs` | L17 | `FindObjectOfType` | `FindObjectOfType<EndLevelMenuManager>()` |
| `Assets/Scripts/Colors/Orange.cs` | L24 | `FindFirstObjectByType` | `Object.FindFirstObjectByType<ColorManager>()` |
| `Assets/Scripts/Colors/RadialWorldSwapController.cs` | L29 | `FindObjectsOfType` | `FindObjectsOfType<RadialToggleable>(true)` |

Evidence: `Assets/Scripts/LevelWin.cs:L10-L17`
```csharp
LevelTimer timer = FindObjectOfType<LevelTimer>();
if (timer != null) timer.StopTimer();
UnlockNewLevel();
EndLevelMenuManager endMenu = FindObjectOfType<EndLevelMenuManager>();
```

Evidence: `Assets/Scripts/Colors/Orange.cs:L24`
```csharp
colorManager = Object.FindFirstObjectByType<ColorManager>();
```

### H2. PlayerPrefs for Game State (16 instances across 5 files)

| File | Lines | Keys |
|------|-------|------|
| `Assets/Scripts/LevelTimer.cs` | L39,L42-43,L62,L65-66,L99 | `BestTime_{scene}`, `Grade_{scene}` |
| `Assets/Scripts/LevelWin.cs` | L31-34 | `ReachedIndex`, `UnlockedLevel` |
| `Assets/Scripts/EndLevelMenuManager.cs` | L38,L45 | `BestTime_{scene}`, `Grade_{scene}` |
| `Assets/Scripts/MenuManager.cs` | L25,L82 | `UnlockedLevel`, `DeleteAll` |
| `Assets/Scripts/GradeDisplay.cs` | L28 | `Grade_{scene}` |

Risk: PlayerPrefs is unencrypted, stored in registry (Windows) or plist (macOS).
No versioning or migration strategy. `DeleteAll` in `MenuManager.ResetGame` wipes everything.

Evidence: `Assets/Scripts/MenuManager.cs:L80-L84`
```csharp
public void ResetGame() {
    PlayerPrefs.DeleteAll();
    SceneManager.LoadScene(0);
}
```

### H3. SceneManager.LoadScene by Index (7) + by Name (2)

**By index:**

| File | Line | Index |
|------|-----:|------:|
| `Assets/Scripts/MenuManager.cs` | L61 | `0` (Main Menu) |
| `Assets/Scripts/MenuManager.cs` | L67 | `1` (Level 1) |
| `Assets/Scripts/MenuManager.cs` | L83 | `0` (Main Menu) |
| `Assets/Scripts/EndLevelMenuManager.cs` | L51 | active scene index (retry) |
| `Assets/Scripts/EndLevelMenuManager.cs` | L56 | `0` (Main Menu) |
| `Assets/Scripts/EndLevelMenuManager.cs` | L68 | `nextSceneIndex` |
| `Assets/Scripts/EndLevelMenuManager.cs` | L73 | `0` (fallback) |

**By name:**

| File | Line | Name |
|------|-----:|------|
| `Assets/Scripts/MenuManager.cs` | L89 | `"Level " + levelId` |
| `Assets/Scripts/PlayerHealth.cs` | L89 | active scene (by buildIndex) |

Evidence: `Assets/Scripts/MenuManager.cs:L61`
```csharp
SceneManager.LoadScene(0); // Main Menu
```

Evidence: `Assets/Scripts/MenuManager.cs:L89`
```csharp
SceneManager.LoadScene("Level " + levelId);
```

### H4. CompareTag with String Literals (7 instances)

| File | Line | Tag |
|------|-----:|-----|
| `Assets/Scripts/LevelWin.cs` | L8 | `"Player"` |
| `Assets/Scripts/Colors/Orange.cs` | L29 | `"Player"` |
| `Assets/Scripts/Colors/Yellow.cs` | L16 | `"Player"` |
| `Assets/Scripts/Colors/Yellow.cs` | L28 | `"Player"` |
| `Assets/Scripts/Colors/Hazard.cs` | L20 | `"Player"` |
| `Assets/Controller/Scripts/PlayerController.cs` | L317 | `"Blue"` (slippery surface) |
| `Assets/Controller/Scripts/PlayerController.cs` | L319 | `"Blue"` (slippery surface) |

Risk: If the tag is not defined in TagManager, `CompareTag` throws at runtime.
No compile-time safety.

Evidence: `Assets/Controller/Scripts/PlayerController.cs:L317-L319`
```csharp
if (hit.collider.CompareTag("Blue")) { ... }
```

### H5. Legacy Input Mixed with New Input System (1 instance)

| File | Line | Call |
|------|-----:|------|
| `Assets/Scripts/MenuManager.cs` | L36 | `Input.GetKeyDown(KeyCode.Escape)` |

Evidence: `Assets/Scripts/MenuManager.cs:L36`
```csharp
if (pauseMenuUI != null && Input.GetKeyDown(KeyCode.Escape))
```

Risk: Project uses new Input System (`PlayerInput.cs`), but `MenuManager` uses legacy
`UnityEngine.Input`. If Input System backend is set to "New" only, this call silently fails.

### H6. DontDestroyOnLoad Singleton (1 instance)

| File | Line |
|------|-----:|
| `Assets/Controller/Scripts/PhysicsSimulator.cs` | L78 |

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs:L72-L78`
```csharp
[RuntimeInitializeOnLoadMethod]
private static void Bootstrap() {
    var simulator = new GameObject("PhysicsSimulator");
    simulator.AddComponent<PhysicsSimulator>();
    DontDestroyOnLoad(simulator);
}
```

Risk: No guard against duplicate creation if `Bootstrap` runs again. The `Instance`
property setter does not destroy duplicates.

### H7. Heavy Update() Loops (3 notable)

| File | Method | Concern |
|------|--------|---------|
| `Assets/Scripts/GradeDisplay.cs:L46-L57` | `Update()` | Iterates all `levelGrades` every frame for S-rank hue shift |
| `Assets/Scripts/Colors/Yellow.cs:L34-L41` | `Update()` | Runs every frame even when player is not on platform |
| `Assets/Scripts/Colors/ColorManager.cs` | `Update()` | Polls input every frame (if `handleInput` enabled) |

Evidence: `Assets/Scripts/GradeDisplay.cs:L46-L57`
```csharp
private void Update() {
    foreach (var levelGrade in levelGrades) {
        if (levelGrade.gradeText != null && levelGrade.gradeText.text == "S") {
            float hue = Mathf.Lerp(0.12f, 0.16f, Mathf.PingPong(Time.time * 0.5f, 1f));
            Color shiftingGold = Color.HSVToRGB(hue, 0.8f, 1f);
            levelGrade.gradeText.color = shiftingGold;
        }
    }
}
```

### H8. Duplicate FormatTime Methods (2 copies)

| File | Line |
|------|-----:|
| `Assets/Scripts/LevelTimer.cs` | L103-L113 |
| `Assets/Scripts/EndLevelMenuManager.cs` | L77-L83 |

Both implement identical minutes:seconds.milliseconds formatting.
Risk: If one is updated and the other is not, displayed times will diverge.

Evidence: `Assets/Scripts/LevelTimer.cs:L103-L108`
```csharp
private static string FormatTime(float time) {
    int minutes = Mathf.FloorToInt(time / 60f);
    int seconds = Mathf.FloorToInt(time % 60f);
    int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
```

<!-- END_CONTENT -->

