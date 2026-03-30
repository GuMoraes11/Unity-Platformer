using UnityEngine;
using TarodevController;

/// <summary>
/// Moving platform that integrates with the TarodevController physics system.
///
/// Implements <see cref="IPhysicsMover"/> so PlayerController can read per-tick
/// position deltas and exit velocity, and <see cref="IPhysicsObject"/> so
/// <see cref="PhysicsSimulator"/> drives it in the correct tick order
/// (platforms tick before players).
///
/// Movement: ping-pongs (or one-shots) between the GameObject's initial position
/// and <see cref="endPoint"/>. A deceleration buffer smoothly slows the platform
/// near each endpoint.
///
/// Color gating (optional): movement and/or collision can be gated by the active
/// color in <see cref="ColorManager"/>, using the same query pattern as Orange.cs.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour, IPhysicsMover, IPhysicsObject
{
    // ── Waypoints ────────────────────────────────────────────────────
    [Header("Waypoints")]
    [Tooltip("Drag an empty Transform here to mark the other end of the path.")]
    [SerializeField] private Transform endPoint;

    [Tooltip("Automatically set startPoint to this transform's position in the editor.")]
    [SerializeField] private bool updateStartFromTransformInEditor = true;

    [SerializeField, HideInInspector] private Vector2 startPoint;

    // ── Speed ────────────────────────────────────────────────────────
    [Header("Speed")]
    [SerializeField, Min(0.01f)] private float speed = 3f;
    [SerializeField] private bool pingPong = true;

    // ── Deceleration buffer ──────────────────────────────────────────
    [Header("Deceleration Buffer")]
    [Tooltip("Distance from the target at which the platform begins to slow down.")]
    [SerializeField, Min(0f)] private float decelBufferDistance = 1f;

    [Tooltip("Minimum speed factor when inside the buffer zone (0 = full stop at endpoint).")]
    [SerializeField, Range(0f, 1f)] private float minSpeedFactor = 0.1f;

    // ── Color gating ─────────────────────────────────────────────────
    [Header("Color Gating (optional)")]
    [SerializeField] private bool gateCollisionByActiveColor;
    [SerializeField] private bool gateMovementByActiveColor;

    [Tooltip("Must match a colorName entry in ColorManager.colorPlatforms.")]
    [SerializeField] private string colorName;

    [Tooltip("Leave null to auto-find at Awake.")]
    [SerializeField] private ColorManager colorManager;

    // ── IPhysicsMover implementation ─────────────────────────────────
    // UsesBounding = false  → PlayerController removes this mover when
    //   ground trace loses contact (simple contact-only platform).
    // RequireGrounding = true → mover is only added via ground trace,
    //   not via OnTriggerEnter2D (this is a solid surface, not a trigger zone).
    public bool UsesBounding => false;
    public bool RequireGrounding => true;
    public Vector2 FramePositionDelta => _framePositionDelta;
    public Vector2 FramePosition => _rb.position;
    public Vector2 Velocity => _velocity;
    public Vector2 TakeOffVelocity => _velocity;

    // ── Private state ────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Collider2D[] _colliders;
    private Vector2 _previousPosition;
    private Vector2 _framePositionDelta;
    private Vector2 _velocity;

    private Vector2 _currentTarget;
    private bool _movingToEnd = true;
    private bool _stopped; // true when non-pingPong platform has reached the end

    // ── Unity lifecycle ──────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _colliders = GetComponentsInChildren<Collider2D>();

        if (colorManager == null)
            colorManager = Object.FindFirstObjectByType<ColorManager>();

        // Snapshot start position at runtime (editor value may be stale if
        // the GO was moved after the last OnValidate).
        startPoint = _rb.position;
        _previousPosition = _rb.position;
        _currentTarget = endPoint != null ? (Vector2)endPoint.position : startPoint;
    }

    private void OnDestroy()
    {
        PhysicsSimulator.Instance.RemovePlatform(this);
    }

    private void Start()
    {
        // Register after Awake so PhysicsSimulator singleton is guaranteed to exist
        // (SimulatorBootstrapper runs at BeforeSceneLoad).
        PhysicsSimulator.Instance.AddPlatform(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (updateStartFromTransformInEditor)
            startPoint = transform.position;
    }
#endif

    // ── IPhysicsObject tick methods ──────────────────────────────────
    // TickUpdate is called from PhysicsSimulator.Update() — nothing to
    // do here for a simple waypoint platform.
    public void TickUpdate(float delta, float time) { }

    /// <summary>
    /// Called from <see cref="PhysicsSimulator.FixedUpdate"/> BEFORE any
    /// player ticks, so <see cref="FramePositionDelta"/> is fresh when
    /// PlayerController reads it in TraceGround().
    /// </summary>
    public void TickFixedUpdate(float delta)
    {
        // ── Color gating: collision ──────────────────────────────────
        if (gateCollisionByActiveColor)
        {
            bool active = IsColorActive();
            SetCollidersEnabled(active);
        }

        // ── Color gating: movement ──────────────────────────────────
        if (gateMovementByActiveColor && !IsColorActive())
        {
            // Platform frozen — still update bookkeeping so delta is zero.
            _previousPosition = _rb.position;
            _framePositionDelta = Vector2.zero;
            _velocity = Vector2.zero;
            return;
        }

        if (_stopped || endPoint == null)
        {
            _framePositionDelta = Vector2.zero;
            _velocity = Vector2.zero;
            return;
        }

        // ── Compute movement ─────────────────────────────────────────
        Vector2 target = _currentTarget;
        float distToTarget = Vector2.Distance(_rb.position, target);

        // Deceleration buffer: scale speed when close to target.
        float speedFactor = 1f;
        if (decelBufferDistance > 0f && distToTarget < decelBufferDistance)
            speedFactor = Mathf.Clamp(distToTarget / decelBufferDistance, minSpeedFactor, 1f);

        float stepSize = speed * speedFactor * delta;
        Vector2 newPos = Vector2.MoveTowards(_rb.position, target, stepSize);
        _rb.MovePosition(newPos);

        // ── Bookkeeping ──────────────────────────────────────────────
        _framePositionDelta = newPos - _previousPosition;
        _velocity = delta > 0f ? _framePositionDelta / delta : Vector2.zero;
        _previousPosition = newPos;

        // ── Endpoint arrival ─────────────────────────────────────────
        if (Vector2.Distance(newPos, target) < 0.001f)
        {
            if (pingPong)
            {
                _movingToEnd = !_movingToEnd;
                _currentTarget = _movingToEnd ? (Vector2)endPoint.position : startPoint;
            }
            else
            {
                _stopped = true;
            }
        }
    }

    // ── Color query (matches Orange.cs pattern) ──────────────────────
    private bool IsColorActive()
    {
        if (colorManager == null || string.IsNullOrEmpty(colorName))
            return true; // No gating configured → always active.

        foreach (var cp in colorManager.colorPlatforms)
        {
            if (cp.colorName.Equals(colorName, System.StringComparison.OrdinalIgnoreCase))
                return colorManager.IsPlatformGroupCurrentlyActive(cp.platformGroup);
        }

        // Color name not found in manager → treat as inactive to fail safe.
        return false;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < _colliders.Length; i++)
            _colliders[i].enabled = enabled;
    }

    // ── Gizmos ───────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (endPoint == null) return;

        Vector2 a = Application.isPlaying ? startPoint : (Vector2)transform.position;
        Vector2 b = endPoint.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(b, 0.15f);
    }
