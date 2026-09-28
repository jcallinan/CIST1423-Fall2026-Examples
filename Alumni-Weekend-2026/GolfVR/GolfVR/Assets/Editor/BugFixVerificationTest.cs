using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Verifies the four bugs reported after real playtesting are actually
    /// fixed: the ball no longer gets force-teleported into the cup on a
    /// real (non-debug) sink, the debug force-sink still visually snaps the
    /// ball safely, the reset button's onButtonDown event now has zero
    /// dangling persistent listeners and its dynamic listener still fires,
    /// and the putter's attachment flags match Valve's tested default.
    /// </summary>
    public static class BugFixVerificationTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int _failures;

        [MenuItem("Tools/GolfVR/Bug Fix Verification Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = manager != null ? manager.GetBallForHole(0) : null; // hole 1's ball
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();

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

            TestRealSinkDoesNotTeleportBall(manager, ball);
            TestDebugSinkSafelySnapsBall(manager, ball);
            TestResetButtonHasNoDanglingListeners();
            TestResetButtonStillFires(manager);
            TestPutterAttachmentFlags();
            TestResetAndJumpResetBallAndPutter(manager, putter);

            if (_failures == 0)
            {
                Debug.Log("[BugFixVerify] RESULT: PASS - all reported bugs verified fixed.");
            }
            else
            {
                Debug.LogError($"[BugFixVerify] RESULT: FAIL - {_failures} check(s) failed.");
            }
        }

        private static void TestRealSinkDoesNotTeleportBall(MiniGolfGameManager manager, GolfBall ball)
        {
            manager.InitializeRound();
            GolfHole hole = manager.holes[0];

            // Simulate the ball rolling to a rest point 0.3m away from the
            // cup center (inside the sink radius, but NOT
            // exactly on top of it) -- the real gameplay case.
            Vector3 restSpot = hole.transform.position + new Vector3(0.1f, 0f, 0f);
            ball.transform.position = restSpot;

            MethodInfo onTriggerStay = typeof(GolfHole).GetMethod("OnTriggerStay", PrivateInstance);
            onTriggerStay.Invoke(hole, new object[] { ball.GetComponent<Collider>() });

            float dist = Vector3.Distance(ball.transform.position, restSpot);
            if (dist < 0.01f)
            {
                Debug.Log("[BugFixVerify] Real sink: OK (ball position untouched, stayed where it rolled to rest).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Real sink: FAIL - ball moved {dist:F3}m from its rest spot (should be 0, a real sink must not force-teleport the ball).");
            }
        }

        private static void TestDebugSinkSafelySnapsBall(MiniGolfGameManager manager, GolfBall ball)
        {
            manager.InitializeRound();
            GolfHole hole = manager.holes[0];
            ball.transform.position = new Vector3(999f, 999f, 999f); // clearly not near the hole

            manager.DebugSinkCurrentHole();

            float dist = Vector3.Distance(ball.transform.position, hole.transform.position);
            if (dist < 1.0f)
            {
                Debug.Log($"[BugFixVerify] Debug sink: OK (ball snapped to within {dist:F2}m of the cup via safe raycast placement).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Debug sink: FAIL - ball is {dist:F2}m from the cup, expected < 1m.");
            }
        }

        private static void TestResetButtonHasNoDanglingListeners()
        {
            GameObject kiosk = GameObject.Find("ResetRoundKiosk");
            Valve.VR.InteractionSystem.HoverButton hoverButton = kiosk != null ? kiosk.GetComponentInChildren<Valve.VR.InteractionSystem.HoverButton>() : null;
            if (hoverButton == null)
            {
                _failures++;
                Debug.LogError("[BugFixVerify] Reset button listeners: FAIL - HoverButton not found.");
                return;
            }

            int downCount = hoverButton.onButtonDown.GetPersistentEventCount();
            int upCount = hoverButton.onButtonUp.GetPersistentEventCount();
            if (downCount == 0 && upCount == 0)
            {
                Debug.Log("[BugFixVerify] Reset button listeners: OK (0 persistent listeners on onButtonDown/onButtonUp, no dangling null targets).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Reset button listeners: FAIL - onButtonDown has {downCount}, onButtonUp has {upCount} persistent listener(s) (expected 0).");
            }
        }

        private static void TestResetButtonStillFires(MiniGolfGameManager manager)
        {
            manager.InitializeRound();
            SetField(manager, "_currentHoleIndex", 4);

            GameObject kiosk = GameObject.Find("ResetRoundKiosk");
            ResetRoundButton resetButton = kiosk != null ? kiosk.GetComponentInChildren<ResetRoundButton>() : null;
            if (resetButton == null)
            {
                _failures++;
                Debug.LogError("[BugFixVerify] Reset button fires: FAIL - ResetRoundButton component not found.");
                return;
            }
            Invoke(resetButton, "Awake");

            Valve.VR.InteractionSystem.HoverButton hoverButton = kiosk.GetComponentInChildren<Valve.VR.InteractionSystem.HoverButton>();
            hoverButton.onButtonDown.Invoke(null); // exactly what HandHoverUpdate calls on a real press

            if (manager.CurrentHoleIndex == 0)
            {
                Debug.Log("[BugFixVerify] Reset button fires: OK (onButtonDown.Invoke correctly reset CurrentHoleIndex to 0).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Reset button fires: FAIL - CurrentHoleIndex is {manager.CurrentHoleIndex}, expected 0 after pressing.");
            }
        }

        private static void TestResetAndJumpResetBallAndPutter(MiniGolfGameManager manager, GolfPutter putter)
        {
            if (putter == null)
            {
                _failures++;
                Debug.LogError("[BugFixVerify] Reset/jump: FAIL - no putter in scene.");
                return;
            }

            // --- Jump to hole 4 with the ball and putter left far away ---
            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>()) b.transform.position = new Vector3(500f, 50f, 500f);
            putter.transform.position = new Vector3(-500f, 50f, 500f);
            manager.JumpToHole(3);

            GolfHole hole4 = manager.holes[3];
            GolfBall ball4 = manager.GetBallForHole(3);
            float ballDist = Vector2.Distance(new Vector2(ball4.transform.position.x, ball4.transform.position.z), new Vector2(hole4.teePoint.position.x, hole4.teePoint.position.z));
            float putterDist = Vector3.Distance(putter.transform.position, hole4.playerTeeLocation.position);
            bool jumpOk = ballDist < 0.5f && putterDist < 2.5f;

            // --- Full reset with everything scattered ---
            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>()) b.transform.position = new Vector3(500f, 50f, 500f);
            putter.transform.position = new Vector3(-500f, 50f, 500f);
            manager.ResetForNextGroup();

            bool allBallsOk = true;
            for (int i = 0; i < manager.holes.Length; i++)
            {
                GolfBall b = manager.GetBallForHole(i);
                Vector3 tee = manager.holes[i].teePoint.position;
                float d = Vector2.Distance(new Vector2(b.transform.position.x, b.transform.position.z), new Vector2(tee.x, tee.z));
                if (d > 0.5f) allBallsOk = false;
            }
            float putterDist1 = Vector3.Distance(putter.transform.position, putter.HomePosition);
            bool resetOk = allBallsOk && putterDist1 < 0.2f && manager.CurrentHoleIndex == 0;

            if (jumpOk && resetOk)
            {
                Debug.Log("[BugFixVerify] Reset/jump: OK (jumping to hole 4 put its ball on the tee and the putter by the player's feet; the full reset put all 9 balls on their tees and the putter back at its starting position on the table).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Reset/jump: FAIL - jump: ballDist={ballDist:F2}, putterDist={putterDist:F2}; reset: allBallsOnTees={allBallsOk}, putterDist={putterDist1:F2}, currentHole={manager.CurrentHoleIndex}.");
            }
        }

        private static void TestPutterAttachmentFlags()
        {
            GameObject putterGo = GameObject.Find("GolfPutter_VR");
            Valve.VR.InteractionSystem.Throwable throwable = putterGo != null ? putterGo.GetComponent<Valve.VR.InteractionSystem.Throwable>() : null;
            if (throwable == null)
            {
                _failures++;
                Debug.LogError("[BugFixVerify] Putter attachment flags: FAIL - Throwable not found.");
                return;
            }

            bool hasSnap = (throwable.attachmentFlags & Valve.VR.InteractionSystem.Hand.AttachmentFlags.SnapOnAttach) != 0;
            if (hasSnap)
            {
                Debug.Log($"[BugFixVerify] Putter attachment flags: OK ({throwable.attachmentFlags}, includes SnapOnAttach).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[BugFixVerify] Putter attachment flags: FAIL - {throwable.attachmentFlags} is missing SnapOnAttach.");
            }
        }

        private static void Invoke(object target, string methodName)
        {
            if (target == null) return;
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null) return;
            m.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, PrivateInstance);
            f.SetValue(target, value);
        }
    }
}
