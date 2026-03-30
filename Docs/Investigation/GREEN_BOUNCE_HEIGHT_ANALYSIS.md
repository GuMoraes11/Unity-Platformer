# Green Bounce Height Decay — Root Cause Analysis

## Summary

Repeated bounces on GreenPlatform lose height because the controller applies **extra
constant gravity** (`ExtraConstantGravity`, default 40) via `ConstantForce2D` every
physics tick while airborne, and an additional **early-jump-release multiplier**
(`EndJumpEarlyExtraForceMultiplier`, default 3×) that triples that gravity when the
player is not holding jump. A pure velocity reflection (`vOut.y = -vIn.y`) does NOT
compensate for this asymmetric gravity — the player decelerates faster going up than
they accelerate coming down, so the downward speed at re-entry is less than the
original upward launch speed.

A secondary contributor is the **grounding snap** in `ToggleGrounded(true)`, which
zeros vertical velocity the instant the ground raycast succeeds. If the green zone
overlaps the ground detection region, the player can be grounded for one tick before
the bounce fires, losing all downward momentum.

---

## 1) Vertical Motion Pipeline

### Where gravity is applied

| Source | Location | Mechanism |
|--------|----------|-----------|
| Unity Rigidbody2D gravity | `_rb.gravityScale = GRAVITY_SCALE` (=1) | Set in `ToggleGrounded(false)` — **PlayerController.cs:L405**. Standard `Physics2D.gravity` (default -9.81) applied by engine every physics step. |
| Extra constant gravity | `_constantForce.force = extraForce * _rb.mass` | **PlayerController.cs:L932–L933**. Applied in the normal (non-force, non-dash, non-wall, non-ladder) branch of `Move()`. `extraForce.y = -Stats.ExtraConstantGravity` when airborne. |
| Early jump release multiplier | `_endedJumpEarly && Velocity.y > 0 ? Stats.EndJumpEarlyExtraForceMultiplier : 1` | **PlayerController.cs:L932**. When the player released jump while still moving upward, the extra gravity is multiplied by `EndJumpEarlyExtraForceMultiplier` (default 3). |

### Custom gravity multiplier / fall multiplier

There is **no separate fall multiplier** (no `fallMultiplier` field). The asymmetry
comes entirely from `EndJumpEarlyExtraForceMultiplier`:

- **Rising after jump release**: total downward force = `Physics2D.gravity.y` + `ExtraConstantGravity * EndJumpEarlyExtraForceMultiplier` = -9.81 + (-40 × 3) = **-129.81 units/s²**
- **Rising with jump held**: total = -9.81 + (-40 × 1) = **-49.81 units/s²**
- **Falling**: `_endedJumpEarly` is true (set at L636 when `Velocity.y < 0`), but the multiplier only applies when `Velocity.y > 0`, so falling gravity = -9.81 + (-40 × 1) = **-49.81 units/s²**

Evidence: **PlayerController.cs:L636** — `_endedJumpEarly` is set true when `Velocity.y < 0`.
Evidence: **PlayerController.cs:L932** — multiplier is gated on `Velocity.y > 0`.

### Every place vertical velocity is changed

| Location | What happens |
|----------|-------------|
| L75–L76 (`AddFrameForce`) | If `resetVelocity`, calls `SetVelocity(Vector2.zero)`. Then accumulates into `_forceToApplyThisFrame`. |
| L261 (`SetFrameData`) | `Velocity = _rb.linearVelocity` — reads current velocity from Rigidbody. |
| L270 (`RemoveTransientVelocity`) | Subtracts `_totalTransientVelocityAppliedLastFrame` from `_rb.linearVelocity`. |
| L388–L392 (`ToggleGrounded(true)`) | **`SetVelocity(_trimmedFrameVelocity)`** — `_trimmedFrameVelocity = (Velocity.x, 0)`. **Zeros vertical velocity.** |
| L405 (`ToggleGrounded(false)`) | Sets `_rb.gravityScale = GRAVITY_SCALE`. |
| L644 (`ExecuteJump`) | `SetVelocity(_trimmedFrameVelocity)` — zeros Y before applying jump force. |
| L877–L884 (`Move` force branch) | `_rb.AddForce(_forceToApplyThisFrame * _rb.mass, ForceMode2D.Impulse)` — applies accumulated frame force. |
| L932–L933 (`Move` normal branch) | Sets `_constantForce.force` for extra gravity. |
| L1000 (`Move` airborne branch) | `newVelocity = new Vector2(targetX, _rb.linearVelocity.y)` — preserves current Y from Rigidbody. |
| L1003 (`Move` final) | `SetVelocity((newVelocity + AdditionalFrameVelocities()) * _currentFrameSpeedModifier)` — multiplies by speed modifier. |

### Grounded state vertical behavior

**PlayerController.cs:L388–L407** (`ToggleGrounded`):

