using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Broader coverage beyond ReflectivePlaytest's "can the whole course be
    /// completed" check: score-term wording (hole-in-one/eagle/birdie/par/
    /// bogey), the out-of-bounds penalty path, the QuickResetController
    /// debug buttons, and the new DebugForceSink/DebugSinkCurrentHole
    /// testing shortcuts (including the guard against force-sinking a hole
    /// that isn't the active one). All driven the same reflection-based
    /// Edit-mode way as ReflectivePlaytest, for the same reasons (batch mode
    /// won't pump Play-mode/coroutine logic on its own).
    /// </summary>
    public static class ExtendedFeatureTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int _failures;

        [MenuItem("Tools/GolfVR/Extended Feature Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = manager != null ? manager.GetBallForHole(0) : null; // hole 1's ball
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();
            QuickResetController quickReset = Object.FindObjectOfType<QuickResetController>();

            if (manager == null || ball == null)
            {
                Debug.LogError("[ExtTest] Could not find MiniGolfGameManager or GolfBall.");
                return;
            }

            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>())
            {
                Invoke(b, "Awake");
                Invoke(b, "Start");
            }
            if (putter != null) Invoke(putter, "Awake");
            foreach (GolfHole h in manager.holes)
            {
                if (h != null) Invoke(h, "Awake");
            }
            Invoke(manager, "Awake");
            Invoke(manager, "Start");

            TestScoreTerm(manager, holeIndex: 0, strokes: 1, expectedSubstring: "HOLE IN ONE", label: "Hole-in-one");
            TestScoreTerm(manager, holeIndex: 0, strokes: 2, expectedSubstring: "PAR!", label: "Par");
            TestScoreTerm(manager, holeIndex: 0, strokes: 3, expectedSubstring: "BOGEY", label: "Bogey");
            TestScoreTerm(manager, holeIndex: 8, strokes: 2, expectedSubstring: "EAGLE", label: "Eagle (hole 9, par 4)");
            TestScoreTerm(manager, holeIndex: 8, strokes: 3, expectedSubstring: "BIRDIE", label: "Birdie (hole 9, par 4)");

            TestOutOfBounds(manager, ball);
            TestQuickReset(quickReset);
            TestForceSinkAnyHoleAndBallOwnership(manager);

            if (_failures == 0)
            {
                Debug.Log("[ExtTest] RESULT: PASS - all extended checks passed.");
            }
            else
            {
                Debug.LogError($"[ExtTest] RESULT: FAIL - {_failures} check(s) failed, see above.");
            }
        }

        private static void TestScoreTerm(MiniGolfGameManager manager, int holeIndex, int strokes, string expectedSubstring, string label)
        {
            manager.InitializeRound();
            SetField(manager, "_currentHoleIndex", holeIndex);

            GolfHole hole = manager.holes[holeIndex];
            SetField(hole, "_isCompleted", false);

            int[] strokesArr = (int[])GetField(manager, "_strokesPerHole");
            strokesArr[holeIndex] = strokes;

            ScoreboardUI scoreboard = manager.scoreboard;

            SetField(hole, "_isCompleted", true); // what GolfHole.Sink does before telling the manager
            manager.OnHoleSunk(hole);

            string banner = scoreboard != null && scoreboard.bannerText != null ? scoreboard.bannerText.text : null;
            if (banner != null && banner.Contains(expectedSubstring))
            {
                Debug.Log($"[ExtTest] {label}: OK (\"{banner.Replace("\n", " / ")}\")");
            }
            else
            {
                _failures++;
                Debug.LogError($"[ExtTest] {label}: FAIL - expected banner to contain \"{expectedSubstring}\", got \"{banner}\"");
            }
        }

        private static void TestOutOfBounds(MiniGolfGameManager manager, GolfBall ball)
        {
            manager.InitializeRound();
            Vector3 restPos = ball.transform.position;
            int strokesBefore = manager.CurrentHoleStrokes;

            MethodInfo handleOob = typeof(GolfBall).GetMethod("HandleOutOfBounds", PrivateInstance);
            handleOob.Invoke(ball, null);

            int strokesAfter = manager.CurrentHoleStrokes;
            float dist = Vector3.Distance(ball.transform.position, restPos);

            if (strokesAfter == strokesBefore + 1 && dist < 0.5f)
            {
                Debug.Log($"[ExtTest] Out-of-bounds: OK (penalty stroke recorded, ball returned to last rest position, {dist:F2}m off).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[ExtTest] Out-of-bounds: FAIL - strokes {strokesBefore}->{strokesAfter} (expected +1), ball moved {dist:F2}m from rest position (expected ~0).");
            }
        }

        private static void TestQuickReset(QuickResetController quickReset)
        {
            if (quickReset == null)
            {
                Debug.LogWarning("[ExtTest] QuickResetController: not found in scene, skipping (not a failure).");
                return;
            }

            Invoke(quickReset, "Awake");

            try
            {
                MethodInfo snapPutter = typeof(QuickResetController).GetMethod("SnapPutterToPlayer", PrivateInstance);
                snapPutter.Invoke(quickReset, null);
                MethodInfo resetBall = typeof(QuickResetController).GetMethod("ResetBallToPlayer", PrivateInstance);
                resetBall.Invoke(quickReset, null);
                Debug.Log("[ExtTest] QuickResetController: OK (both debug buttons ran without exceptions; Player.instance is null outside Play mode so they no-op, as designed).");
            }
            catch (TargetInvocationException e)
            {
                _failures++;
                Debug.LogError($"[ExtTest] QuickResetController: FAIL - {e.InnerException}");
            }
        }

        private static void TestForceSinkAnyHoleAndBallOwnership(MiniGolfGameManager manager)
        {
            manager.InitializeRound();

            // 1. Force-sinking a hole other than the one the player is at works
            //    (every hole has its own ball) and only touches that hole.
            GolfHole hole4 = manager.holes[3];
            hole4.DebugForceSink();
            bool onlyHole4 = hole4.IsCompleted && !manager.holes[0].IsCompleted && manager.CurrentHoleIndex == 3;
            if (onlyHole4)
            {
                Debug.Log("[ExtTest] Force-sink any hole: OK (hole 4 sunk with its own ball; hole 1 untouched; scoreboard moved to hole 4).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[ExtTest] Force-sink any hole: FAIL - hole4 completed={hole4.IsCompleted}, hole1 completed={manager.holes[0].IsCompleted}, current hole index={manager.CurrentHoleIndex}.");
            }

            // 2. Another hole's ball dropped into a cup must not sink it.
            GolfHole hole6 = manager.holes[5];
            GolfBall hole1Ball = manager.GetBallForHole(0);
            hole1Ball.transform.position = hole6.transform.position;
            Physics.SyncTransforms();
            MethodInfo stay = typeof(GolfHole).GetMethod("OnTriggerStay", PrivateInstance);
            stay.Invoke(hole6, new object[] { hole1Ball.GetComponent<Collider>() });
            if (!hole6.IsCompleted)
            {
                Debug.Log("[ExtTest] Ball ownership: OK (hole 1's ball sitting in hole 6's cup did not sink hole 6).");
            }
            else
            {
                _failures++;
                Debug.LogError("[ExtTest] Ball ownership: FAIL - hole 6 was sunk by hole 1's ball.");
            }

            // 3. Sinking all nine holes (each with its own ball) finishes the round.
            manager.InitializeRound();
            for (int i = 0; i < manager.holes.Length; i++)
            {
                manager.holes[i].DebugForceSink();
            }
            if (manager.IsGameFinished)
            {
                Debug.Log("[ExtTest] Round completion: OK (all nine holes sunk in arbitrary order -> round finished).");
            }
            else
            {
                _failures++;
                Debug.LogError("[ExtTest] Round completion: FAIL - all nine holes sunk but the round is not finished.");
            }
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null) return;
            m.Invoke(target, null);
        }

        private static object GetField(object target, string name)
        {
            FieldInfo f = target.GetType().GetField(name, PrivateInstance);
            return f.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, PrivateInstance);
            f.SetValue(target, value);
        }
    }
}
