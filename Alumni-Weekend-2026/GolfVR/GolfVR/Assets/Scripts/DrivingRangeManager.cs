using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// Runs the driving range: keeps one ball waiting on the tee, and every time
    /// the driver hits it, leaves that ball (and its coloured flight trail) out on
    /// the range, tees up a fresh one, and records how far the shot went -- carry
    /// (first touchdown) and total (after the roll), apex, ball speed and launch
    /// angle -- on a history board and on a floating label where the ball stopped.
    /// </summary>
    public class DrivingRangeManager : MonoBehaviour
    {
        public static DrivingRangeManager Instance { get; private set; }

        [Header("References")]
        public DriverClub club;
        [Tooltip("Where the ball is teed up (its resting point on the mat)")]
        public Transform teePoint;

        [Header("Ball & Trail")]
        public Material ballMaterial;
        public Material trailMaterial;
        public float ballRadius = 0.03f;
        [Tooltip("Height (m) of the tee peg the ball sits on")]
        public float teeHeight = 0.04f;
        [Tooltip("How many balls (with their trails) stay out on the range before the oldest is removed")]
        public int maxBallsOnRange = 12;
        [Tooltip("Seconds after a hit before the next ball appears on the tee")]
        public float respawnDelay = 0.8f;

        [Header("Displays")]
        public TextMesh lastShotText;
        public TextMesh historyText;
        public TextMesh messageText;
        public Font labelFont;
        public int historyRows = 8;

        private readonly List<RangeBall> _balls = new List<RangeBall>();
        private readonly List<GameObject> _labels = new List<GameObject>();
        private readonly List<RangeBall> _finished = new List<RangeBall>();
        private int _shotCounter;
        private float _messageHideTime;
        private PhysicMaterial _ballPhysics;

        private const float YardsPerMeter = 1.0936133f;
        private const float MphPerMps = 2.2369363f;

        public int ShotCount => _shotCounter;
        public IReadOnlyList<RangeBall> FinishedShots => _finished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SpawnBall();
            RefreshDisplays(null);
        }

        private void Update()
        {
            if (messageText != null && messageText.gameObject.activeSelf && Time.time > _messageHideTime)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        /// <summary>Puts a new ball on the tee, ready to be hit.</summary>
        public RangeBall SpawnBall()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "RangeBall_" + (_shotCounter + 1);
            go.transform.localScale = Vector3.one * (ballRadius * 2f);

            Renderer r = go.GetComponent<Renderer>();
            Color color = Color.HSVToRGB((_shotCounter * 0.137f) % 1f, 0.75f, 1f);
            if (ballMaterial != null)
            {
                r.sharedMaterial = ballMaterial;
            }
            // Tint per shot so a ball and its trail can be matched by colour.
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", Color.Lerp(Color.white, color, 0.35f));
            r.SetPropertyBlock(mpb);

            SphereCollider col = go.GetComponent<SphereCollider>();
            if (_ballPhysics == null)
            {
                _ballPhysics = new PhysicMaterial("RangeBallPhysics")
                {
                    bounciness = 0.35f,
                    dynamicFriction = 0.5f,
                    staticFriction = 0.5f,
                    bounceCombine = PhysicMaterialCombine.Average,
                    frictionCombine = PhysicMaterialCombine.Average
                };
            }
            col.sharedMaterial = _ballPhysics;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.0459f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxAngularVelocity = 200f;

            // The teleport pointer should pass over balls rather than turning red on them.
            go.AddComponent<Valve.VR.InteractionSystem.IgnoreTeleportTrace>();

            RangeBall ball = go.AddComponent<RangeBall>();
            ball.shotNumber = ++_shotCounter;

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 3600f;
            trail.minVertexDistance = 0.25f;
            trail.widthMultiplier = 0.07f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.alignment = LineAlignment.View;
            if (trailMaterial != null) trail.sharedMaterial = trailMaterial;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            trail.colorGradient = g;
            trail.emitting = false;
            ball.Trail = trail;

            Vector3 tee = teePoint != null ? teePoint.position : Vector3.zero;
            ball.MakeReady(tee + Vector3.up * (teeHeight + ballRadius));

            ball.Landed += OnBallLanded;
            ball.Finished += OnBallFinished;

            _balls.Add(ball);
            TrimOldBalls();
            return ball;
        }

        /// <summary>Called by the club the moment it strikes a ball.</summary>
        public void OnBallLaunched(RangeBall ball, float clubHeadSpeed)
        {
            Debug.Log($"[GolfVR] Shot #{ball.shotNumber}: club head {clubHeadSpeed:F1} m/s ({clubHeadSpeed * MphPerMps:F0} mph), ball {ball.launchSpeed:F1} m/s, launch {ball.launchAngle:F1} deg.");
            RefreshDisplays(ball);
            Invoke(nameof(SpawnNext), respawnDelay);
        }

        public void SpawnNext()
        {
            // Never stack two balls on the tee.
            foreach (RangeBall b in _balls)
            {
                if (b != null && b.state == RangeBall.State.Ready) return;
            }
            SpawnBall();
        }

        private void OnBallLanded(RangeBall ball)
        {
            Debug.Log($"[GolfVR] Shot #{ball.shotNumber} landed: carry {ball.carryDistance * YardsPerMeter:F0} yd ({ball.carryDistance:F0} m), apex {ball.apexHeight:F1} m.");
            RefreshDisplays(ball);
        }

        private void OnBallFinished(RangeBall ball)
        {
            if (!_finished.Contains(ball)) _finished.Add(ball);
            Debug.Log($"[GolfVR] Shot #{ball.shotNumber} finished: carry {ball.carryDistance * YardsPerMeter:F0} yd, total {ball.totalDistance * YardsPerMeter:F0} yd ({ball.totalDistance:F0} m).");
            CreateDistanceLabel(ball);
            RefreshDisplays(ball);
        }

        // Edit-mode tests can't use Destroy().
        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        private void TrimOldBalls()
        {
            while (_balls.Count > maxBallsOnRange + 1)
            {
                RangeBall oldest = _balls[0];
                _balls.RemoveAt(0);
                if (oldest == null) continue;
                _finished.Remove(oldest);
                Kill(oldest.gameObject);
            }
            // Labels are parented to their balls' rest spots, not the balls; drop the oldest ones too.
            while (_labels.Count > maxBallsOnRange)
            {
                if (_labels[0] != null) Kill(_labels[0]);
                _labels.RemoveAt(0);
            }
        }

        /// <summary>Wipes every ball, trail, label and the history, and tees up a fresh ball.</summary>
        public void ClearAll()
        {
            CancelInvoke(nameof(SpawnNext));
            foreach (RangeBall b in _balls) if (b != null) Kill(b.gameObject);
            foreach (GameObject l in _labels) if (l != null) Kill(l);
            _balls.Clear();
            _labels.Clear();
            _finished.Clear();
            _shotCounter = 0;
            SpawnBall();
            RefreshDisplays(null);
            ShowMessage("Range cleared");
        }

        public void ShowMessage(string message, float seconds = 2.5f)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            _messageHideTime = Time.time + seconds;
        }

        // ---- displays ----

        public static string Distance(float meters)
        {
            return $"{meters * YardsPerMeter:F0} yd ({meters:F0} m)";
        }

        private void RefreshDisplays(RangeBall current)
        {
            if (lastShotText != null)
            {
                if (current == null)
                {
                    lastShotText.text = _finished.Count == 0 ? "DRIVING RANGE\nGrab the driver and\nswing through the ball!" : "";
                }
                else if (current.state == RangeBall.State.Flying)
                {
                    lastShotText.text = $"SHOT #{current.shotNumber}\nIN FLIGHT...\n{current.launchSpeed * MphPerMps:F0} mph  {current.launchAngle:F0}°";
                }
                else if (current.state == RangeBall.State.Rolling)
                {
                    lastShotText.text = $"SHOT #{current.shotNumber}\nCARRY {Distance(current.carryDistance)}\nrolling...";
                }
                else
                {
                    lastShotText.text = $"SHOT #{current.shotNumber}\nTOTAL {Distance(current.totalDistance)}\nCarry {Distance(current.carryDistance)}";
                }
            }

            if (historyText != null)
            {
                StringBuilder sb = new StringBuilder();
                if (_finished.Count == 0)
                {
                    sb.Append("No shots yet");
                }
                else
                {
                    float best = 0f, sum = 0f;
                    foreach (RangeBall b in _finished)
                    {
                        best = Mathf.Max(best, b.totalDistance);
                        sum += b.totalDistance;
                    }
                    sb.Append($"BEST {best * YardsPerMeter:F0} yd   AVG {sum / _finished.Count * YardsPerMeter:F0} yd   SHOTS {_finished.Count}\n");
                    int shown = 0;
                    for (int i = _finished.Count - 1; i >= 0 && shown < historyRows; i--, shown++)
                    {
                        RangeBall b = _finished[i];
                        sb.Append($"#{b.shotNumber}   {b.totalDistance * YardsPerMeter:F0} yd  (carry {b.carryDistance * YardsPerMeter:F0})   apex {b.apexHeight:F0} m   {b.launchSpeed * MphPerMps:F0} mph\n");
                    }
                }
                historyText.text = sb.ToString();
            }
        }

        private void CreateDistanceLabel(RangeBall ball)
        {
            GameObject go = new GameObject("DistanceLabel_" + ball.shotNumber);
            go.transform.position = ball.restPosition + Vector3.up * 0.6f;

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = $"#{ball.shotNumber}\n{ball.totalDistance * YardsPerMeter:F0} yd";
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = 0.06f;
            tm.color = Color.HSVToRGB(((ball.shotNumber - 1) * 0.137f) % 1f, 0.6f, 1f);
            if (labelFont != null)
            {
                tm.font = labelFont;
                go.GetComponent<MeshRenderer>().sharedMaterial = labelFont.material;
            }

            go.AddComponent<RangeLabel>();
            _labels.Add(go);
        }
    }

    /// <summary>Keeps a floating text label facing the player and readable at any distance.</summary>
    public class RangeLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 toCam = cam.transform.position - transform.position;
            float dist = toCam.magnitude;
            // Text reads correctly from its local -Z side, so point +Z away from the viewer.
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            // Grow with distance so a ball 200 m away still has a legible label.
            transform.localScale = Vector3.one * Mathf.Clamp(dist * 0.03f, 0.6f, 12f);
        }
    }
}
