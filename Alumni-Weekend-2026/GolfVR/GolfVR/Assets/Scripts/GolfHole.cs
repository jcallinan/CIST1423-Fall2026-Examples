using System.Collections;
using UnityEngine;

namespace GolfVR
{
    [RequireComponent(typeof(Collider))]
    public class GolfHole : MonoBehaviour
    {
        [Header("Hole Identity")]
        [Tooltip("Hole number (1, 2, or 3)")]
        public int holeNumber = 1;

        [Tooltip("Hole name")]
        public string holeName = "Panther Straightaway";

        [Tooltip("Par score for this hole")]
        public int par = 2;

        [Header("Course Navigation Points")]
        [Tooltip("Where the ball starts on this hole")]
        public Transform teePoint;

        [Tooltip("Where the player teleports/stands when starting this hole")]
        public Transform playerTeeLocation;

        [Header("Celebratory Effects")]
        [Tooltip("Optional animated flag")]
        public Transform flagTransform;

        [Tooltip("Confetti or particle celebration")]
        public ParticleSystem celebrationParticles;

        [Tooltip("Audio source for victory chime")]
        public AudioSource audioSource;
        public AudioClip celebrationFanfare;

        private bool _isCompleted = false;
        private AudioClip _generatedFanfareClip;

        public bool IsCompleted => _isCompleted;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

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

            GenerateFanfareAudioClip();

            if (celebrationParticles == null)
            {
                celebrationParticles = CreateDefaultCelebrationParticles();
            }
        }

        [Header("Cup Capture")]
        [Tooltip("Horizontal distance (m) from the cup center inside which a slow ball is gently pulled toward the cup")]
        public float captureRadius = 0.30f;

        [Tooltip("Balls faster than this (m/s) inside the capture radius skip over the cup instead of being pulled in")]
        public float captureMaxSpeed = 3.0f;

        [Tooltip("Pull strength (m/s^2) toward the cup center while a slow ball is inside the capture radius")]
        public float capturePull = 4.0f;

        [Tooltip("Horizontal distance (m) from the cup center within which a ball that has dropped below the green counts as holed")]
        public float sinkRadius = 0.14f;

        [Tooltip("The ball counts as holed once its center is below this height (m) above the hole's origin -- the green surface around the cup sits ~0.10 above it, a ball resting on the green sits ~0.15")]
        public float sinkHeight = 0.11f;

        private Rigidbody _ballBody;

        /// <summary>
        /// The ball that belongs to this hole (every hole has its own). A
        /// stand-alone hole with no manager falls back to any ball in the scene.
        /// </summary>
        private GolfBall OwnBall()
        {
            MiniGolfGameManager m = MiniGolfGameManager.Instance;
            if (m == null || m.holes == null) return FindObjectOfType<GolfBall>();
            for (int i = 0; i < m.holes.Length; i++)
            {
                if (m.holes[i] == this) return m.GetBallForHole(i);
            }
            return null;
        }

        /// <summary>
        /// Distance-based cup logic, run every physics step for THIS hole and its own
        /// ball only. Deliberately not built on trigger callbacks: Unity
        /// stops sending OnTriggerStay to a sleeping rigidbody, and a ball
        /// that has just come to rest is exactly the case that needs to
        /// register. Two stages:
        ///  1. capture assist -- a slow ball within captureRadius is pulled
        ///     toward the cup center so a near miss still drops in;
        ///  2. sink -- once the ball is within sinkRadius AND below the
        ///     green's surface (i.e. actually in the physical cup), it counts.
        /// </summary>
        private void FixedUpdate()
        {
            if (_isCompleted) return;

            GolfBall ball = OwnBall();
            if (ball == null) return;
            if (_ballBody == null) _ballBody = ball.GetComponent<Rigidbody>();
            if (_ballBody == null) return;

            Vector3 toCup = transform.position - ball.transform.position;
            float heightAboveCupOrigin = -toCup.y;
            toCup.y = 0f;
            float dist = toCup.magnitude;

            if (dist < sinkRadius && heightAboveCupOrigin < sinkHeight)
            {
                Sink(ball, snapBallToHole: false);
                return;
            }

            if (dist < captureRadius && dist > 0.001f && heightAboveCupOrigin >= sinkHeight
                && _ballBody.velocity.magnitude < captureMaxSpeed)
            {
                _ballBody.AddForce(toCup / dist * capturePull, ForceMode.Acceleration);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Secondary path (also what the headless playtest drives directly):
            // a ball already down in the cup counts even if FixedUpdate hasn't
            // run yet.
            if (_isCompleted) return;

            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null || ball != OwnBall()) return; // another hole's ball can never sink here

            Vector3 flat = ball.transform.position - transform.position;
            float height = flat.y;
            flat.y = 0f;
            if (flat.magnitude < sinkRadius && height < sinkHeight)
            {
                Sink(ball, snapBallToHole: false);
            }
        }

