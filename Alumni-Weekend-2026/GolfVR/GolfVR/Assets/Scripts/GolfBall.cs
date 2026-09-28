using System.Collections;
using UnityEngine;

namespace GolfVR
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class GolfBall : MonoBehaviour
    {
        [Header("Hole")]
        [Tooltip("Which hole (0-8) this ball belongs to. Every hole has its own ball; it only counts for, and only sinks in, its own hole.")]
        public int holeIndex = 0;

        [Header("Physics Settings")]
        [Tooltip("Linear velocity below which the ball is brought to a complete stop")]
        public float stopVelocityThreshold = 0.08f;

        [Tooltip("Rolling drag applied while moving")]
        public float rollingDrag = 0.25f;

        [Tooltip("Angular drag applied while rolling")]
        public float rollingAngularDrag = 0.25f;

        [Header("Course Rules")]
        [Tooltip("Y position below which the ball is marked Out Of Bounds")]
        public float outOfBoundsY = -3.0f;

        [Tooltip("Maximum allowed time for a single shot to roll before forcing a stop")]
        public float maxRollDuration = 12.0f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip bounceClip;

        // State
        private Rigidbody _rigidbody;
        private Vector3 _lastRestPosition;
        private bool _isRolling;
        private float _rollTimer;
        private AudioClip _generatedBounceClip;

        public bool IsRolling => _isRolling;
        public Vector3 LastRestPosition => _lastRestPosition;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.drag = rollingDrag;
            _rigidbody.angularDrag = rollingAngularDrag;
            _rigidbody.maxAngularVelocity = 50f;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1.0f;
                    audioSource.playOnAwake = false;
                }
            }

            GenerateBounceAudioClip();
        }

        private void Start()
        {
            _lastRestPosition = transform.position;
        }

        private void FixedUpdate()
        {
            float speed = _rigidbody.velocity.magnitude;

            if (_isRolling)
            {
                _rollTimer += Time.fixedDeltaTime;

                // Stop ball cleanly if rolling slowly or timed out
                if (speed < stopVelocityThreshold || _rollTimer > maxRollDuration)
                {
                    StopBall();
                }
            }

            // Check out of bounds / hazard fall
            if (transform.position.y < outOfBoundsY)
            {
                HandleOutOfBounds();
            }
        }

        /// <summary>
        /// Called by the putter when a stroke strikes the ball
        /// </summary>
        public void ReceivePutt(Vector3 impulse)
        {
            // Record last rest position before the hit
            _lastRestPosition = transform.position;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.AddForce(impulse, ForceMode.VelocityChange);

            _isRolling = true;
            _rollTimer = 0f;

            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.RecordStroke(holeIndex);
            }
        }

        /// <summary>
        /// Sharply cuts the ball's speed the instant it enters a cup trigger.
        /// There's no physical funnel geometry to capture it, so without this
        /// a putt hit with any real force just rolls straight through the
        /// trigger and out the other side before the hole's sink check runs.
        /// </summary>
        public void DampenForCup()
        {
            _rigidbody.velocity *= 0.1f;
            _rigidbody.angularVelocity *= 0.1f;
        }

        public void StopBall()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _isRolling = false;
            _rollTimer = 0f;
            _lastRestPosition = transform.position;

            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.OnBallStopped(transform.position);
            }
        }

        public void SpawnAtTee(Vector3 teePosition)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = FindRestingPosition(teePosition);
            _lastRestPosition = transform.position;
            _isRolling = false;
            _rollTimer = 0f;
        }

        /// <summary>
        /// Tee markers are authored at ground level (y=0), but the actual
        /// fairway surface sits well above that on raised course pieces.
        /// Raycast down to find the real surface instead of assuming a fixed
        /// height, so this works across flat, sloped, and raised sections.
        /// </summary>
        private Vector3 FindRestingPosition(Vector3 teePosition)
        {
            float radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
            Vector3 castOrigin = teePosition + Vector3.up * 3f;

            if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * radius;
            }

            return teePosition + Vector3.up * radius;
        }

        private void HandleOutOfBounds()
        {
            Debug.Log("[GolfVR] Ball went Out of Bounds! Resetting to last position.");

            // Penalty stroke and reset
            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.RecordPenaltyStroke($"Hole {holeIndex + 1}: Out of Bounds! (+1 Penalty)", holeIndex);
            }

            SpawnAtTee(_lastRestPosition);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Play bumper / wall bounce sound
            if (_isRolling && collision.relativeVelocity.magnitude > 0.5f)
            {
                PlayBounceSound(collision.relativeVelocity.magnitude);
            }
        }

        private void PlayBounceSound(float speed)
        {
            if (audioSource == null) return;

            float vol = Mathf.Clamp01(speed / 4.0f);
            AudioClip clip = bounceClip != null ? bounceClip : _generatedBounceClip;
            if (clip != null)
            {
                audioSource.pitch = Random.Range(1.1f, 1.3f);
                audioSource.PlayOneShot(clip, Mathf.Max(0.2f, vol));
            }
        }

        private void GenerateBounceAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 90f);
                float tone = Mathf.Sin(2f * Mathf.PI * 650f * t);
                samples[i] = tone * env;
            }

            _generatedBounceClip = AudioClip.Create("BounceProcedural", numSamples, 1, sampleRate, false);
            _generatedBounceClip.SetData(samples, 0);
        }
    }
}
