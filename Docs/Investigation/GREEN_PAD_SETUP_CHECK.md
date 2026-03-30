# Green Pad Setup Check — Hierarchy, Layers, and False Grounding

## 1) Hierarchy / Transform Sanity

### Expected hierarchy (from GreenPlatform.cs header, lines 5–17)

```
GreenPadRoot          (GreenPlatform script, optional SpriteRenderer)
├── OuterZone         (BoxCollider2D [trigger] + GreenPadZoneRelay [Outer])
└── InnerZone         (BoxCollider2D [trigger] + GreenPadZoneRelay [Inner])
```

### Should Inner be a sibling of Outer, or nested under Outer?

**Sibling.** Both are direct children of the root. The design uses ref-counted
overlap tracking (`outerCount`, `innerCount`) on the GreenPlatform root script.
`GreenPadZoneRelay.Awake()` calls `GetComponentInParent<GreenPlatform>()` to find
its owner (GreenPadZoneRelay.cs:L30–L31). If Inner were nested under Outer, this
would still work (it walks up the hierarchy), but it creates unnecessary coupling
and makes it harder to position the zones independently.

### What happens if Inner is nested under Outer with a large negative local Y offset?

If Inner is a child of Outer and has a large negative local Y:

1. **Owner resolution still works** — `GetComponentInParent<GreenPlatform>()` walks
   up through Outer to Root and finds the GreenPlatform.

2. **The Inner zone may extend below the Outer zone.** The design expects Inner to
   be physically contained within Outer. If Inner extends outside Outer, a player
   could enter Inner without entering Outer. In that case:
   - `NotifyZoneEnter` increments `innerCount` but `outerCount` remains 0.
   - No launch is queued (launch requires `outerCount > 0` transition).
   - The bonus check (`state.innerCount > 0`) would be true, but the launch never
     fires, so the bonus is never applied.
   - **Result: player enters Inner, nothing happens.** No crash, but no bounce either.

3. **Moving Outer moves Inner** — since Inner is a child, any transform change on
   Outer also moves Inner. This is usually undesirable for independent zone sizing.

### Does the design expect Inner to be physically inside Outer?

**Yes.** The entire design assumes:
- Outer is the larger detection volume (bounce trigger).
- Inner is a smaller volume fully contained within Outer (bonus region).
- A player entering Inner has necessarily already entered Outer.

Evidence: GreenPlatform.cs:L189 — bonus check is `state.innerCount > 0`, but launch
is only queued on outer 0→1 transition (L231–L235). If Inner extends outside Outer,
the bonus path is unreachable.

---

## 2) Layer / Mask Sanity

### Project layer table (from ProjectSettings/TagManager.asset)

| Index | Name           |
|-------|----------------|
| 0     | Default        |
| 1     | TransparentFX  |
| 2     | Ignore Raycast |
| 3     | Ground         |
| 4     | Water          |
| 5     | UI             |
| 6     | (empty)        |
| 7     | Player         |
| 8     | Climbable      |
| 9     | Ladders        |
| 10–31 | (empty)        |

### PlayerStats asset values (Assets/Controller/Stat Presets/Player Stats.asset)

| Field            | m_Bits | Binary             | Layers included                        |
|------------------|--------|--------------------|----------------------------------------|
| PlayerLayer      | 128    | `0000010000000`    | 7 (Player)                             |
| CollisionLayers  | 777    | `1100001001`       | **0 (Default), 3 (Ground), 8 (Climbable), 9 (Ladders)** |
| ClimbableLayer   | 264    | `0100001000`       | 3 (Ground), 8 (Climbable)              |
| LadderLayer      | 512    | `1000000000`       | 9 (Ladders)                            |

### Verification: 777 = 1 + 8 + 256 + 512 = bits 0, 3, 8, 9 ✓

### Would putting Green trigger zones on layer "Default" (0) cause them to be treated as ground?

**YES.** Layer 0 (Default) is included in `CollisionLayers` (bit 0 is set in 777).
The ground raycast at PlayerController.cs:L360 uses `Stats.CollisionLayers` as its
layer mask. If a trigger collider is on Default and `Physics2D.queriesHitTriggers`
is true, the raycast will hit it.

