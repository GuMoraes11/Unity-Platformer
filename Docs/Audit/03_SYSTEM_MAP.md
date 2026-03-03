# 03 SYSTEM MAP
<!-- BEGIN_CONTENT -->

## High-Level Systems

### 1. Input

| Script | Role |
|--------|------|
| `Assets/Controller/Scripts/PlayerInput.cs` | Reads new Input System actions; returns `FrameInput` struct |
| `Assets/Controller/Input/PlayerInputActions.cs` | Auto-generated action map bindings (Move, Jump, Dash) |

Communication: `PlayerInput` → `PlayerController` (via `FrameInput` struct consumed each frame).

Evidence: `Assets/Controller/Scripts/PlayerInput.cs:L30-L37`
```csharp
public FrameInput Gather() {
    return new FrameInput {
        Move = _actions.Player.Move.ReadValue<Vector2>(),
        JumpDown = _actions.Player.Jump.WasPressedThisFrame(),
        JumpHeld = _actions.Player.Jump.IsPressed(),
        DashDown = _actions.Player.Dash.WasPressedThisFrame()
    };
}
```

### 2. Player Movement / Physics

| Script | Role |
|--------|------|
| `Assets/Controller/Scripts/PlayerController.cs` (1043 L) | Core movement: ground check, jump (coyote, wall, air), dash, crouch, ladder, wall-climb |
| `Assets/Controller/Scripts/PhysicsSimulator.cs` (81 L) | Singleton tick manager; drives `IPhysicsObject.TickUpdate` / `TickFixedUpdate` |
| `Assets/Controller/Scripts/PlayerStats.cs` (191 L) | ScriptableObject holding 50 public tuning fields (`Assets/Controller/Scripts/PlayerStats.cs:L13-L80`) |

Communication: `PhysicsSimulator.Instance` registers `IPhysicsObject` instances via `AddPlayer` /
`RemovePlayer`. Each frame, `Update()` calls `TickUpdate(delta, time)` on all registered objects;
each physics step, `FixedUpdate()` calls `TickFixedUpdate(delta)`.

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs:L22-L34`
```csharp
private void Update()
{
    var delta = Time.deltaTime;
    _time += delta;
    foreach (var platform in _platforms) { platform.TickUpdate(delta, _time); }
    foreach (var player in _players) { player.TickUpdate(delta, _time); }
}
```

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs:L37-L49`
```csharp
private void FixedUpdate()
{
    var delta = Time.deltaTime;
    foreach (var platform in _platforms) { platform.TickFixedUpdate(delta); }
    foreach (var player in _players) { player.TickFixedUpdate(delta); }
}
```

Evidence: `Assets/Controller/Scripts/PlayerController.cs:L94,L98`
```csharp
PhysicsSimulator.Instance.AddPlayer(this);
...
private void OnDestroy() => PhysicsSimulator.Instance.RemovePlayer(this);
```

### 3. Player Presentation (Animation / VFX / Audio)

| Script | Role |
|--------|------|
| `Assets/Controller/Scripts/PlayerAnimator.cs` (444 L) | Sprite animation, particle effects, audio clips, squish/stretch |

Communication: Subscribes to `PlayerController` events (`Jumped`, `GroundedChanged`,
`DashChanged`, `WallGrabChanged`). Reads `PlayerController` velocity for lean/animation state.

Evidence: `Assets/Controller/Scripts/PlayerAnimator.cs:L59-L62`
```csharp
_player.Jumped += OnJumped;
_player.GroundedChanged += OnGroundedChanged;
_player.DashChanged += OnDashChanged;
_player.WallGrabChanged += OnWallGrabChanged;
```

### 4. Health / Damage

| Script | Role |
|--------|------|
| `Assets/Scripts/PlayerHealth.cs` (114 L) | HP tracking, knockback, invincibility blink, death → scene reload |
| `Assets/Scripts/Colors/Hazard.cs` (30 L) | Trigger/stay damage dealer; calls `PlayerHealth.TakeDamage` |

Communication: `Hazard.OnTriggerEnter2D` → `PlayerHealth.TakeDamage(damage, sourcePosition)`.
On death, `PlayerHealth` reloads the current scene via `SceneManager.LoadScene`.

Evidence: `Assets/Scripts/Colors/Hazard.cs:L18-L27`
```csharp
void TryDealDamage(Collider2D collision) {
    if (collision.CompareTag("Player")) {
        PlayerHealth health = collision.GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(damageAmount, transform.position);
    }
}
```

Evidence: `Assets/Scripts/PlayerHealth.cs:L86-L90`
```csharp
private IEnumerator RestartLevel() {
    yield return new WaitForSeconds(0f);
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
```

### 5. World-Swap / Color System

| Script | Role |
|--------|------|
| `Assets/Scripts/Colors/ColorManager.cs` (132 L) | Manages color platform groups; toggles alpha + colliders |
| `Assets/Scripts/Colors/RadialWorldSwapController.cs` (99 L) | Radial wave animation driving per-object toggle |
| `Assets/Scripts/Colors/RadialToggleable.cs` (42 L) | Per-object state receiver for radial swap |
| `Assets/Scripts/Colors/Orange.cs` (88 L) | Crumbling platform (blink → disappear → respawn) |
| `Assets/Scripts/Colors/Yellow.cs` (42 L) | Glow-on-contact via URP Light2D |

