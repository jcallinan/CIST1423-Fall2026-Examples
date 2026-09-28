using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// Runs the 9-hole round. Every hole has its OWN ball sitting at its tee
    /// (GolfBall.holeIndex says which hole a ball belongs to), so there is no
    /// carrying one ball from hole to hole and nothing that has to "advance":
    /// the player just walks or teleports to any hole and putts the ball
    /// that's waiting there. Strokes are counted per ball, a hole's sink only
    /// counts for its own ball, and the round is finished once all nine holes
    /// are completed. The "current hole" (shown on the scoreboard, which
    /// follows the player) is simply whichever hole the player is at.
    /// </summary>
    public class MiniGolfGameManager : MonoBehaviour
    {
        public static MiniGolfGameManager Instance { get; private set; }

        [Header("Course Setup (9 Holes)")]
        public GolfHole[] holes = new GolfHole[9];

        [Header("Equipment & Player")]
        [Tooltip("The Hole 1 ball. The full set (one per hole) is discovered at startup from every GolfBall's holeIndex.")]
        public GolfBall golfBall;
        public GolfPutter golfPutter;
        public Transform playerTransform;

        [Header("UI Scoreboard")]
        public ScoreboardUI scoreboard;

        [Header("Banners")]
        [Tooltip("Seconds the 'HOLE COMPLETE' banner stays up after a sink")]
        public float holeTransitionDelay = 3.5f;

        [Tooltip("How close (m) the player must be to a hole's tee for the scoreboard to switch to that hole")]
        public float scoreboardFollowRadius = 6f;

        [Header("Audio")]
        public AudioSource globalAudioSource;

        // One ball per hole, indexed by hole index.
        private GolfBall[] _holeBalls;

        // Game State
        private int _currentHoleIndex = 0;
        private int[] _strokesPerHole;
        private bool _isGameFinished = false;
        private float _nextFollowCheck;

        public int CurrentHoleIndex => _currentHoleIndex;
        public int CurrentHoleNumber => _currentHoleIndex + 1;
        public int CurrentHoleStrokes => (_strokesPerHole != null && _currentHoleIndex < _strokesPerHole.Length) ? _strokesPerHole[_currentHoleIndex] : 0;
        public bool IsGameFinished => _isGameFinished;

        /// <summary>The ball belonging to the hole the player is currently at.</summary>
        public GolfBall CurrentBall => GetBallForHole(_currentHoleIndex);

        private int HoleCount => holes != null && holes.Length > 0 ? holes.Length : 9;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (globalAudioSource == null)
            {
                globalAudioSource = GetComponent<AudioSource>();
            }

            _strokesPerHole = new int[HoleCount];
            ResolveBalls();
        }

        private void Start()
        {
            InitializeRound();
        }

        /// <summary>
        /// Builds the hole-index -> ball table from every GolfBall in the
        /// scene. A scene with a single ball (older layouts) just gets that
        /// ball for hole 1.
        /// </summary>
        private void ResolveBalls()
        {
            int count = HoleCount;
            _holeBalls = new GolfBall[count];

            foreach (GolfBall b in FindObjectsOfType<GolfBall>())
            {
                int i = Mathf.Clamp(b.holeIndex, 0, count - 1);
                if (_holeBalls[i] == null) _holeBalls[i] = b;
            }

            if (_holeBalls[0] == null && golfBall != null) _holeBalls[0] = golfBall;
            if (_holeBalls[0] != null) golfBall = _holeBalls[0];
        }

        public GolfBall GetBallForHole(int index)
        {
            if (_holeBalls == null) ResolveBalls();
            if (index < 0 || index >= _holeBalls.Length) return null;
            return _holeBalls[index];
        }

        private int IndexOfHole(GolfHole hole)
        {
            if (holes == null) return -1;
            for (int i = 0; i < holes.Length; i++)
            {
                if (holes[i] == hole) return i;
            }
            return -1;
        }

        /// <summary>
        /// Resets scoring and every hole, puts every ball back on its own
        /// tee and sends the player to Hole 1. The putter's position is left
        /// alone by default -- on first app launch it should stay wherever
        /// it's placed in the scene (e.g. a starting display table) until a
        /// player has picked it up -- but pass true (as ResetForNextGroup
        /// does) to also let go of it and snap it back into place, e.g. when
        /// staff are resetting the course for a new group.
        /// </summary>
        public void InitializeRound(bool repositionPutter = false)
        {
            _currentHoleIndex = 0;
            _isGameFinished = false;

            int count = HoleCount;
            _strokesPerHole = new int[count];

            for (int i = 0; i < count; i++)
            {
                if (holes != null && i < holes.Length && holes[i] != null)
                {
                    holes[i].ResetHole();
                    PutBallOnTee(i);
                }
            }

            // Player to Hole 1 and its ball to the tee. On a full reset the
            // putter goes back to its STARTING position in the scene (the
            // display table) -- letting go of it first if it's in a hand,
            // since it's sticky.
            SetupHole(0, false);
            if (repositionPutter && golfPutter != null)
            {
                golfPutter.ReturnHome();
            }

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner("WELCOME TO HOLE 1! Every hole has its own ball -- walk or teleport to any hole and putt!");
            }
        }

        /// <summary>
        /// For event staff: resets the whole round for the next group of
        /// players, including snapping the putter and every ball back to
        /// their starting places so the course looks freshly set up.
        /// </summary>
        public void ResetForNextGroup()
        {
            InitializeRound(repositionPutter: true);
        }

        private void PutBallOnTee(int index)
        {
            GolfBall ball = GetBallForHole(index);
            if (ball != null && holes[index] != null && holes[index].teePoint != null)
            {
                ball.SpawnAtTee(holes[index].teePoint.position);
            }
        }

        /// <summary>
        /// Makes the given hole the one the player is "at": moves the player
        /// there, resets that hole's ball onto its tee, and (optionally)
        /// snaps the putter beside it.
        /// </summary>
        private void SetupHole(int index, bool repositionPutter = true)
        {
            if (holes == null || index < 0 || index >= holes.Length || holes[index] == null) return;

            GolfHole currentHole = holes[index];
            currentHole.ResetHole();

            // 1. This hole's ball back on its tee
            PutBallOnTee(index);

            // 2. Move player near the tee
            if (currentHole.playerTeeLocation != null)
            {
                TeleportPlayer(currentHole.playerTeeLocation.position, currentHole.playerTeeLocation.rotation);
            }

            // 3. Putter: let go of it if it's in a hand (it's sticky) and put it on
            // the ground right in front of where the player now stands, ready
            // to be picked up again.
            if (repositionPutter && golfPutter != null)
            {
                golfPutter.ReleaseFromHand();
                PlacePutterForPickup(currentHole);
            }

            // 4. Scoreboard + "current hole" follow the player to this hole
            SetCurrentHole(index);
        }

        /// <summary>
        /// Lays the putter flat on the ground just in front of and beside the
        /// spot the player stands at for this hole, so it is right by their
        /// feet after a reset or jump instead of somewhere across the green.
        /// </summary>
        private void PlacePutterForPickup(GolfHole hole)
        {
            Transform stand = hole.playerTeeLocation != null ? hole.playerTeeLocation : hole.teePoint;
            if (stand == null) return;

            Vector3 spot = stand.position + stand.forward * 0.45f + stand.right * 0.3f;
            float surfaceY = Physics.Raycast(spot + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : stand.position.y;

            // Lying flat (shaft horizontal), a little above the ground so it settles.
            Quaternion lying = Quaternion.LookRotation(stand.forward, Vector3.up) * Quaternion.Euler(0f, 0f, 90f);
            golfPutter.ResetToPosition(new Vector3(spot.x, surfaceY + 0.1f, spot.z), lying);
        }

        /// <summary>
        /// Points the scoreboard (and the flag animators / hole indicator)
        /// at the given hole, bringing the scoreboard along to that hole's
        /// tee instead of leaving it stranded where the player has left.
        /// </summary>
        public void SetCurrentHole(int index)
        {
            if (holes == null || index < 0 || index >= holes.Length || holes[index] == null) return;

            _currentHoleIndex = index;
            GolfHole currentHole = holes[index];

            if (scoreboard != null && currentHole.playerTeeLocation != null)
            {
                Transform teeLoc = currentHole.playerTeeLocation;
                Vector3 boardPos = teeLoc.position + teeLoc.right * 1.8f + Vector3.up * 1.0f;
                scoreboard.transform.position = boardPos;
                // LookRotation points the object's local +Z at the target,
                // but a Canvas reads correctly from its local -Z side (same
                // convention as this project's TextMesh signs) -- so the
                // forward vector needs to point AWAY from the tee, not at
                // it, or a player standing at the tee sees the scoreboard
                // mirrored/backwards (confirmed via a headless screenshot
                // taken from the tee's own position).
                scoreboard.transform.rotation = Quaternion.LookRotation(boardPos - teeLoc.position, Vector3.up);
            }

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
            }
        }

        /// <summary>Counts a stroke against the current hole.</summary>
        public void RecordStroke()
        {
            RecordStroke(_currentHoleIndex);
        }

        /// <summary>Counts a stroke against the given hole (called by that hole's ball).</summary>
        public void RecordStroke(int holeIndex)
        {
            if (_isGameFinished) return;
            if (_strokesPerHole == null || holeIndex < 0 || holeIndex >= _strokesPerHole.Length) return;
            if (holes != null && holeIndex < holes.Length && holes[holeIndex] != null && holes[holeIndex].IsCompleted) return;

            _strokesPerHole[holeIndex]++;
            Debug.Log($"[GolfVR] Stroke recorded! Hole {holeIndex + 1}: {_strokesPerHole[holeIndex]} strokes.");

            // Putting a ball means the player is at that hole.
            if (holeIndex != _currentHoleIndex)
            {
                SetCurrentHole(holeIndex);
            }
            else if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
            }
        }

        public void RecordPenaltyStroke(string reason)
        {
            RecordPenaltyStroke(reason, _currentHoleIndex);
        }

        public void RecordPenaltyStroke(string reason, int holeIndex)
        {
            if (_isGameFinished) return;
            if (_strokesPerHole == null || holeIndex < 0 || holeIndex >= _strokesPerHole.Length) return;

            _strokesPerHole[holeIndex]++;

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                scoreboard.ShowBanner(reason, 2.5f);
            }
        }

        public void OnBallStopped(Vector3 position)
        {
            // Ball came to rest
        }

        /// <summary>
        /// A hole's own ball went in the cup: score it, celebrate, and finish
        /// the round if that was the last open hole. Nothing moves -- the
        /// player is free to head to whichever hole they like next.
        /// </summary>
        public void OnHoleSunk(GolfHole hole)
        {
            int index = IndexOfHole(hole);
            if (index < 0)
            {
                Debug.LogWarning($"[GolfVR] Hole {hole.holeNumber} sunk but isn't in the manager's hole list -- ignoring.");
                return;
            }

            if (_isGameFinished)
            {
                Debug.Log($"[GolfVR] Hole {hole.holeNumber} sunk but the round is already finished -- ignoring. Press the reset kiosk to start a new round.");
                return;
            }

            _currentHoleIndex = index;

            int strokes = _strokesPerHole[index];
            int diff = strokes - hole.par;

            string scoreTerm;
            if (strokes <= 1) scoreTerm = "HOLE IN ONE! 🌟";
            else if (diff <= -2) scoreTerm = "EAGLE! 🦅";
            else if (diff == -1) scoreTerm = "BIRDIE! 🐦";
            else if (diff == 0) scoreTerm = "PAR! ⛳";
            else if (diff == 1) scoreTerm = "BOGEY 🏌️";
            else scoreTerm = $"+{diff} BOGEY 🏌️";

            int remaining = 0;
            for (int i = 0; i < holes.Length; i++)
            {
                if (holes[i] != null && !holes[i].IsCompleted) remaining++;
            }

            int nextOpen = -1;
            for (int k = 1; k < holes.Length; k++)
            {
                int j = (index + k) % holes.Length;
                if (holes[j] != null && !holes[j].IsCompleted)
                {
                    nextOpen = j;
                    break;
                }
            }

            string msg = $"HOLE {hole.holeNumber} COMPLETE!\n{scoreTerm} ({strokes} Strokes)";
            if (remaining > 0 && nextOpen >= 0) msg += $"\nNext up: Hole {nextOpen + 1}";
            Debug.Log($"[GolfVR] {msg.Replace('\n', ' ')} -- {remaining} hole(s) left.");

            // Scoreboard/banner work is presentation only: guard it so a UI
            // problem can never affect the scoring state above.
            try
            {
                if (scoreboard != null)
                {
                    scoreboard.UpdateScoreboard(holes, _strokesPerHole, _currentHoleIndex);
                    scoreboard.ShowBanner(msg, holeTransitionDelay);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (remaining == 0)
            {
                _isGameFinished = true;
                Debug.Log("[GolfVR] Round complete.");

                int totalStrokes = 0;
                int totalPar = 0;
                for (int i = 0; i < holes.Length; i++)
                {
                    if (i < _strokesPerHole.Length) totalStrokes += _strokesPerHole[i];
                    if (holes[i] != null) totalPar += holes[i].par;
                }

                int totalDiff = totalStrokes - totalPar;
                string totalDiffStr = totalDiff == 0 ? "Even Par" : (totalDiff > 0 ? $"+{totalDiff}" : $"{totalDiff}");
                string finalMsg = $"🏆 {holes.Length}-HOLE CHAMPIONSHIP COMPLETE! 🏆\nTotal: {totalStrokes} Strokes ({totalDiffStr})\nThank you for visiting the UPB VR Lab!";

                try
                {
                    if (scoreboard != null)
                    {
                        scoreboard.UpdateScoreboard(holes, _strokesPerHole, holes.Length - 1);
                        scoreboard.ShowBanner(finalMsg, 12.0f);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void TeleportPlayer(Vector3 targetPos, Quaternion targetRot)
        {
            if (Player.instance != null)
            {
                Transform playerRoot = Player.instance.transform;
                playerRoot.position = targetPos;
                playerRoot.rotation = targetRot;
            }
            else if (playerTransform != null)
            {
                playerTransform.position = targetPos;
                playerTransform.rotation = targetRot;
            }
        }

        /// <summary>Puts the current hole's ball back on its tee.</summary>
        public void ResetCurrentBallToTee()
        {
            PutBallOnTee(_currentHoleIndex);
        }

        /// <summary>
        /// Testing convenience: takes the player straight to any hole and
        /// resets just that hole -- its completed flag, stroke count and
        /// ball (back on its tee) -- plus the putter beside it. Other holes'
        /// progress is left alone. For the physical "pick a hole" panel; also
        /// doubles as a per-hole "put the ball and putter back" button when
        /// pressed for the hole you're already at.
        /// </summary>
        public void JumpToHole(int index)
        {
            if (holes == null || index < 0 || index >= holes.Length || holes[index] == null)
            {
                Debug.LogWarning($"[GolfVR] JumpToHole: index {index} is out of range.");
                return;
            }

            _isGameFinished = false;

            if (_strokesPerHole == null || _strokesPerHole.Length != HoleCount)
            {
                _strokesPerHole = new int[HoleCount];
            }
            _strokesPerHole[index] = 0;

            SetupHole(index, repositionPutter: true);

            if (scoreboard != null)
            {
                scoreboard.UpdateScoreboard(holes, _strokesPerHole, index);
                scoreboard.ShowBanner($"JUMPED TO HOLE {index + 1}", 2.5f);
            }

            Debug.Log($"[GolfVR] Jumped to hole {index + 1}.");
        }

        /// <summary>
        /// Travel only: takes the player to the given hole's tee (wrapping
        /// past the last hole back to the first when `index` is out of
        /// range) WITHOUT resetting anything -- the hole's ball is wherever
        /// it was left, the score is kept, and the putter stays in the hand.
        /// Used by the "NEXT HOLE" buttons at every tee, as a reliable
        /// alternative to the arc teleport.
        /// </summary>
        public void GoToHole(int index)
        {
            if (holes == null || holes.Length == 0) return;
            index = ((index % holes.Length) + holes.Length) % holes.Length;
            if (holes[index] == null) return;

            GolfHole hole = holes[index];
            if (hole.playerTeeLocation != null)
            {
                TeleportPlayer(hole.playerTeeLocation.position, hole.playerTeeLocation.rotation);
            }

            SetCurrentHole(index);

            if (scoreboard != null)
            {
                string state = hole.IsCompleted ? "(already sunk)" : $"Par {hole.par}";
                scoreboard.ShowBanner($"HOLE {hole.holeNumber}: {hole.holeName}  {state}", 3.0f);
            }

            Debug.Log($"[GolfVR] Went to hole {index + 1}.");
        }

        private void Update()
        {
            // The scoreboard and hole indicator follow the player around the
            // course: whichever tee they're standing near is the current hole.
            if (Time.time >= _nextFollowCheck)
            {
                _nextFollowCheck = Time.time + 0.3f;
                FollowPlayerToNearestHole();
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(debugSinkKey))
            {
                DebugSinkCurrentHole();
            }
#endif
        }

        private void FollowPlayerToNearestHole()
        {
            if (Player.instance == null || holes == null) return;

            Vector3 p = Player.instance.feetPositionGuess;
            int best = -1;
            float bestDist = scoreboardFollowRadius;
            for (int i = 0; i < holes.Length; i++)
            {
                if (holes[i] == null || holes[i].playerTeeLocation == null) continue;
                Vector3 d = holes[i].playerTeeLocation.position - p;
                d.y = 0f;
                if (d.magnitude < bestDist)
                {
                    bestDist = d.magnitude;
                    best = i;
                }
            }

            if (best >= 0 && best != _currentHoleIndex)
            {
                SetCurrentHole(best);
            }
        }

#if UNITY_EDITOR
        [Tooltip("Editor-only: press this key in Play mode to sink the current hole for quick testing")]
        public KeyCode debugSinkKey = KeyCode.K;
#endif

        /// <summary>
        /// Testing convenience: sinks the hole the player is at -- its ball
        /// snaps into the cup and the real celebration/scoring path runs
        /// (confetti, fanfare, fireworks, scoreboard) -- without needing to
        /// actually putt it in. Right-click the "Mini Golf Game Manager"
        /// component header in the Inspector during Play mode and choose
        /// "DEBUG: Sink Current Hole", or press K.
        /// </summary>
        [ContextMenu("DEBUG: Sink Current Hole")]
        public void DebugSinkCurrentHole()
        {
            if (holes == null || _currentHoleIndex < 0 || _currentHoleIndex >= holes.Length || holes[_currentHoleIndex] == null)
            {
                Debug.LogWarning("[GolfVR] DebugSinkCurrentHole: no valid current hole to sink.");
                return;
            }

            holes[_currentHoleIndex].DebugForceSink();
        }
    }
}