### Are trigger colliders on CollisionLayers included in ground checks?

**YES — this is the critical finding.**

The global Physics2D setting is:
```
m_QueriesHitTriggers: 1    (ProjectSettings/Physics2DSettings.asset)
```

`CalculateCollisions()` (PlayerController.cs:L330–L370) does **NOT** toggle
`Physics2D.queriesHitTriggers`. It only toggles `queriesStartInColliders`:

```csharp
// PlayerController.cs:L332–L335
var prevQueryStartIn = Physics2D.queriesStartInColliders;
Physics2D.queriesStartInColliders = false;
```

The ground raycast at L360:
```csharp
_groundHit = Physics2D.Raycast(point, -Up, GrounderLength + _currentStepDownLength, Stats.CollisionLayers);
```

Since `queriesHitTriggers` remains at its global default (`true`), this raycast
**will hit trigger colliders** on any layer in `CollisionLayers`.

**Contrast with other code that does toggle it:**
- `CalculateLadders()` (L570–L578): explicitly sets `queriesHitTriggers = true` then restores.
- `CheckPos()` (L769–L780): explicitly sets `queriesHitTriggers = false` then restores.
- `CalculateCollisions()`: **does not touch it at all**.

### Summary: which layers are safe for Green zones?

| Layer          | In CollisionLayers? | Safe for Green zones? |
|----------------|---------------------|-----------------------|
| Default (0)    | **YES**             | **NO** — will be detected as ground |
| TransparentFX (1) | No              | Yes                   |
| Ignore Raycast (2) | No             | Yes (but raycasts ignore it by convention) |
| Ground (3)     | **YES**             | **NO** — will be detected as ground |
| Water (4)      | No                  | Yes                   |
| UI (5)         | No                  | Yes                   |
| (empty) (6)    | No                  | Yes                   |
| Player (7)     | No                  | Risky — may cause self-interaction |
| Climbable (8)  | **YES**             | **NO** — will be detected as ground |
| Ladders (9)    | **YES**             | **NO** — will be detected as ground |

---

## 3) False Grounding Risk

### The attack path (step by step)

1. Green zone trigger collider is on a layer in `CollisionLayers` (e.g., Default or Ground).

2. Player falls toward the green pad. The ground raycast in `CalculateCollisions()`
   (PlayerController.cs:L360) fires downward with `Stats.CollisionLayers` mask.

3. `Physics2D.queriesHitTriggers` is globally `true` (Physics2DSettings.asset:
   `m_QueriesHitTriggers: 1`). `CalculateCollisions` does NOT set it to `false`.

4. The raycast hits the green zone's trigger collider. `_groundHit` is set.
   `PerformRay` returns `true` (assuming the normal angle check passes — a flat
   horizontal trigger zone has normal = (0,1), angle vs Up = 0°, which passes
   `MaxWalkableSlope = 46`).

5. `isGroundedThisFrame` becomes `true`. If `!_grounded`, `ToggleGrounded(true)` fires.

6. **`ToggleGrounded(true)`** (PlayerController.cs:L395–L407):
   - `_rb.gravityScale = 0` — disables gravity
   - **`SetVelocity(_trimmedFrameVelocity)`** — `_trimmedFrameVelocity.y` is always 0
     (set at L269: `new Vector2(Velocity.x, 0)`). **This zeros vertical velocity.**
   - `_constantForce.force = Vector2.zero` — disables extra gravity
   - `_canDash = true` — resets dash
   - `_coyoteUsable = true` — resets coyote time
   - `_bufferedJumpUsable = true` — resets jump buffer
   - `ResetAirJumps()` — resets air jumps

7. **Consequence**: The player's downward velocity is zeroed. When GreenPlatform's
   `FixedUpdate` reads `pc.Velocity` to compute the reflection, `vIn.y ≈ 0`.
   The reflected velocity `vOut.y ≈ 0`. **The bounce produces no upward force.**

### Evidence chain

