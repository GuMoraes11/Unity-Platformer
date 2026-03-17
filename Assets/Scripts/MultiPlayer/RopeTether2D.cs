using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeTether2D : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerA;
    public Transform playerB;

    [Header("Constraint")]
    public float maxDistance = 6.5f;

    [Tooltip("How hard the rope pulls when overstretched.")]
    public float pullStrength = 45f;

    [Tooltip("Velocity damping applied along the rope axis when overstretched.")]
    public float damping = 6f;

    [Header("Visual")]
    public bool drawRope = true;
    public Vector3 ropeOffsetA = Vector3.zero;
    public Vector3 ropeOffsetB = Vector3.zero;

    private Rigidbody2D _rbA;
    private Rigidbody2D _rbB;
    private LineRenderer _lr;
    private bool _isFrozen = false;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = 2;
    }

    private void Start()
    {
        CacheBodies();
    }

    private void CacheBodies()
    {
        _rbA = playerA != null ? playerA.GetComponent<Rigidbody2D>() : null;
        _rbB = playerB != null ? playerB.GetComponent<Rigidbody2D>() : null;
    }

    public void SetFrozen(bool frozen)
    {
        _isFrozen = frozen;

        if (_lr != null)
            _lr.enabled = !frozen && drawRope;
    }

    private void FixedUpdate()
    {
        if (_isFrozen) return;
        if (playerA == null || playerB == null) return;

        if (_rbA == null || _rbB == null)
            CacheBodies();

        if (_rbA == null || _rbB == null) return;

        Vector2 a = _rbA.position;
        Vector2 b = _rbB.position;

        Vector2 delta = b - a;
        float dist = delta.magnitude;

        if (dist <= maxDistance || dist <= 0.0001f) return;

        Vector2 dir = delta / dist;
        float overstretch = dist - maxDistance;

        Vector2 pull = dir * (overstretch * pullStrength);

        float relVel = Vector2.Dot(_rbB.linearVelocity - _rbA.linearVelocity, dir);
        Vector2 damp = dir * (relVel * damping);

        _rbA.AddForce(pull + damp, ForceMode2D.Force);
        _rbB.AddForce(-pull - damp, ForceMode2D.Force);
    }

    private void LateUpdate()
    {
        if (_isFrozen || !drawRope || _lr == null || playerA == null || playerB == null)
        {
            if (_lr != null) _lr.enabled = false;
            return;
        }

        _lr.enabled = true;
        _lr.SetPosition(0, playerA.position + ropeOffsetA);
        _lr.SetPosition(1, playerB.position + ropeOffsetB);
    }
}