When grounded becomes **true** (L388–L399):
- `_rb.gravityScale = 0` — disables Rigidbody gravity
- `SetVelocity(_trimmedFrameVelocity)` — **zeros vertical velocity** (`_trimmedFrameVelocity.y` is always 0, see L262)
- `_constantForce.force = Vector2.zero` — disables extra gravity

When grounded becomes **false** (L401–L407):
- `_rb.gravityScale = GRAVITY_SCALE` — re-enables Rigidbody gravity

---

## 2) AddFrameForce Semantics

### Implementation

```
// PlayerController.cs:L73–L77
public void AddFrameForce(Vector2 force, bool resetVelocity = false)
{
    if (resetVelocity) SetVelocity(Vector2.zero);
    _forceToApplyThisFrame += force;
}
```

- **Stores force**, not velocity. Accumulated into `_forceToApplyThisFrame` (Vector2).
- `resetVelocity=true` calls `SetVelocity(Vector2.zero)` which sets `_rb.linearVelocity = Vector2.zero`.

### Where it is consumed

**PlayerController.cs:L877–L884** (`Move()`, first branch):

```
if (_forceToApplyThisFrame != Vector2.zero)
{
    _rb.linearVelocity += AdditionalFrameVelocities();
    _rb.AddForce(_forceToApplyThisFrame * _rb.mass, ForceMode2D.Impulse);
    return;   // <-- early return, skips normal movement AND gravity setup
}
```

- Uses `Rigidbody2D.AddForce(..., ForceMode2D.Impulse)`.
- `ForceMode2D.Impulse` applies `force / mass * mass = force` as an instantaneous velocity change.
- **Early return** means the normal gravity branch (L932–L933) is **NOT executed** this tick.
- However, `_rb.gravityScale` is still 1 (set when leaving ground), so Unity's built-in gravity IS applied by the physics engine in the same step.

### Gravity timing relative to force

