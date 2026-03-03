# 00 META
<!-- BEGIN_CONTENT -->

## Unity Editor Version
- Unity Editor: `6000.0.40f1` (revision `157d81624ddf`)
  Evidence: ProjectSettings/ProjectVersion.txt:L1-L2

## Packages (`Packages/manifest.json`)
| Package | Version |
|---|---|
| com.unity.render-pipelines.universal | 17.0.4 |
| com.unity.inputsystem | 1.13.0 |
| com.unity.cinemachine | 3.1.3 |
| com.unity.timeline | 1.8.7 |
| com.unity.test-framework | 1.4.6 |
| com.unity.ugui | 2.0.0 |
| com.unity.visualscripting | 1.9.5 |

Evidence: Packages/manifest.json:L2-L15

## Render Pipeline (URP)
- URP is installed via package `com.unity.render-pipelines.universal@17.0.4`.
  Evidence: Packages/manifest.json:L2-L12
- Project `GraphicsSettings` points `m_CustomRenderPipeline` to an asset with GUID `681886c5eb7344803b6206f758bf0b1c`.
  Evidence: ProjectSettings/GraphicsSettings.asset:L37-L45
- `GraphicsSettings` includes URP Global Settings mapping for `UnityEngine.Rendering.Universal.UniversalRenderPipeline` with GUID `93b439a37f63240aca3dd4e01d978a9f`.
  Evidence: ProjectSettings/GraphicsSettings.asset:L60-L62
- URP Pipeline Asset name is `UniversalRP`.
  Evidence: Assets/Settings/UniversalRP.asset:L12-L21
- `UniversalRP.asset.meta` GUID matches `GraphicsSettings.m_CustomRenderPipeline` GUID (`681886c5eb7344803b6206f758bf0b1c`).
  Evidence: Assets/Settings/UniversalRP.asset.meta:L1-L6

## Input System
- New Input System is installed via package `com.unity.inputsystem@1.13.0`.
  Evidence: Packages/manifest.json:L2-L12
- Build Settings includes `com.unity.input.settings.actions` config object with GUID `2bcd2660ca9b64942af0de543d8d7100`.
  Evidence: ProjectSettings/EditorBuildSettings.asset:L35-L37
- `Assets/Settings/InputSystem_Actions.inputactions` has matching GUID in its `.meta`.
  Evidence: Assets/Settings/InputSystem_Actions.inputactions.meta:L1-L3

## Compile Symbols (Scripting Define Symbols)
- `ProjectSettings/ProjectSettings.asset` currently shows `scriptingDefineSymbols: {}`.
  This means **no custom scripting defines** are set for any platform.
  Evidence: ProjectSettings/ProjectSettings.asset:L577-L579

## Active Input Handling
- `activeInputHandler: 2` → **Both** (legacy Input Manager + new Input System Package active simultaneously).
  Evidence: ProjectSettings/ProjectSettings.asset:L674
  ```
  activeInputHandler: 2
  ```
- This explains why `MenuManager.cs` can use `Input.GetKeyDown(KeyCode.Escape)` (legacy) alongside
  the new Input System used by `PlayerInput.cs`. Both backends are active.
- **Risk:** "Both" mode has a small runtime overhead. If the project switches to "New" only (value `1`),
  all legacy `Input.*` calls (e.g., `MenuManager.cs:L36`) will silently stop working.

## Unity Version Consistency Check

| Item | Value |
|------|-------|
| **Actual version** (ProjectVersion.txt) | `6000.0.40f1` |
| **Intended version** (per team) | `6000.2.7f2` |
| **Match?** | **NO — MISMATCH** |

Evidence: ProjectSettings/ProjectVersion.txt:L1
```
m_EditorVersion: 6000.0.40f1
```

**Risk:** Unity 6000.0.x → 6000.2.x is a minor version jump. API changes, package compatibility
shifts, and serialized asset format changes are possible. Opening the project in 6000.2.7f2 will
trigger a one-way upgrade of `.meta` files and serialized assets.

**Recommended workflow:**
1. Ensure clean git state (no uncommitted changes)
2. Open project in Unity 6000.2.7f2
3. Let the asset database reimport
4. Fix any compilation errors from API changes (e.g., `Rigidbody2D.linearVelocity` API)
5. Run all 9 build scenes to verify no regressions
6. Commit as a single atomic commit: `chore: upgrade Unity 6000.0.40f1 → 6000.2.7f2`

<!-- END_CONTENT -->

