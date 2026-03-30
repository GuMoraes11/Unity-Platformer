# Player Physics & Moving Platform Investigation

> Generated: 2026-03-10 — read-only code investigation.

## 1) Player Simulation Model

### Velocity application

The player uses a **Dynamic `Rigidbody2D`** (`RigidbodyType2D.Dynamic`) with direct `linearVelocity` writes.
Gravity is toggled via `_rb.gravityScale` (0 when grounded, 1 when airborne) and supplemented by a
`ConstantForce2D` component for extra gravity (`Stats.ExtraConstantGravity`).

All velocity changes go through `SetVelocity()`:

```csharp
private void SetVelocity(Vector2 newVel)
{
    _rb.linearVelocity = newVel;
    Velocity = newVel;
}
```

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L1017-L1021

Jump forces are applied via `_rb.AddForce(... ForceMode2D.Impulse)` in `Move()` when `_forceToApplyThisFrame != Vector2.zero`.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L877-L884

### Where simulation runs

The controller does **not** use Unity's `Update()`/`FixedUpdate()` directly. Instead, a singleton
`PhysicsSimulator` drives all registered `IPhysicsObject` instances:

- `PhysicsSimulator.Update()` → calls `TickUpdate(delta, time)` on platforms then players (input gathering).
- `PhysicsSimulator.FixedUpdate()` → calls `TickFixedUpdate(delta)` on platforms then players (physics sim).

**Tick order is: platforms first, then players** — this is critical for platform delta to be available
when the player reads it.

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs`:L22-L49

The player's `TickFixedUpdate` runs a 14-step pipeline (see comment block at L134-L151):
`RemoveTransientVelocity → SetFrameData → Collisions → Direction → Walls → Ladders → Jump → Dash → ExternalModifiers → TraceGround → Move → Crouch → CleanFrameData → SaveState`

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L134-L180

## 2) Timing Model and "Momentum" Implications

### Input sampling

Input is gathered in `TickUpdate()` (called from `Update`) via `_playerInput.Gather()`.
The `FrameInput` struct captures `Move`, `JumpDown`, `JumpHeld`, `DashDown` using Unity's Input System
(`WasPressedThisFrame()`, `IsPressed()`, `ReadValue<Vector2>()`).

Evidence: `Assets/Controller/Scripts/PlayerInput.cs`:L27-L36
Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L126-L132, L226-L241

### Where motion is applied

All motion is applied in `TickFixedUpdate()` (called from `FixedUpdate`). The final velocity write
happens in `Move()` at the end of the pipeline.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L875-L1015

### Delta time source

`_time` is accumulated from `Time.deltaTime` in `PhysicsSimulator.Update()` — this is **Update-rate time**,
not `FixedUpdate` time. It is used for timing windows (coyote, jump buffer, dash cooldown) during the
Fixed tick. This is intentional per the class doc comment.

`_delta` is set to `Time.deltaTime` from `FixedUpdate` for the physics step (acceleration, friction, decay).

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs`:L23-L25 (Update time accumulation)
Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L109, L126-L129, L152-L154

**Implication**: Timing windows are frame-rate-sensitive (Update time), while physics integration is
fixed-step. This is a common pattern for responsive input feel.

## 3) Collision/Grounding and Platform Attachment

### Ground detection

Ground is detected via **raycasts** in `CalculateCollisions()`. A centre ray fires downward from
`RayPoint` (player position + step height). If it misses, up to 5 offset rays zigzag outward.

A hit is accepted only if `Vector2.Angle(hit.normal, Up) <= Stats.MaxWalkableSlope`.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L323-L363

### Platform identity capture

**Yes** — `_groundHit` (`RaycastHit2D`) is stored, which contains the collider and transform of the
ground surface. In `TraceGround()`, the hit transform is queried for `IPhysicsMover`:

```csharp
if (_groundHit.transform.TryGetComponent(out currentPlatform))
{
    _activatedMovers.Add(currentPlatform);
}
```

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L828-L831

### Active platform concept

The controller maintains:
- `_lastPlatform` (`IPhysicsMover`) — the platform from the previous frame's ground trace.
- `_activatedMovers` (`HashSet<IPhysicsMover>`, max 5) — all currently active movers (ground + trigger).