Communication: `ColorManager` holds `List<ColorPlatform>` groups. `RadialWorldSwapController`
calls `FindObjectsOfType<RadialToggleable>()` in `Awake` and drives `ApplyState` per frame.
`Orange` queries `ColorManager.IsPlatformGroupCurrentlyActive` on respawn.

Evidence: `Assets/Scripts/Colors/RadialWorldSwapController.cs:L29`
```csharp
toggleables = FindObjectsOfType<RadialToggleable>();
```

Evidence: `Assets/Scripts/Colors/Orange.cs:L24`
```csharp
colorManager = Object.FindFirstObjectByType<ColorManager>();
```

### 6. Level Flow / Progression

| Script | Role |
|--------|------|
| `Assets/Scripts/LevelManager.cs` (17 L) | Assigns `LevelGradingData` to `LevelTimer` via `FindObjectOfType` |
| `Assets/Scripts/LevelTimer.cs` (135 L) | Tracks elapsed time, grades, saves best time/grade to PlayerPrefs |
| `Assets/Scripts/LevelWin.cs` (39 L) | Win trigger: stops timer, unlocks next level, shows end menu |
| `Assets/Scripts/LevelGradingData.cs` (25 L) | ScriptableObject with grade thresholds (S/A/B/F) |
| `Assets/Scripts/EndLevelMenuManager.cs` (85 L) | End-level UI: current/best time, grade, retry/next/menu |

Communication chain on level win:
1. `LevelWin.OnTriggerEnter2D` (player enters trigger)
2. → `FindObjectOfType<LevelTimer>().StopTimer()` (saves best time/grade to PlayerPrefs)
3. → `FindObjectOfType<EndLevelMenuManager>().ShowEndLevelMenu(time, grade)`
4. → `LevelWin.UnlockNewLevel()` (increments PlayerPrefs `UnlockedLevel` / `ReachedIndex`)

Evidence: `Assets/Scripts/LevelWin.cs:L8-L25`
```csharp
if (other.CompareTag("Player")) {
    LevelTimer timer = FindObjectOfType<LevelTimer>();
    if (timer != null) timer.StopTimer();
    UnlockNewLevel();
    EndLevelMenuManager endMenu = FindObjectOfType<EndLevelMenuManager>();
    if (endMenu != null && timer != null) {
        string grade = timer.GetCurrentGrade();
        endMenu.ShowEndLevelMenu(timer.GetCurrentTime(), grade);
    }
}
```

### 7. Menus / UI

| Script | Role |
|--------|------|
| `Assets/Scripts/MenuManager.cs` (104 L) | Main menu, pause menu, level select, quit, reset |
| `Assets/Scripts/GradeDisplay.cs` (71 L) | Displays saved grades per level with S-rank gold hue shift |
| `Assets/Scripts/FloatingTextEffect.cs` (34 L) | Cosmetic wobble + bob for UI text |

Communication: `MenuManager` uses `SceneManager.LoadScene` (by index or name) for navigation.
`GradeDisplay` reads `PlayerPrefs.GetString("Grade_{sceneName}")` per level.

Evidence: `Assets/Scripts/MenuManager.cs:L25`
```csharp
int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 2);
```

Evidence: `Assets/Scripts/GradeDisplay.cs:L28`
```csharp
string savedGrade = PlayerPrefs.GetString(gradeKey, "-");
```

### 8. Editor Tooling

| Script | Role |
|--------|------|
| `Assets/Controller/Scripts/Editor/ControllerSetupEditorWindow.cs` (105 L) | One-time layer setup (Player/Climbable/Ladders on layers 7/8/9) |

Communication: Editor-only (`#if UNITY_EDITOR`). Writes to `TagManager.asset` via `SerializedObject`.

Evidence: `Assets/Controller/Scripts/Editor/ControllerSetupEditorWindow.cs:L12-L17`
```csharp
private static readonly Dictionary<int, string> LayerDict = new() {
    { 7, "Player" },
    { 8, "Climbable" },
    { 9, "Ladders" }
};
```

### 9. Persistence (cross-cutting)

All persistence uses `PlayerPrefs` (no save file, no cloud save).

| Key Pattern | Writer | Reader |
|-------------|--------|--------|
| `BestTime_{sceneName}` | `LevelTimer.StopTimer` | `LevelTimer.GetBestTimeString`, `EndLevelMenuManager.ShowEndLevelMenu` |
| `Grade_{sceneName}` | `LevelTimer.StopTimer` | `GradeDisplay.Start`, `EndLevelMenuManager.ShowEndLevelMenu` |
| `UnlockedLevel` | `LevelWin.UnlockNewLevel` | `MenuManager.Start` |
| `ReachedIndex` | `LevelWin.UnlockNewLevel` | `LevelWin.UnlockNewLevel` |

Evidence: `Assets/Scripts/LevelTimer.cs:L39-L43`
```csharp
float bestTime = PlayerPrefs.GetFloat(levelKey, Mathf.Infinity);
if (currentTime < bestTime) {
    PlayerPrefs.SetFloat(levelKey, currentTime);
    PlayerPrefs.Save();
}
```

<!-- END_CONTENT -->