| Step | File | Lines | What |
|------|------|-------|------|
| Global default | ProjectSettings/Physics2DSettings.asset | `m_QueriesHitTriggers: 1` | Triggers are hit by queries |
| No override in ground check | PlayerController.cs | L330–L356 | `CalculateCollisions` only toggles `queriesStartInColliders`, not `queriesHitTriggers` |
| Raycast uses CollisionLayers | PlayerController.cs | L360 | `Physics2D.Raycast(..., Stats.CollisionLayers)` |
| CollisionLayers includes Default | Player Stats.asset | `m_Bits: 777` | Bit 0 (Default) is set |
| Grounding zeros Y velocity | PlayerController.cs | L399 | `SetVelocity(_trimmedFrameVelocity)` where `.y = 0` (L269) |
| GreenPlatform reads velocity | GreenPlatform.cs | L180 | `Vector2 vIn = pc.Velocity` |

### Is this currently happening?

**Unknown from code alone.** It depends on what layer the Green zone GameObjects are
actually set to in the scene. If they are on Default (the Unity default for new
GameObjects), then **yes, this is actively causing false grounding and broken bounces.**

The current `OnValidate` warning in GreenPlatform.cs only checks for the "Ground"
layer name (L328). It does **not** check for Default, Climbable, or Ladders — all of
which are also in `CollisionLayers`.

The runtime `ValidateZonesRuntime()` added in the previous fix does check against
the full `CollisionLayers` mask, but only at `Awake` time and only if a
`PlayerController` is found.

---

## 4) Recommendations

### Correct layer for Outer/Inner zones

Create a dedicated layer (e.g., **"Triggers"** or **"PlatformTriggers"**) that is
**NOT** included in `CollisionLayers`, `ClimbableLayer`, or `LadderLayer`. Assign
both Outer and Inner zones to this layer.

If creating a new layer is not desired, use any existing layer not in
`CollisionLayers` (777): layers 1, 2, 4, 5, or 6. Layer 6 (unnamed) is the safest
choice since it has no existing purpose.

**Do NOT use Default (0).** Unity assigns new GameObjects to Default, and Default is
in `CollisionLayers`. This is the most likely current misconfiguration.

### Correct hierarchy

```
GreenPadRoot          [Layer: any]  (GreenPlatform script)
├── OuterZone         [Layer: NOT in CollisionLayers]  (BoxCollider2D [trigger] + GreenPadZoneRelay [Outer])
└── InnerZone         [Layer: NOT in CollisionLayers]  (BoxCollider2D [trigger] + GreenPadZoneRelay [Inner])
```

- Both zones as **siblings** under root (not nested).
- Inner physically contained within Outer.
- Both on a layer excluded from all player detection masks.
- Both colliders set to `isTrigger = true`.

### Are current symptoms explained by setup problems alone?

**Partially.** There are two independent issues:

1. **Bounce height decay** — caused by `_endedJumpEarly` flag not being reset by
   `AddFrameForce`, leading to 3× extra gravity on the upward arc. This is a
   **controller-level bug**, fixed in the previous change (PlayerController.cs:L80).
   This is NOT a setup problem.

2. **Bounce producing zero/near-zero upward velocity** — caused by false grounding
   if zones are on a layer in `CollisionLayers` (most likely Default). This IS a
   **setup problem**. The false grounding zeros `vIn.y` before the bounce reads it.

Both issues can coexist. A player on a misconfigured green pad would experience:
- False grounding → velocity zeroed → weak/no bounce (setup problem)
- Even if grounding is avoided, the upward arc decays due to stale `_endedJumpEarly`
  (controller bug, now fixed)

### Additional code-level recommendation

`CalculateCollisions()` should explicitly set `Physics2D.queriesHitTriggers = false`
before the ground raycast, matching the pattern already used in `CheckPos()` and
`CalculateLadders()`. This would make ground detection immune to trigger colliders
regardless of layer assignment, eliminating the entire class of false-grounding bugs.

This is a one-line addition (plus restore in `finally`) at PlayerController.cs:L332–L335.