When `_lastPlatform` changes and the old platform has `UsesBounding == false`, it is removed from
`_activatedMovers` and its exit velocity is applied as `_decayingTransientVelocity`.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L808-L809, L834-L845, L1047-L1048

## 4) Existing Platform/Mover Integration

### The contract: `IPhysicsMover`

```csharp
public interface IPhysicsMover
{
    bool UsesBounding { get; }
    bool RequireGrounding { get; }
    Vector2 FramePositionDelta { get; }
    Vector2 FramePosition { get; }
    Vector2 Velocity { get; }
    Vector2 TakeOffVelocity { get; }
}
```

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs`:L58-L69

| Property | Purpose |
|---|---|
| `UsesBounding` | If `true`, mover uses a trigger volume; removal happens via `OnTriggerExit2D`. If `false`, removal happens when ground trace loses contact. |
| `RequireGrounding` | If `true`, the mover is only added via ground trace (not trigger enter). If `false`, trigger enter alone adds it. |
| `FramePositionDelta` | Per-tick position change of the platform. Divided by `_delta` to produce transient velocity. |
| `FramePosition` | Current world position; used to skip movers the player is below (`_framePosition.y < platform.FramePosition.y - SKIN_WIDTH`). |
| `Velocity` | Not directly consumed by PlayerController (available for external use). |
| `TakeOffVelocity` | Velocity inherited when leaving the platform; feeds `_decayingTransientVelocity`. |

### How the player receives platform movement

**Transient velocity injection** — not parenting, not joints, not direct displacement:

```csharp
foreach (var platform in _activatedMovers)
{
    if (_framePosition.y < platform.FramePosition.y - SKIN_WIDTH) continue;
    _frameTransientVelocity += platform.FramePositionDelta / _delta;
}
```

This transient velocity is added in `AdditionalFrameVelocities()` inside `Move()`, then stripped at the
start of the next tick by `RemoveTransientVelocity()`.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L847-L854, L265-L287, L1005-L1014

### Mover entry/exit paths

1. **Ground contact** (`TraceGround`): `_groundHit.transform.TryGetComponent<IPhysicsMover>` → add to `_activatedMovers`.
2. **Trigger overlap** (`OnTriggerEnter2D`): if `!mover.RequireGrounding` → add to `_activatedMovers`.
3. **Exit via trigger** (`OnTriggerExit2D`): always removes from `_activatedMovers`.
4. **Exit via ground loss** (`TraceGround`): if `_lastPlatform` changes and `UsesBounding == false` → remove + apply exit velocity.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L1052-L1062, L828-L845

### Exit velocity (launch momentum)

```csharp
private void ApplyMoverExitVelocity(IPhysicsMover mover)
{
    var platformVel = mover.TakeOffVelocity;
    if (platformVel.y < 0) platformVel.y *= Stats.NegativeYVelocityNegation;
    _decayingTransientVelocity += platformVel;
}
```

`_decayingTransientVelocity` decays each frame via `ExternalVelocityDecayRate` in `RemoveTransientVelocity()`.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L857-L862, L278-L284

## 5) Recommended Implementation Options (ranked)

### Option A: Implement platform as `IPhysicsMover` (RECOMMENDED)

The contract already exists and is fully wired into `PlayerController`. A new `MovingPlatform` script
needs only to implement `IPhysicsMover` and optionally `IPhysicsObject` (to register with `PhysicsSimulator`
for deterministic tick ordering).

**Required scripts to create/touch:**
- New: `MovingPlatform.cs` (implements `IPhysicsMover`, optionally `IPhysicsObject`)
- No changes to `PlayerController.cs` or `PhysicsSimulator.cs`

**Implementation sketch:**
- Use a **Kinematic `Rigidbody2D`** on the platform.
- In `TickFixedUpdate`, compute waypoint movement via `Rigidbody2D.MovePosition()`.
- Track `_previousPosition` to compute `FramePositionDelta` and `Velocity`.
- Set `TakeOffVelocity` = current velocity (or a scaled version for launch pads).
- Set `RequireGrounding = true` for standard platforms, `false` for wind zones / conveyor triggers.
- Set `UsesBounding = false` for simple contact platforms, `true` for trigger-volume movers.

**Risk level:** LOW — uses the existing, tested integration path.

**Minimal test checklist:**
1. Player rides platform horizontally without sliding off.
2. Player rides platform vertically (up) without bouncing/jitter.
3. Player jumps off moving platform and inherits momentum (decaying).
4. Player walks off platform edge and inherits momentum.
5. Platform moving downward does not cause ground-detection flicker.
6. Multiple platforms in scene do not interfere.

### Option B: Kinematic Rigidbody2D with velocity feed (no IPhysicsMover)

Use a kinematic `Rigidbody2D` platform and feed its velocity into the player via `AddFrameForce()` or
by directly writing to `_decayingTransientVelocity` through a custom coupling script.

**Required scripts to touch:**
- New: `MovingPlatform.cs` (standalone, no interface)
- New or modified: coupling script on player or platform that detects contact and injects velocity

**Risk level:** MEDIUM — bypasses the existing mover system; must manually handle entry/exit/decay.

**Pros:** Simpler if you only need one platform type.
**Cons:** Duplicates logic already in PlayerController; exit velocity and decay must be reimplemented.

### Option C: Physics joint approach

Attach a `FixedJoint2D` or `RelativeJoint2D` between player and platform on contact.

**Required scripts to touch:**
- New: `MovingPlatform.cs` with joint management
- Possibly `PlayerController.cs` to handle joint-induced velocity conflicts

**Risk level:** HIGH — the controller writes `linearVelocity` directly every tick, which will fight
the joint constraint. Gravity toggling (`gravityScale = 0` when grounded) adds further conflict.
The two-collider switching system may break joint anchors.

**Pros:** Zero custom velocity math.
**Cons:** Fundamentally incompatible with this controller's velocity-override architecture.

## 6) "Do Not Do" List (avoid jank)

### 1. Do NOT use `Transform.position` to move the platform

The player reads `FramePositionDelta` which is derived from physics-step positions. Moving via
`transform.position` skips physics interpolation and can cause the player to phase through or jitter.
Use `Rigidbody2D.MovePosition()` on a Kinematic body instead.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L853 (delta divided by `_delta` assumes physics-consistent positions)

### 2. Do NOT parent the player to the platform

The controller writes `_rb.linearVelocity` and `_rb.position` directly. Parenting a Dynamic Rigidbody
under a moving transform causes Unity's physics engine to fight the parent transform, producing
teleportation and velocity spikes. The existing transient-velocity system replaces parenting.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L1017-L1021 (direct velocity writes)

### 3. Do NOT change PhysicsSimulator tick order (platforms must tick before players)

`PhysicsSimulator.FixedUpdate()` ticks platforms first, then players. If reversed, the player would
read stale `FramePositionDelta` (one frame behind), causing the player to lag behind the platform.

Evidence: `Assets/Controller/Scripts/PhysicsSimulator.cs`:L37-L48

### 4. Do NOT toggle global `Physics2D.queriesStartInColliders` without restoring it

`CalculateCollisions()` and `CalculateLadders()` already toggle global Physics2D query flags with
try/finally guards. A platform script that also toggles these flags without restoration will corrupt
the player's ground detection.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L325-L349, L563-L572

### 5. Do NOT apply platform velocity in `Update` — use `TickFixedUpdate` only

The transient velocity system adds velocity in `TickFixedUpdate` and strips it at the start of the
next `TickFixedUpdate`. Applying platform velocity in `Update` would be stripped incorrectly and
cause frame-rate-dependent drift.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L152-L158, L265-L274

### 6. Do NOT set `RequireGrounding = false` on solid-surface platforms

If `RequireGrounding` is `false`, the mover is added via `OnTriggerEnter2D` without ground contact.
For a solid platform (BoxCollider2D, not trigger), this path is never reached. The platform would
never register. Use `RequireGrounding = true` for any platform the player stands on.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L1055

### 7. Do NOT forget to implement `FramePosition` correctly

`TraceGround()` skips movers where `_framePosition.y < platform.FramePosition.y - SKIN_WIDTH`.
If `FramePosition` returns an incorrect value (e.g., always zero), the player will never receive
platform velocity when standing on top of it.

Evidence: `Assets/Controller/Scripts/PlayerController.cs`:L851

