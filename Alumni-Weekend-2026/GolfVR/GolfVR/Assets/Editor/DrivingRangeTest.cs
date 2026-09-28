using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Headless test of the driving range: swings the driver (by feeding its
    /// sweep-hit method synthetic club-head positions at several speeds), lets
    /// the launched ball fly under real physics (Physics.Simulate), and checks
    /// carry/total distance, apex, trail, the history, and the "fresh ball on
    /// the tee" loop. Distances are printed so the aerodynamics can be tuned.
    /// </summary>
    public static class DrivingRangeTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string ScenePath = "Assets/Scenes/ICARUS_DrivingRange_v1.unity";
        private static int _failures;

        [MenuItem("Tools/GolfVR/Driving Range Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            DrivingRangeManager mgr = Object.FindObjectOfType<DrivingRangeManager>();
            DriverClub club = Object.FindObjectOfType<DriverClub>();
            Invoke(mgr, "Awake");
            Invoke(mgr, "Start");

            SimulationMode previous = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();

                float[] clubSpeeds = { 8f, 15f, 22f, 30f, 40f };
                float lastTotal = 0f;
                bool monotonic = true;

                foreach (float speed in clubSpeeds)
                {
                    RangeBall ball = ReadyBall(mgr);
                    if (ball == null) { Fail("no ready ball on the tee before the swing at " + speed + " m/s"); continue; }

                    // Swing: head travels along +Z, slightly rising, through the ball.
                    Vector3 dir = new Vector3(0f, 0.05f, 1f).normalized;
                    Vector3 start = ball.transform.position - dir * 1.2f;
                    float dt = 1f / 90f;
                    RangeBall hit = null;
                    Vector3 prev = start;
                    for (int i = 0; i < 400 && hit == null; i++)
                    {
                        Vector3 cur = start + dir * (speed * dt * (i + 1));
                        Physics.SyncTransforms();
                        hit = club.TrySweepHit(prev, cur, dt);
                        prev = cur;
                    }

                    if (hit == null) { Fail($"swing at {speed} m/s never hit the ball"); continue; }
                    if (hit != ball) Fail("hit a different ball than the one on the tee");

                    // A ball that has just been hit must not be hittable again.
                    RangeBall again = club.TrySweepHit(prev, prev + dir * 0.05f, dt);
                    if (again != null) Fail("the same ball was hit twice");

                    // Fly it out.
                    for (int step = 0; step < 4000 && ball.state != RangeBall.State.Done; step++)
                    {
                        ball.PhysicsTick(0.02f);
                        Physics.Simulate(0.02f);
                    }

                    Debug.Log($"[RangeTest] club {speed:F0} m/s ({speed * 2.237f:F0} mph): ball {ball.launchSpeed:F1} m/s @ {ball.launchAngle:F1} deg -> carry {ball.carryDistance:F0} m ({ball.carryDistance * 1.0936f:F0} yd), total {ball.totalDistance:F0} m ({ball.totalDistance * 1.0936f:F0} yd), apex {ball.apexHeight:F1} m, air time {ball.flightTime:F1} s, state={ball.state}");

                    if (ball.state != RangeBall.State.Done) Fail($"ball never came to rest at {speed} m/s (state {ball.state})");
                    if (ball.totalDistance < ball.carryDistance - 0.01f) Fail("total distance is shorter than carry");
                    if (ball.totalDistance <= lastTotal) monotonic = false;
                    lastTotal = ball.totalDistance;
                    if (Mathf.Abs(ball.restPosition.x) > ball.totalDistance * 0.1f + 0.5f) Fail("ball drifted sideways on a straight swing");
                    if (ball.Trail == null) Fail("ball has no trail renderer");

                    // The manager should now tee up a fresh ball.
                    mgr.SpawnNext();
                }

                if (!monotonic) Fail("harder swings should always go farther");

                // Reasonable real-world ballpark for the middle swing (22 m/s = ~50 mph club head).
                // Checked loosely: this is a tuning aid as much as a test.
                RangeBall mid = null;
                foreach (RangeBall b in mgr.FinishedShots) { if (Mathf.Abs(b.launchSpeed - 22f * 1.45f * 1.3f) < 3f) mid = b; }
                if (mid != null && (mid.carryDistance < 40f || mid.carryDistance > 140f))
                    Fail($"22 m/s swing carried {mid.carryDistance:F0} m, outside the plausible 40-140 m");

                if (mgr.FinishedShots.Count != clubSpeeds.Length) Fail($"history has {mgr.FinishedShots.Count} shots, expected {clubSpeeds.Length}");
                if (mgr.historyText == null || !mgr.historyText.text.Contains("BEST")) Fail("history board did not show a BEST line");

                RangeBall ready = ReadyBall(mgr);
                if (ready == null) Fail("no fresh ball waiting on the tee after the last shot");

                // Clear wipes everything and tees up one ball.
                mgr.ClearAll();
                if (mgr.FinishedShots.Count != 0 || ReadyBall(mgr) == null) Fail("ClearAll did not reset the range");
                int balls = 0;
                foreach (RangeBall b in Object.FindObjectsOfType<RangeBall>()) balls++;
                if (balls != 1) Fail($"expected exactly 1 ball after ClearAll, found {balls}");
            }
            finally
            {
                Physics.simulationMode = previous;
            }

            if (_failures == 0) Debug.Log("[RangeTest] RESULT: PASS - swings launch, balls fly/land/roll/stop, distances recorded, fresh ball teed up, clear works.");
            else Debug.LogError($"[RangeTest] RESULT: FAIL - {_failures} check(s) failed.");
        }

        private static RangeBall ReadyBall(DrivingRangeManager mgr)
        {
            foreach (RangeBall b in Object.FindObjectsOfType<RangeBall>())
            {
                if (b.state == RangeBall.State.Ready) return b;
            }
            return null;
        }

        private static void Fail(string message)
        {
            _failures++;
            Debug.LogError("[RangeTest] FAIL: " + message);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo m = target.GetType().GetMethod(name, PrivateInstance);
            if (m != null) m.Invoke(target, null);
        }
    }
}
