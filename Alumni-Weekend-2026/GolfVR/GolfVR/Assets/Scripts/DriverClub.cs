using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// The driving-range driver. Held like the putter (sticky grip, Grip point
    /// tilt cycled with the trackpad), but a strike is detected by sweeping the
    /// club head through space every frame -- a 30 m/s head moves half a metre
    /// per frame, far too fast for ordinary collision callbacks -- and launches
    /// the ball with the head's speed, a driver's loft and a smash factor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DriverClub : MonoBehaviour
    {
        [Header("Club Components")]
        public Transform clubHead;
        public Transform gripPoint;

        [Header("Striking")]
        [Tooltip("Radius (m) of the sphere swept through the air by the club head")]
        public float headRadius = 0.06f;

        [Tooltip("Ball speed / club head speed on a good strike (real drivers ~1.45)")]
        public float smashFactor = 1.45f;

        [Tooltip("Extra multiplier to make a casual VR swing carry like a real one")]
        public float swingBoost = 1.3f;

        [Tooltip("Static loft of the club face in degrees; added to whatever upward/downward angle the swing itself has")]
        public float loftDegrees = 11f;

        [Tooltip("Club head speed (m/s) below which a touch is ignored (a nudge, not a stroke)")]
        public float minHitSpeed = 1.5f;

        public float maxBallSpeed = 85f;

        [Header("Haptics")]
        public float hapticDuration = 0.15f;
        public float hapticFrequency = 120f;
        [Range(0f, 1f)] public float hapticAmplitude = 1f;

        [Header("Audio")]
        public AudioSource audioSource;

        [Header("Generated Visual Mesh")]
        public bool autoGenerateVisuals = true;
        public float shaftRadius = 0.009f;
        public float gripRadius = 0.016f;
        public float gripLength = 0.28f;
        public Color shaftColor = new Color(0.15f, 0.15f, 0.17f);
        public Color gripColor = new Color(0.05f, 0.05f, 0.05f);
        public Color headColor = new Color(0.10f, 0.10f, 0.12f);

        [Header("Presentation")]
        [Tooltip("Keep the club floating upright (kinematic) at its starting pose until it's picked up, so it is easy to grab without stooping or walking to a table")]
        public bool floatWhenIdle = true;

        [Header("Grip Pose")]
        [Tooltip("Degrees the shaft leans forward while held. While holding the club, the SnapTurnRight button cycles the presets and the choice is remembered.")]
        public float gripPitchDegrees = 20f;
        public float[] gripPitchPresets = { 0f, 20f, 35f, 50f, 65f };

        private const string GripPitchPrefKey = "GolfVR.DriverGripPitchDegrees";

        private Hand _hand;
        private Interactable _interactable;
        private Throwable _throwable;
        private Rigidbody _rigidbody;
        private SteamVR_Action_Boolean _cycleAction;
        private bool _cycleActionLookedUp;
        private AudioClip _hitClip;

        private Vector3 _prevHeadPos;
        private bool _havePrev;
        private Vector3 _smoothedVelocity;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private bool _hasHome;

        public bool IsHeld => _hand != null;
        public Vector3 HomePosition => _homePosition;
        public Vector3 LastHeadVelocity { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _interactable = GetComponent<Interactable>();
            _throwable = GetComponent<Throwable>();

            if (!_hasHome)
            {
                _homePosition = transform.position;
                _homeRotation = transform.rotation;
                _hasHome = true;
            }

            if (clubHead == null) clubHead = transform.Find("ClubHead") ?? transform;
            if (gripPoint == null) gripPoint = transform.Find("Grip") ?? transform;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f;
                    audioSource.playOnAwake = false;
                }
            }

            gripPitchDegrees = PlayerPrefs.GetFloat(GripPitchPrefKey, gripPitchDegrees);
            ApplyGripPose();
            if (floatWhenIdle) Freeze();
            GenerateHitClip();
            BuildVisualsIfMissing();
        }

        private void Start()
        {
            _prevHeadPos = clubHead.position;
            _havePrev = true;

            if (_interactable != null)
            {
                _interactable.onAttachedToHand += OnAttached;
                _interactable.onDetachedFromHand += OnDetached;
            }
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.onAttachedToHand -= OnAttached;
                _interactable.onDetachedFromHand -= OnDetached;
            }
        }

        private void OnAttached(Hand hand) { _hand = hand; }

        private void OnDetached(Hand hand)
        {
            if (_hand == hand) _hand = null;
        }

        private void Update()
        {
            if (_hand == null)
            {
                // Not in a hand: trackpad right edge brings the club to you.
                if (!_cycleActionLookedUp) LookUpCycleAction();
                if (_cycleAction != null && _cycleAction.GetStateDown(SteamVR_Input_Sources.Any)) SnapToPlayer();
                return;
            }

            if (!_cycleActionLookedUp) LookUpCycleAction();

            if (_cycleAction != null && _cycleAction.GetStateDown(SteamVR_Input_Sources.Any))
            {
                CycleGripPitch();
            }
        }

        private void LookUpCycleAction()
        {
            _cycleActionLookedUp = true;
            try { _cycleAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("SnapTurnRight"); }
            catch { _cycleAction = null; }
        }

        /// <summary>Puts the club upright right in front of the player, ready to grab.</summary>
        public void SnapToPlayer()
        {
            if (Player.instance == null || IsHeld) return;

            Vector3 pos = Player.instance.feetPositionGuess
                        + Player.instance.bodyDirectionGuess.normalized * 0.5f
                        + Vector3.up * 0.15f;
            ResetToPosition(pos, Quaternion.identity);
            if (floatWhenIdle) Freeze();
        }

        private void Freeze()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        // The club is parented to the hand, whose pose updates in Update, so the
        // head position is final by LateUpdate -- sweep from last frame to now.
        private void LateUpdate()
        {
            Vector3 cur = clubHead.position;
            if (_havePrev && Time.deltaTime > 1e-5f)
            {
                TrySweepHit(_prevHeadPos, cur, Time.deltaTime);
            }
            _prevHeadPos = cur;
            _havePrev = true;
        }

        /// <summary>
        /// Sweeps the club head from prevPos to curPos over dt seconds and, if
        /// it passes through a ball waiting on the tee, launches it. Public so
        /// headless tests can drive a swing with synthetic head positions.
        /// Returns the ball that was hit, or null.
        /// </summary>
        public RangeBall TrySweepHit(Vector3 prevPos, Vector3 curPos, float dt)
        {
            Vector3 instant = (curPos - prevPos) / Mathf.Max(dt, 1e-5f);
            // Blend with the previous frame's velocity: a single frame of tracking jitter shouldn't decide the shot.
            Vector3 velocity = _smoothedVelocity.sqrMagnitude > 0f ? Vector3.Lerp(_smoothedVelocity, instant, 0.6f) : instant;
            _smoothedVelocity = instant;
            LastHeadVelocity = velocity;

            if (velocity.magnitude < minHitSpeed) return null;

            Vector3 delta = curPos - prevPos;
            float dist = delta.magnitude;
            RangeBall hitBall = null;

            if (dist > 1e-4f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(prevPos, headRadius, delta / dist, dist, ~0, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                foreach (RaycastHit h in hits)
                {
                    RangeBall b = h.collider.GetComponent<RangeBall>();
                    if (b != null && b.state == RangeBall.State.Ready && h.distance < best)
                    {
                        best = h.distance;
                        hitBall = b;
                    }
                }
            }

            if (hitBall == null)
            {
                foreach (Collider c in Physics.OverlapSphere(curPos, headRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    RangeBall b = c.GetComponent<RangeBall>();
                    if (b != null && b.state == RangeBall.State.Ready)
                    {
                        hitBall = b;
                        break;
                    }
                }
            }

            if (hitBall == null) return null;

            StrikeBall(hitBall, velocity);
            return hitBall;
        }

        private void StrikeBall(RangeBall ball, Vector3 headVelocity)
        {
            float headSpeed = headVelocity.magnitude;
            Vector3 dir = headVelocity / headSpeed;

            // Horizontal direction follows the swing path. A swing that is almost purely vertical
            // has no reliable direction, so fall back to where the player is facing.
            Vector3 horizontal = new Vector3(dir.x, 0f, dir.z);
            if (horizontal.magnitude < 0.25f)
            {
                horizontal = FacingDirection();
            }
            horizontal.Normalize();

            float swingElevation = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float launch = Mathf.Clamp(loftDegrees + swingElevation * 0.35f, 4f, 38f);
            float ballSpeed = Mathf.Min(headSpeed * smashFactor * swingBoost, maxBallSpeed);

            Vector3 velocity = (horizontal * Mathf.Cos(launch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(launch * Mathf.Deg2Rad)) * ballSpeed;

            // The club must not bat the ball a second time on its way through.
            Collider ballCol = ball.GetComponent<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(c, ballCol, true);
            }

            ball.Launch(velocity);

            PlayHit(ballSpeed);
            if (_hand != null)
            {
                try { _hand.TriggerHapticPulse(hapticDuration, hapticFrequency, hapticAmplitude); }
                catch { }
            }

            if (DrivingRangeManager.Instance != null)
            {
                DrivingRangeManager.Instance.OnBallLaunched(ball, headSpeed);
            }
        }

        private Vector3 FacingDirection()
        {
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 f = cam != null ? cam.forward : Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
        }

        // ---- grip pose / hand handling (same scheme as the putter) ----

        public void ApplyGripPose()
        {
            if (gripPoint == null || gripPoint == transform) return;
            gripPoint.localRotation = Quaternion.Euler(gripPitchDegrees, 0f, 0f);
        }

        public void CycleGripPitch()
        {
            if (gripPitchPresets == null || gripPitchPresets.Length == 0) return;

            int next = 0;
            for (int i = 0; i < gripPitchPresets.Length; i++)
            {
                if (Mathf.Approximately(gripPitchPresets[i], gripPitchDegrees))
                {
                    next = (i + 1) % gripPitchPresets.Length;
                    break;
                }
            }

            gripPitchDegrees = gripPitchPresets[next];
            PlayerPrefs.SetFloat(GripPitchPrefKey, gripPitchDegrees);
            ApplyGripPose();

            if (_hand != null)
            {
                Hand hand = _hand;
                Hand.AttachmentFlags flags = _throwable != null
                    ? _throwable.attachmentFlags
                    : Hand.AttachmentFlags.SnapOnAttach | Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.DetachOthers | Hand.AttachmentFlags.TurnOnKinematic;
                hand.AttachObject(gameObject, GrabTypes.Grip, flags, gripPoint);
            }

            Debug.Log($"[GolfVR] Driver grip angle set to {gripPitchDegrees:F0} degrees.");
            if (DrivingRangeManager.Instance != null)
            {
                DrivingRangeManager.Instance.ShowMessage($"Club angle: {gripPitchDegrees:F0}°  (trackpad right to change)");
            }
        }

        /// <summary>Lets go of the (sticky) club if it's in a hand.</summary>
        public void ReleaseFromHand()
        {
            Hand hand = _hand != null ? _hand : GetComponentInParent<Hand>();
            _hand = null;
            if (hand != null) hand.DetachObject(gameObject, false);

            if (transform.parent != null && transform.GetComponentInParent<Hand>() != null)
            {
                transform.SetParent(null, true);
            }
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }
        }

        /// <summary>Puts the club back where it started in the scene (the display table).</summary>
        public void ReturnHome()
        {
            ReleaseFromHand();
            if (_hasHome) ResetToPosition(_homePosition, _homeRotation);
            if (floatWhenIdle) Freeze();
        }

        public void ResetToPosition(Vector3 position, Quaternion rotation)
        {
            if (_rigidbody.isKinematic) _rigidbody.isKinematic = false;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
            _prevHeadPos = clubHead != null ? clubHead.position : position;
            _smoothedVelocity = Vector3.zero;
        }

        // ---- audio / visuals ----

        private void PlayHit(float ballSpeed)
        {
            if (audioSource == null || _hitClip == null) return;
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(_hitClip, Mathf.Clamp(0.4f + ballSpeed / 60f, 0.4f, 1f));
        }

        private void GenerateHitClip()
        {
            int sampleRate = 44100;
            int numSamples = (int)(sampleRate * 0.12f);
            float[] samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 40f);
                float tone = Mathf.Sin(2f * Mathf.PI * 900f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 2300f * t);
                float noise = (Random.value * 2f - 1f) * 0.35f;
                samples[i] = (tone * 0.5f + noise) * env;
            }
            _hitClip = AudioClip.Create("DriverHitProcedural", numSamples, 1, sampleRate, false);
            _hitClip.SetData(samples, 0);
        }

        private void BuildVisualsIfMissing()
        {
            if (!autoGenerateVisuals) return;
            if (GetComponentInChildren<MeshRenderer>() != null) return;

            Material shaftMat = new Material(Shader.Find("Standard")) { color = shaftColor };
            shaftMat.SetFloat("_Metallic", 0.7f);
            shaftMat.SetFloat("_Glossiness", 0.6f);
            Material gripMat = new Material(Shader.Find("Standard")) { color = gripColor };
            gripMat.SetFloat("_Glossiness", 0.2f);
            Material headMat = new Material(Shader.Find("Standard")) { color = headColor };
            headMat.SetFloat("_Metallic", 0.5f);
            headMat.SetFloat("_Glossiness", 0.7f);

            CreateBar("ShaftVisual", gripPoint.localPosition, clubHead.localPosition, shaftRadius, shaftMat);

            Vector3 shaftDir = (clubHead.localPosition - gripPoint.localPosition).normalized;
            if (shaftDir.sqrMagnitude > 0.0001f)
            {
                CreateBar("GripVisual", gripPoint.localPosition, gripPoint.localPosition + shaftDir * gripLength, gripRadius, gripMat);
            }

            // Big rounded driver head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "ClubHeadVisual";
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(clubHead, false);
            head.transform.localPosition = Vector3.zero;
            head.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            head.GetComponent<MeshRenderer>().sharedMaterial = headMat;
        }

        private void CreateBar(string barName, Vector3 localA, Vector3 localB, float radius, Material mat)
        {
            Vector3 delta = localB - localA;
            float length = delta.magnitude;
            if (length < 0.001f) return;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = barName;
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = (localA + localB) * 0.5f;
            bar.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            bar.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