        private void Sink(GolfBall ball, bool snapBallToHole)
        {
            _isCompleted = true;
            if (ball != null)
            {
                if (snapBallToHole)
                {
                    // Only used by the debug force-sink path, where the ball
                    // hasn't actually rolled into the cup and needs a visual
                    // nudge. SpawnAtTee raycasts down to the real surface
                    // first -- directly teleporting to transform.position
                    // (the hole trigger's own anchor, which can sit at or
                    // below the green's solid collider) used to embed the
                    // ball in that geometry, and the physics engine would
                    // violently eject it out of view on the next physics
                    // step the instant it settled here.
                    ball.SpawnAtTee(transform.position);
                }
                ball.StopBall();
            }

            Debug.Log($"[GolfVR] Hole {holeNumber} SUNK (ball at {(ball != null ? ball.transform.position.ToString("F2") : "n/a")}).");

            // The celebration is decoration -- an exception inside it must
            // never stop the round from advancing to the next hole.
            try
            {
                PlayCelebration();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.OnHoleSunk(this);
            }
        }

        /// <summary>
        /// Testing convenience: forces this hole to sink right now, running
        /// the exact same celebration/scoring path a real putt would (ball
        /// snapped into the cup, confetti, fanfare, fireworks, hole
        /// advance) without needing to actually putt the ball in. Right-click
        /// the MiniGolfGameManager component in the Inspector during Play
        /// mode and choose "DEBUG: Sink Current Hole" to call it.
        /// </summary>
        [ContextMenu("DEBUG: Force Sink This Hole")]
        public void DebugForceSink()
        {
            if (_isCompleted)
            {
                Debug.Log($"[GolfVR] {name} is already completed.");
                return;
            }

            GolfBall ball = OwnBall();
            if (ball == null)
            {
                Debug.LogWarning($"[GolfVR] {name}: no ball found for this hole.");
                return;
            }

            Sink(ball, snapBallToHole: true);
        }

        public void PlayCelebration()
        {
            // 1. Particle effect
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }

            // 2. Victory chime / fanfare
            if (audioSource != null)
            {
                AudioClip clip = celebrationFanfare != null ? celebrationFanfare : _generatedFanfareClip;
                if (clip != null)
                {
                    audioSource.pitch = 1.0f;
                    audioSource.PlayOneShot(clip, 0.9f);
                }
            }

            // 3. Flag spin / bounce animation
            if (flagTransform != null)
            {
                StartCoroutine(AnimateFlag());
            }

            // 4. Alumni Weekend fireworks: night sky, fireworks, horns, back to day
            if (FireworksCelebrationController.Instance != null)
            {
                FireworksCelebrationController.Instance.PlayFireworksCelebration();
            }
        }

        private IEnumerator AnimateFlag()
        {
            float elapsed = 0f;
            float duration = 2.0f;
            Vector3 origScale = flagTransform.localScale;
            Quaternion origRot = flagTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Spin around Y and slight celebration bounce
                flagTransform.Rotate(Vector3.up, 360f * Time.deltaTime * 2.0f, Space.World);
                float bounce = 1f + 0.15f * Mathf.Sin(progress * Mathf.PI * 4f);
                flagTransform.localScale = origScale * bounce;

                yield return null;
            }

            flagTransform.localScale = origScale;
            flagTransform.localRotation = origRot;
        }

        public void ResetHole()
        {
            _isCompleted = false;
        }

        /// <summary>
        /// Generates a triumphant 4-note chord fanfare procedurally
        /// </summary>
        private void GenerateFanfareAudioClip()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            // Notes: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz) arpeggio
            float[] frequencies = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = 0.22f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float freq = frequencies[noteIndex];

                // Envelope per note
                float noteT = (t % noteDuration);
                float env = Mathf.Exp(-noteT * 3.5f);

                // Pleasant bell-like harmonics
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                samples[i] = v * env * 0.4f;
            }

            _generatedFanfareClip = AudioClip.Create("VictoryFanfareProcedural", numSamples, 1, sampleRate, false);
            _generatedFanfareClip.SetData(samples, 0);
        }

        /// <summary>
        /// Builds a simple confetti-burst particle effect so sinking the ball
        /// has a visual payoff, not just a sound.
        /// </summary>
        private ParticleSystem CreateDefaultCelebrationParticles()
        {
            GameObject psObject = new GameObject("CelebrationConfetti");
            psObject.transform.SetParent(transform, false);
            psObject.transform.localPosition = Vector3.up * 0.3f;

            ParticleSystem ps = psObject.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 3.0f;
            main.startSize = 0.08f;
            main.gravityModifier = 1.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.55f, 1f));

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.1f;

            return ps;
        }
    }
}
