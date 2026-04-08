using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TarodevController;

[DisallowMultipleComponent]
public class AdminFlyNoClip : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Key toggleKey = Key.Backquote;

    [Header("Movement")]
    [SerializeField] private float flySpeed = 14f;
    [SerializeField] private float fastMultiplier = 2.25f;
    [SerializeField] private float slowMultiplier = 0.4f;

    [Header("Vertical Controls")]
    [SerializeField] private bool useSeparateRiseDescendKeys = false;
    [SerializeField] private Key riseKey = Key.E;
    [SerializeField] private Key descendKey = Key.Q;

    [Header("Optional")]
    [SerializeField] private bool freezeRopeTetherWhileFlying = true;
    [SerializeField] private bool zeroVelocityOnExit = true;

    public bool IsAdminFlying => _isFlying;

    private static readonly HashSet<AdminFlyNoClip> ActiveFlyers = new HashSet<AdminFlyNoClip>();

    private Rigidbody2D _rb;
    private Collider2D[] _colliders;
    private Behaviour _playerControllerBehaviour;
    private TarodevController.PlayerInput _playerInput;
    private PlayerHealth _playerHealth;

    private bool _isFlying;
    private float _storedGravityScale;
    private RigidbodyType2D _storedBodyType;
    private CollisionDetectionMode2D _storedCollisionMode;
    private RigidbodyInterpolation2D _storedInterpolation;

    private Vector2 _flyMoveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _playerControllerBehaviour = GetComponent("PlayerController") as Behaviour;
        _playerInput = GetComponent<TarodevController.PlayerInput>();
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnDisable()
    {
        if (_isFlying)
            ExitFlyMode();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleFlyMode();
        }
#endif

        if (!_isFlying)
            return;

        _flyMoveInput = ReadMovementInput();
    }

    private void LateUpdate()
    {
        if (!_isFlying)
            return;

        HandleFlyMovement();
    }

    private void ToggleFlyMode()
    {
        if (_isFlying) ExitFlyMode();
        else EnterFlyMode();
    }

    private void EnterFlyMode()
    {
        if (_isFlying) return;

        _isFlying = true;
        ActiveFlyers.Add(this);

        if (_playerHealth != null)
            _playerHealth.SetExternalInvulnerable(true);

        if (_playerControllerBehaviour != null)
            _playerControllerBehaviour.enabled = false;

        if (_rb != null)
        {
            _storedGravityScale = _rb.gravityScale;
            _storedBodyType = _rb.bodyType;
            _storedCollisionMode = _rb.collisionDetectionMode;
            _storedInterpolation = _rb.interpolation;

            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.gravityScale = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.simulated = true;
        }

        SetCollidersEnabled(false);

        if (freezeRopeTetherWhileFlying)
            UpdateGlobalTetherState();
    }

    private void ExitFlyMode()
    {
        if (!_isFlying) return;

        _isFlying = false;
        ActiveFlyers.Remove(this);

        if (_playerHealth != null)
            _playerHealth.SetExternalInvulnerable(false);

        SetCollidersEnabled(true);

        if (_rb != null)
        {
            _rb.bodyType = _storedBodyType;
            _rb.gravityScale = _storedGravityScale;
            _rb.collisionDetectionMode = _storedCollisionMode;
            _rb.interpolation = _storedInterpolation;

            if (zeroVelocityOnExit)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        if (_playerControllerBehaviour != null)
            _playerControllerBehaviour.enabled = true;

        if (freezeRopeTetherWhileFlying)
            UpdateGlobalTetherState();
    }

    private void HandleFlyMovement()
    {
        Vector2 move = _flyMoveInput;

        float speedMultiplier = 1f;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                speedMultiplier *= fastMultiplier;

            if (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed)
                speedMultiplier *= slowMultiplier;
        }
#endif

        float finalSpeed = flySpeed * speedMultiplier;
        Vector3 delta = new Vector3(move.x, move.y, 0f) * (finalSpeed * Time.deltaTime);

        transform.position += delta;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private Vector2 ReadMovementInput()
    {
        Vector2 move = Vector2.zero;

        if (_playerInput != null)
        {
            FrameInput frameInput = _playerInput.Gather();
            move = frameInput.Move;
        }

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null)
            return move.normalized;

        if (useSeparateRiseDescendKeys)
        {
            float y = 0f;
            if (kb[riseKey].isPressed) y += 1f;
            if (kb[descendKey].isPressed) y -= 1f;

            if (Mathf.Abs(y) > 0.01f)
                move.y = y;
        }
#endif

        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        if (_colliders == null) return;

        foreach (var col in _colliders)
        {
            if (col == null) continue;
            col.enabled = enabledState;
        }
    }

    private static void UpdateGlobalTetherState()
    {
        if (CouchCoopSpawner.Instance == null)
            return;

        if (CouchCoopSpawner.Instance.TetherInstance == null)
            return;

        bool anyFlying = false;
        foreach (var flyer in ActiveFlyers)
        {
            if (flyer != null && flyer._isFlying)
            {
                anyFlying = true;
                break;
            }
        }

        CouchCoopSpawner.Instance.TetherInstance.SetFrozen(anyFlying);
    }
}