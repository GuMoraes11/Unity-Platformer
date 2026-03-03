# 04 VOLATILITY
<!-- BEGIN_CONTENT -->

## Scoring Criteria

Each script is scored 1–5 on three axes:

| Axis | What it measures |
|------|-----------------|
| **Likelihood of change** | Feature requests, known TODOs, incomplete code, broad responsibility |
| **Blast radius** | How many other scripts / systems break if this file changes |
| **Coupling** | Number of direct dependencies (inbound + outbound) |

**Composite = Likelihood × 0.4 + Blast × 0.35 + Coupling × 0.25** (max 5.0)

## Top 15 Volatile Scripts

| Rank | Script | Lines | Likelihood | Blast | Coupling | Composite | Key Reason |
|-----:|--------|------:|-----------:|------:|---------:|----------:|------------|
| 1 | `PlayerController.cs` | 1043 | 5 | 5 | 5 | 5.00 | Largest file; drives physics, animation, input; touched by every feature |
| 2 | `PlayerAnimator.cs` | 444 | 4 | 4 | 4 | 4.00 | Tightly coupled to PlayerController events; 35 SerializeFields |
| 3 | `LevelTimer.cs` | 135 | 4 | 4 | 4 | 4.00 | Persistence hub; grading logic; read by LevelWin + EndLevelMenuManager |
| 4 | `MenuManager.cs` | 104 | 4 | 4 | 3 | 3.75 | Scene loading by index; legacy Input; level unlock gating |
| 5 | `ColorManager.cs` | 132 | 4 | 4 | 3 | 3.75 | Central to world-swap; queried by Orange, RadialWorldSwapController |
| 6 | `LevelWin.cs` | 39 | 4 | 4 | 3 | 3.75 | Win trigger; 2× FindObjectOfType; PlayerPrefs writes |
| 7 | `PlayerStats.cs` | 191 | 3 | 5 | 3 | 3.70 | 50 public fields (`Assets/Controller/Scripts/PlayerStats.cs:L13-L80`); any change ripples to PlayerController |
| 8 | `EndLevelMenuManager.cs` | 85 | 4 | 3 | 3 | 3.35 | Scene loading by index; duplicate FormatTime; PlayerPrefs reads |
| 9 | `PlayerHealth.cs` | 114 | 4 | 3 | 3 | 3.35 | Death → scene reload; invincibility coroutine; knockback |
| 10 | `RadialWorldSwapController.cs` | 99 | 3 | 3 | 3 | 3.00 | FindObjectsOfType in Awake; Update-driven wave animation |
| 11 | `PhysicsSimulator.cs` | 81 | 2 | 5 | 2 | 2.85 | Singleton; low change likelihood but catastrophic if broken |
| 12 | `Orange.cs` | 88 | 3 | 2 | 3 | 2.65 | FindFirstObjectByType; coroutine respawn; queries ColorManager |
| 13 | `GradeDisplay.cs` | 71 | 3 | 2 | 2 | 2.40 | Update loop for S-rank hue; PlayerPrefs reads |
| 14 | `PlayerInput.cs` | 59 | 2 | 3 | 2 | 2.35 | Stable wrapper; but blast radius if FrameInput struct changes |
| 15 | `Yellow.cs` | 42 | 2 | 1 | 2 | 1.65 | Self-contained; Light2D dependency |

## Evidence for Top 10 Volatility Rankings

### Rank 1 — PlayerController.cs
- 1043 lines, largest file in project.
  Evidence: `Assets/Controller/Scripts/PlayerController.cs:L1-L1043` (full file)
- 28 public fields; any external script can mutate state.
  Evidence: `Assets/Controller/Scripts/PlayerController.cs:L30-L57`
- Registers with singleton: `PhysicsSimulator.Instance.AddPlayer(this)`.
  Evidence: `Assets/Controller/Scripts/PlayerController.cs:L94`