#endif
}

// ── Test checklist (manual, in-editor) ───────────────────────────────
//
// 1. Stand on platform (horizontal move):
//    Player rides with no jitter; platform transient velocity keeps
//    the player attached. Verify via FramePositionDelta != 0 in debugger.
//
// 2. Jump while platform is moving:
//    Player inherits platform velocity on take-off (TakeOffVelocity →
//    _decayingTransientVelocity in PlayerController). Player should
//    continue drifting in the platform's direction after jumping.
//
// 3. Platform disabled by color:
//    - gateCollisionByActiveColor = true: colliders disable when color
//      is inactive; player falls through.
//    - gateMovementByActiveColor = true: platform freezes in place when
//      color is inactive; resumes when color becomes active.
//
// 4. Decel buffer near endpoints:
//    Set decelBufferDistance > 0 and observe the platform slowing
//    smoothly as it approaches each waypoint. minSpeedFactor = 0 should
//    bring it to a near-stop right at the endpoint.
//
// 5. Ping-pong vs one-shot:
//    pingPong = true: platform oscillates. pingPong = false: platform
//    moves to endPoint once and stops.
//
// 6. Multiple platforms in scene:
//    Place 2+ MovingPlatforms. Verify no cross-contamination of
//    _activatedMovers in PlayerController (each is an independent
//    IPhysicsMover instance).

