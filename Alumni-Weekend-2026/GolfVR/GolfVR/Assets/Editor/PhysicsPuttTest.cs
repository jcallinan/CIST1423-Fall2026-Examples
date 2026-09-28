using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Real-physics putt test. Unlike ReflectivePlaytest (which teleports the
    /// ball straight onto the cup and calls OnTriggerStay by hand), this rolls
    /// the ball from the tee toward the cup at various speeds using
    /// Physics.Simulate, and replays Unity's trigger Enter/Stay callbacks
    /// (Stay only while the rigidbody is awake, exactly like the engine) so
    /// real cup geometry, ball damping, sleeping and the sink rules are all
    /// exercised. Batch mode can't enter Play mode, so this is the closest
    /// headless approximation of an in-headset putt.
    /// </summary>
    public static class PhysicsPuttTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float Step = 0.02f;

        [MenuItem("Tools/GolfVR/Physics Putt Test")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(GetArg("-testScene") ?? "Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);
            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = Object.FindObjectOfType<GolfBall>();
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();

            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>())
            {
                Invoke(b, "Awake");
                Invoke(b, "Start");
            }
            if (putter != null) Invoke(putter, "Awake");
            foreach (GolfHole h in manager.holes) Invoke(h, "Awake");
            Invoke(manager, "Awake");
            Invoke(manager, "Start");

            string dragArg = GetArg("-testDrag"); string angArg = GetArg("-testAngDrag");
            Rigidbody ballBody = ball.GetComponent<Rigidbody>();
            foreach (GolfBall each in Object.FindObjectsOfType<GolfBall>())
            {
                Rigidbody eachBody = each.GetComponent<Rigidbody>();
                if (dragArg != null) eachBody.drag = float.Parse(dragArg, System.Globalization.CultureInfo.InvariantCulture);
                if (angArg != null) eachBody.angularDrag = float.Parse(angArg, System.Globalization.CultureInfo.InvariantCulture);
            }
            Debug.Log($"[PhysTest] ball drag={ballBody.drag} angularDrag={ballBody.angularDrag} mass={ballBody.mass}");

            // Default: the six holes with a clear straight line from tee to cup
            // (3, 5 and 8 have obstacles in the way and are exercised with -testHoles).
            bool defaultRun = GetArg("-testHoles") == null;
            int[] holeIdx = ParseIntList("-testHoles", new[] { 0, 1, 3, 5, 6, 8 });
            float[] speeds = ParseFloatList("-testSpeeds", new[] { 3.0f, 5.0f });

            int sunk = 0, total = 0;
            SimulationMode previousMode = Physics.simulationMode;
            try
            {
            foreach (int hi in holeIdx)
            {
                foreach (float speed in speeds)
                {
                    total++;
                    if (RollAtHole(manager, ball, hi, speed)) sunk++;
                }
            }
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }
            Debug.Log($"[PhysTest] SUMMARY: {sunk}/{total} putts registered as sunk.");
            if (defaultRun)
            {
                if (sunk == total) Debug.Log("[PhysTest] RESULT: PASS - every clear-line putt at 3 and 5 m/s rolled into its own cup and registered.");
                else Debug.LogError($"[PhysTest] RESULT: FAIL - only {sunk}/{total} clear-line putts sank.");
            }
        }

        private static bool RollAtHole(MiniGolfGameManager manager, GolfBall ball, int holeIndex, float speed)
        {
            GolfHole hole = manager.holes[holeIndex];
            ball = manager.GetBallForHole(holeIndex);
            manager.JumpToHole(holeIndex);
            GolfPutter parkedPutter = Object.FindObjectOfType<GolfPutter>();
            if (parkedPutter != null) { parkedPutter.transform.position = new Vector3(0f, 30f, 0f); parkedPutter.GetComponent<Rigidbody>().isKinematic = true; }
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            Collider ballCol = ball.GetComponent<Collider>();
            float ballRadius = ball.GetComponent<SphereCollider>().radius * ball.transform.lossyScale.x;

            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) { Physics.Simulate(Step); }   // let it settle on the tee

            Vector3 toHole = hole.transform.position - ball.transform.position;
            toHole.y = 0f;
            Vector3 dir = toHole.normalized;
            string angArg = GetArg("-testAngle");
            if (angArg != null) dir = Quaternion.Euler(0f, float.Parse(angArg, System.Globalization.CultureInfo.InvariantCulture), 0f) * dir;
            Vector3 start = ball.transform.position;
            ball.ReceivePutt(dir * speed);

            bool wasInside = false;
            float minDist = float.MaxValue;
            float sinkTime = -1f;
            StringBuilder trace = new StringBuilder();
            float t = 0f;
            for (int i = 0; i < 700 && !hole.IsCompleted; i++)
            {
                Physics.Simulate(Step);
                t += Step;
                Invoke(ball, "FixedUpdate");
                Invoke(hole, "FixedUpdate");

                Vector3 flat = ball.transform.position - hole.transform.position;
                flat.y = 0f;
                minDist = Mathf.Min(minDist, flat.magnitude);

                bool inside = false;
                foreach (Collider c in Physics.OverlapSphere(ball.transform.position, ballRadius, ~0, QueryTriggerInteraction.Collide))
                {
                    if (c == hole.GetComponent<Collider>()) { inside = true; break; }
                }

                if (inside && !wasInside) InvokeWithArgs(hole, "OnTriggerEnter", ballCol);
                if (inside && !rb.IsSleeping()) InvokeWithArgs(hole, "OnTriggerStay", ballCol);
                wasInside = inside;

                if (i % 25 == 0)
                {
                    trace.Append($"t={t:F1}s d={flat.magnitude:F2} y={ball.transform.position.y:F2} v={rb.velocity.magnitude:F2}{(inside ? " IN-TRIGGER" : "")} | ");
                }
                if (hole.IsCompleted) sinkTime = t;
            }

            Debug.Log($"[PhysTest] Hole {holeIndex + 1} speed {speed:F1} m/s: sunk={hole.IsCompleted}" +
                      (hole.IsCompleted ? $" at t={sinkTime:F2}s" : "") +
                      $", closest approach {minDist:F2} m, final pos {ball.transform.position.ToString("F2")}, start {start.ToString("F2")}");
            Debug.Log($"[PhysTest]   trace: {trace}");

            if (hole.IsCompleted)
            {
                Debug.Log($"[PhysTest]   scoreboard state: hole {manager.CurrentHoleNumber} current, finished={manager.IsGameFinished}");
            }
            return hole.IsCompleted;
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo m = target.GetType().GetMethod(name, PrivateInstance);
            if (m != null) m.Invoke(target, null);
        }

        private static void InvokeWithArgs(object target, string name, object arg)
        {
            MethodInfo m = target.GetType().GetMethod(name, PrivateInstance);
            if (m != null) m.Invoke(target, new[] { arg });
        }

        private static int[] ParseIntList(string flag, int[] fallback)
        {
            string s = GetArg(flag);
            if (s == null) return fallback;
            List<int> l = new List<int>();
            foreach (string p in s.Split(',')) l.Add(int.Parse(p));
            return l.ToArray();
        }

        private static float[] ParseFloatList(string flag, float[] fallback)
        {
            string s = GetArg(flag);
            if (s == null) return fallback;
            List<float> l = new List<float>();
            foreach (string p in s.Split(',')) l.Add(float.Parse(p, System.Globalization.CultureInfo.InvariantCulture));
            return l.ToArray();
        }

        private static string GetArg(string flag)
        {
            string[] a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == flag) return a[i + 1];
            return null;
        }
    }
}
