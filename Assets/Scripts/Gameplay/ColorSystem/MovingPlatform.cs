using UnityEngine;
using TarodevController;

/// <summary>
/// Moving platform that integrates with the TarodevController physics system.
///
/// Implements IPhysicsMover so PlayerController can read per-tick
/// position deltas and exit velocity, and IPhysicsObject so
/// PhysicsSimulator drives it in the correct tick order
/// (platforms tick before players).
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

    [Tooltip("The platform group object from ColorManager that controls whether this platform is active. Usually this is the same parent object assigned in ColorManager.")]
    [SerializeField] private GameObject platformGroupRoot;

    [Tooltip("Leave null to auto-find at Awake.")]
    [SerializeField] private ColorManager colorManager;

    // ── IPhysicsMover implementation ─────────────────────────────────
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
    private bool _stopped;

    // ── Unity lifecycle ──────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _colliders = GetComponentsInChildren<Collider2D>(true);

        if (colorManager == null)
            colorManager = Object.FindFirstObjectByType<ColorManager>();

        // If nothing was assigned, default to this GameObject.
        // If your moving platform is a child inside a color group, assign the actual group parent in the inspector.
        if (platformGroupRoot == null)
            platformGroupRoot = gameObject;

        startPoint = _rb.position;
        _previousPosition = _rb.position;
        _currentTarget = endPoint != null ? (Vector2)endPoint.position : startPoint;
    }

    private void Start()
    {
        if (PhysicsSimulator.Instance != null)
            PhysicsSimulator.Instance.AddPlatform(this);
    }

    private void OnDestroy()
    {
        if (PhysicsSimulator.Instance != null)
            PhysicsSimulator.Instance.RemovePlatform(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (updateStartFromTransformInEditor)
            startPoint = transform.position;
    }
#endif

    // ── IPhysicsObject tick methods ──────────────────────────────────
    public void TickUpdate(float delta, float time) { }

    public void TickFixedUpdate(float delta)
    {
        bool colorActive = IsColorActive();

        if (gateCollisionByActiveColor)
            SetCollidersEnabled(colorActive);

        if (gateMovementByActiveColor && !colorActive)
        {
            _previousPosition = _rb.position;
            _framePositionDelta = Vector2.zero;
            _velocity = Vector2.zero;
            return;
        }

        if (_stopped || endPoint == null)
        {
            _previousPosition = _rb.position;
            _framePositionDelta = Vector2.zero;
            _velocity = Vector2.zero;
            return;
        }

        Vector2 target = _currentTarget;
        float distToTarget = Vector2.Distance(_rb.position, target);

        float speedFactor = 1f;
        if (decelBufferDistance > 0f && distToTarget < decelBufferDistance)
            speedFactor = Mathf.Clamp(distToTarget / decelBufferDistance, minSpeedFactor, 1f);

        float stepSize = speed * speedFactor * delta;
        Vector2 currentPos = _rb.position;
        Vector2 newPos = Vector2.MoveTowards(currentPos, target, stepSize);

        _rb.MovePosition(newPos);

        _framePositionDelta = newPos - _previousPosition;
        _velocity = delta > 0f ? _framePositionDelta / delta : Vector2.zero;
        _previousPosition = newPos;

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

    // ── Color query adapted to current ColorManager ──────────────────
    private bool IsColorActive()
    {
        if (colorManager == null)
            return true;

        if (platformGroupRoot == null)
            return true;

        return colorManager.IsGroupActive(platformGroupRoot);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = enabled;
        }
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