In the tick where `AddFrameForce` fires:
1. `TickFixedUpdate` runs → `Move()` applies impulse via `AddForce(Impulse)` and returns early.
2. `_constantForce.force` retains its **previous frame's value** (not zeroed, not updated).
3. After `TickFixedUpdate` returns, Unity's physics step integrates:
   - `_rb.gravityScale * Physics2D.gravity` (built-in)
   - `_constantForce.force / mass` (ConstantForce2D, which still has the previous frame's extra gravity)

**This means one tick of extra gravity is applied in the same step as the bounce impulse.**

### Cleared

**PlayerController.cs:L293** (`CleanFrameData`): `_forceToApplyThisFrame = Vector2.zero`.

---

## 3) Grounding Interference

### Ground detection mechanism

**PlayerController.cs:L323–L363** (`CalculateCollisions`):
- Uses `Physics2D.Raycast(point, -Up, GrounderLength, Stats.CollisionLayers)`.
- `Stats.CollisionLayers` is a `LayerMask` — **only layers in this mask are hit**.
- `Physics2D.queriesStartInColliders` is set to `false` during the cast.
- The default `Physics2D.queriesHitTriggers` is **not explicitly set** in `CalculateCollisions`. It uses whatever the global default is (Unity default: `true` in older versions, `false` in newer).

### Are trigger colliders included in ground checks?

- `Physics2D.queriesHitTriggers` is toggled in `CalculateLadders` (L563–L571) but **restored** via try/finally.
- In `CalculateCollisions`, it is **not toggled**. The global default applies.
- **If the global default is `true`** (or if another script sets it to `true` and doesn't restore it), trigger colliders on the `CollisionLayers` mask **would be detected as ground**.
- **If the green zone triggers are on a layer included in `Stats.CollisionLayers`**, they could cause false grounding.

### What happens if green zones are on Ground layer

If the green zone trigger colliders are on a layer included in `Stats.CollisionLayers`:
1. `CalculateCollisions` raycast hits the trigger collider.
2. `ToggleGrounded(true)` fires → **zeros vertical velocity** (L392), sets `gravityScale=0`.
3. Next tick: player is "grounded" with zero Y velocity.
4. GreenPlatform's `FixedUpdate` reads `pc.Velocity` → sees `(vx, 0)` → reflects to `(vx, 0)` → **no bounce**.

**This is a critical failure mode.** The green zones MUST be on a layer NOT in `Stats.CollisionLayers`, or `Physics2D.queriesHitTriggers` must be globally `false`.

### Current GreenPlatform validation

**GreenPlatform.cs:L306–L335** (`ValidateZone`): warns if zone is on layer "Ground". This is necessary but may not be sufficient — `Stats.CollisionLayers` could include layers other than "Ground".

---

## 4) Momentum Loss Sources

### Source 1: Grounding snap (CRITICAL)

**PlayerController.cs:L392**: `SetVelocity(_trimmedFrameVelocity)` — zeros Y on grounding.

If the player touches ground (or a trigger detected as ground) for even one tick before the bounce fires, all downward momentum is lost. The bounce then reflects `vIn.y ≈ 0` → `vOut.y ≈ 0`.

### Source 2: Extra constant gravity (PRIMARY for height decay)

**PlayerController.cs:L932–L933**: `-Stats.ExtraConstantGravity` (default 40) applied via `ConstantForce2D` every airborne tick in the normal Move branch.

This is **not applied** in the tick where `_forceToApplyThisFrame != 0` (early return at L884). But `ConstantForce2D.force` retains its previous value, so Unity's physics engine still applies it.

### Source 3: Early jump release multiplier (SIGNIFICANT)

**PlayerController.cs:L932**: When `_endedJumpEarly && Velocity.y > 0`, extra gravity is multiplied by `EndJumpEarlyExtraForceMultiplier` (default 3).

After a bounce, the player is rising with `Velocity.y > 0`. If `_endedJumpEarly` is true (which it is — set at L636 when `Velocity.y < 0` during the fall before the bounce), the rising phase gets **3× extra gravity**. This is the primary asymmetry:

- **Falling into bounce**: gravity = -9.81 + (-40) = -49.81
- **Rising after bounce**: gravity = -9.81 + (-40 × 3) = -129.81 (if `_endedJumpEarly` is true)

The player decelerates ~2.6× faster going up than they accelerated coming down. A perfect velocity reflection loses ~60% of its height on the first bounce.

### Source 4: `_endedJumpEarly` flag not reset by bounce

**PlayerController.cs:L636**: `_endedJumpEarly` is set true when `Velocity.y < 0`. It is only reset to false in `ExecuteJump` (L645). `AddFrameForce` does NOT reset it.

This means after a bounce, `_endedJumpEarly` remains true → the 3× multiplier applies to the entire upward arc.

### Source 5: ConstantForce2D residual in bounce tick

As noted in §2, the `ConstantForce2D` retains its previous frame's force value during the bounce tick. One tick of extra gravity is applied simultaneously with the impulse.

### Source 6: Speed modifier multiplier

**PlayerController.cs:L1003**: `* _currentFrameSpeedModifier`. In the normal branch, velocity is multiplied by this. In the force branch (L877–L884), this multiplier is NOT applied. So this is **not** a factor for bounces.

### Source 7: Transient velocity removal

**PlayerController.cs:L265–L287** (`RemoveTransientVelocity`): Subtracts `_totalTransientVelocityAppliedLastFrame`. If the bounce tick also had transient velocity (e.g., from a moving platform), this could subtract from the bounce velocity next tick. **Unlikely to be relevant for static green platforms.**

### NOT a factor: Max fall speed / velocity clamping

There is **no MaxFallSpeed field** in PlayerStats. There is no explicit velocity clamping anywhere in PlayerController. Velocity is unbounded.

---

## 5) Recommendation

### Is gravity compensation mathematically needed?

**Yes, but only because of controller-side interference.** In a standard physics engine with symmetric gravity, a perfect velocity reflection (`vOut = -vIn`) preserves height indefinitely. The decay is caused by:

1. **`_endedJumpEarly` flag** remaining true after bounce → 3× extra gravity on the upward arc (PRIMARY CAUSE, ~60% height loss per bounce)
2. **`ConstantForce2D` residual** applying one tick of extra gravity during the bounce tick (MINOR)
3. **Potential grounding snap** if zones overlap ground detection (CRITICAL if misconfigured)

### The actual problem is controller-side interference

The `_endedJumpEarly` flag is designed for jump-cut feel (release jump button → fall faster). It was never designed to interact with external launch forces. After a bounce, the flag is stale from the previous fall, causing the entire upward arc to use 3× gravity.

### Cleanest fix category

**D) Use a dedicated launch hook that bypasses movement code for 1 frame** — but a lighter variant:

The cleanest fix is to **reset `_endedJumpEarly = false`** when an external force is applied upward. This can be done inside `AddFrameForce` itself:

```
if (force.y > 0) _endedJumpEarly = false;
```

This single line eliminates the primary cause (3× gravity on upward arc) without adding gravity compensation math, without bypassing the movement system, and without modifying the bounce platform code.

**Category ranking:**

| Category | Verdict |
|----------|---------|
| **D (light)**: Reset `_endedJumpEarly` in `AddFrameForce` when force.y > 0 | **Best.** Fixes root cause. 1 line. No side effects on normal jumps (flag is already false during a held jump). |
| **C**: Prevent grounding interference | **Also needed** as a safety measure (ensure zones are not on `CollisionLayers`). Already partially addressed by `OnValidate` warning. |
| **A**: Use `abs(downward speed)` as upward bounce | Masks the symptom. Doesn't fix the 3× gravity on the upward arc — height still decays, just starts higher. |
| **B**: Compensate for custom gravity multiplier | Over-engineered. Requires knowing the exact multiplier at bounce time, which depends on `_endedJumpEarly` state. Fragile. |

### Combined recommendation

1. **Fix D (light)**: Reset `_endedJumpEarly = false` in `AddFrameForce` when `force.y > 0`.
2. **Fix C**: Ensure green zone layers are excluded from `Stats.CollisionLayers`. Strengthen `OnValidate` to check against the actual `CollisionLayers` mask, not just the "Ground" layer name.

