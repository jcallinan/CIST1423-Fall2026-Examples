using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// A driving-range ball. Sits frozen on the tee until the driver hits it
    /// (<see cref="Launch"/>), then flies with simple aerodynamics -- quadratic
    /// air drag plus a backspin lift force so a good drive carries like a real
    /// one -- records where it first lands (carry), rolls out, and reports its
    /// final resting distance. A trail renderer (added by the manager) draws
    /// the flight.
    ///
    /// All physics bookkeeping lives in <see cref="PhysicsTick"/>, called from
    /// FixedUpdate; the headless tests call it themselves between
    /// Physics.Simulate steps.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class RangeBall : MonoBehaviour
    {
        public enum State { Ready, Flying, Rolling, Done }

        [Header("Aerodynamics")]
        [Tooltip("Quadratic air drag coefficient (m^-1). ~0.0047 matches a real golf ball.")]
        public float airDrag = 0.0047f;

        [Tooltip("Backspin lift coefficient (m^-1): lift acceleration = lift * speed^2, perpendicular to the flight path")]
        public float lift = 0.0026f;

        [Header("Ground")]
        public float rollingDrag = 0.6f;
        public float rollingAngularDrag = 0.6f;

        [Tooltip("Speed (m/s) below which a rolling ball counts as stopped")]
        public float stopSpeed = 0.25f;

        [Tooltip("Seconds it must stay slower than stopSpeed to be finished")]
        public float stopTime = 0.75f;

        public float maxRollTime = 40f;

        public State state = State.Ready;
        public int shotNumber;

        // Results (metres unless noted)
        public Vector3 launchPosition;
        public float launchSpeed;      // m/s
        public float launchAngle;      // degrees above horizontal
        public float apexHeight;       // m above the tee
        public float flightTime;       // s in the air until first touchdown
        public float carryDistance;    // horizontal distance to first touchdown
        public float totalDistance;    // horizontal distance to final rest
        public Vector3 landingPosition;
        public Vector3 restPosition;

        public System.Action<RangeBall> Landed;
        public System.Action<RangeBall> Finished;

        public TrailRenderer Trail { get; set; }

        private Rigidbody _rb;
        private float _radius;
        private float _stateTime;
        private float _slowTimer;
        private readonly Collider[] _overlap = new Collider[8];

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        }

        private float Radius
        {
            get
            {
                if (_radius <= 0f) _radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
                return _radius;
            }
        }

        private Rigidbody Body
        {
            get
            {
                if (_rb == null) _rb = GetComponent<Rigidbody>();
                return _rb;
            }
        }

        /// <summary>Freezes the ball on the tee, ready to be hit.</summary>
        public void MakeReady(Vector3 position)
        {
            state = State.Ready;
            Body.isKinematic = true;
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.position = position;
            if (Trail != null)
            {
                Trail.Clear();
                Trail.emitting = false;
            }
        }

        /// <summary>The club has struck the ball: send it on its way.</summary>
        public void Launch(Vector3 velocity)
        {
            if (state != State.Ready) return;

            launchPosition = transform.position;
            launchSpeed = velocity.magnitude;
            launchAngle = launchSpeed > 0.01f ? Mathf.Asin(Mathf.Clamp(velocity.y / launchSpeed, -1f, 1f)) * Mathf.Rad2Deg : 0f;
            apexHeight = 0f;
            flightTime = 0f;
            _stateTime = 0f;
            _slowTimer = 0f;

            Body.isKinematic = false;
            Body.drag = 0f;
            Body.angularDrag = 0.05f;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.velocity = velocity;

            state = State.Flying;
            if (Trail != null)
            {
                Trail.Clear();
                Trail.emitting = true;
            }
        }

        private void FixedUpdate()
        {
            PhysicsTick(Time.fixedDeltaTime);
        }

        /// <summary>Applies air forces and advances the flight/roll/finish state machine.</summary>
        public void PhysicsTick(float dt)
        {
            if (state == State.Ready || state == State.Done) return;

            _stateTime += dt;
            Vector3 pos = transform.position;

            if (pos.y < -5f)
            {
                Finish();
                return;
            }

            if (state == State.Flying)
            {
                flightTime += dt;
                apexHeight = Mathf.Max(apexHeight, pos.y - launchPosition.y);

                Vector3 v = Body.velocity;
                float speed = v.magnitude;
                if (speed > 0.5f)
                {
                    Vector3 accel = -airDrag * speed * v;
                    Vector3 liftDir = Vector3.Cross(Vector3.Cross(v, Vector3.up), v);
                    if (liftDir.sqrMagnitude > 1e-6f)
                    {
                        accel += liftDir.normalized * (lift * speed * speed);
                    }
                    Body.AddForce(accel, ForceMode.Acceleration);
                }

                // First touchdown (ignore the first instant, while the ball is still leaving the tee).
                if (_stateTime > 0.1f && TouchingGround())
                {
                    landingPosition = pos;
                    carryDistance = Flat(pos - launchPosition).magnitude;
                    state = State.Rolling;
                    _stateTime = 0f;
                    Body.drag = rollingDrag;
                    Body.angularDrag = rollingAngularDrag;
                    Landed?.Invoke(this);
                }
            }
            else if (state == State.Rolling)
            {
                if (Body.velocity.magnitude < stopSpeed)
                {
                    _slowTimer += dt;
                }
                else
                {
                    _slowTimer = 0f;
                }

                if (_slowTimer >= stopTime || _stateTime > maxRollTime)
                {
                    Finish();
                }
            }
        }

        private void Finish()
        {
            restPosition = transform.position;
            totalDistance = Flat(restPosition - launchPosition).magnitude;
            if (carryDistance <= 0f) carryDistance = totalDistance;

            state = State.Done;
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            if (Trail != null) Trail.emitting = false;
            Finished?.Invoke(this);
        }

        private bool TouchingGround()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, Radius + 0.02f, _overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlap[i];
                if (c == null) continue;
                // Static colliders are the ground; other balls and the club have rigidbodies.
                if (c.attachedRigidbody == null) return true;
            }
            return false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