- Stringly-typed surface detection: `CompareTag("Blue")`.
  Evidence: `Assets/Controller/Scripts/PlayerController.cs:L317-L319`

### Rank 2 — PlayerAnimator.cs
- 35 SerializeFields for particles, audio, sprites.
  Evidence: `Assets/Controller/Scripts/PlayerAnimator.cs:L10-L50`
- Both `Update()` and `LateUpdate()` run every frame.
  Evidence: `Assets/Controller/Scripts/PlayerAnimator.cs:L100-L120`

### Rank 3 — LevelTimer.cs
- Persistence hub: writes `BestTime_` and `Grade_` keys.
  Evidence: `Assets/Scripts/LevelTimer.cs:L39-L43`
- Duplicate `FormatTime` also in EndLevelMenuManager.
  Evidence: `Assets/Scripts/LevelTimer.cs:L103-L113`

### Rank 4 — MenuManager.cs
- Legacy `Input.GetKeyDown` mixed with new Input System.
  Evidence: `Assets/Scripts/MenuManager.cs:L36`
- Scene loading by magic index `0` and `1`.
  Evidence: `Assets/Scripts/MenuManager.cs:L61-L67`

### Rank 5 — ColorManager.cs
- Central to world-swap; `Orange.cs` reads public alpha fields directly.
  Evidence: `Assets/Scripts/Colors/Orange.cs:L58`
- `Update()` polls input every frame when `handleInput` is true.
  Evidence: `Assets/Scripts/Colors/ColorManager.cs:L90-L100`

### Rank 6 — LevelWin.cs
- Two `FindObjectOfType` calls per win event.
  Evidence: `Assets/Scripts/LevelWin.cs:L10-L17`
- PlayerPrefs writes for unlock tracking.
  Evidence: `Assets/Scripts/LevelWin.cs:L31-L34`

### Rank 7 — PlayerStats.cs
- 50 public fields; massive API surface.
  Evidence: `Assets/Controller/Scripts/PlayerStats.cs:L13-L80`
- `OnValidate` calls `FindObjectsByType<PlayerController>`.
  Evidence: `Assets/Controller/Scripts/PlayerStats.cs:L180-L191`

### Rank 8 — EndLevelMenuManager.cs
- Duplicate `FormatTime` method.
  Evidence: `Assets/Scripts/EndLevelMenuManager.cs:L77-L83`
- `NextLevel()` resets `Time.timeScale` but `RetryLevel()` does not.
  Evidence: `Assets/Scripts/EndLevelMenuManager.cs:L49-L68`

### Rank 9 — PlayerHealth.cs
- Death restarts scene via coroutine with `WaitForSeconds(0f)`.
  Evidence: `Assets/Scripts/PlayerHealth.cs:L86-L90`
- No null-guard on `spriteRenderer` in `TakeDamage` path.
  Evidence: `Assets/Scripts/PlayerHealth.cs:L55-L70`

### Rank 10 — RadialWorldSwapController.cs
- `FindObjectsOfType<RadialToggleable>()` in `Awake`.
  Evidence: `Assets/Scripts/Colors/RadialWorldSwapController.cs:L29`
- `Update()` runs wave calculation every frame during animation.
  Evidence: `Assets/Scripts/Colors/RadialWorldSwapController.cs:L40-L60`

## Scripts Below Threshold (not ranked)

| Script | Lines | Reason for exclusion |
|--------|------:|----------------------|
| `Hazard.cs` | 30 | Minimal; self-contained damage dealer |
| `RadialToggleable.cs` | 42 | Pure data receiver; no outbound deps |
| `FloatingTextEffect.cs` | 34 | Cosmetic only; zero coupling |
| `LevelGradingData.cs` | 25 | ScriptableObject; stable schema |
| `LevelManager.cs` | 17 | Trivial glue; single FindObjectOfType |
| `ControllerSetupEditorWindow.cs` | 105 | Editor-only; does not affect runtime |

<!-- END_CONTENT -->

