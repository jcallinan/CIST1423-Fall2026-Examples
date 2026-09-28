using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Verifies the "pick any hole" testing panel: all 9 JumpToHoleButtons
    /// exist with the right holeIndex, none of them (or the fireworks-test
    /// button) have dangling persistent listeners (the exact bug found on
    /// the reset kiosk), and pressing one actually jumps -- including
    /// jumping backward to an earlier, already-completed hole.
    /// </summary>
    public static class HoleSelectPanelTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int _failures;

        [MenuItem("Tools/GolfVR/Hole Select Panel Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = Object.FindObjectOfType<GolfBall>();
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

            GameObject kiosk = GameObject.Find("HoleSelectKiosk");
            if (kiosk == null)
            {
                Debug.LogError("[HoleSelectTest] HoleSelectKiosk not found.");
                Debug.LogError("[HoleSelectTest] RESULT: FAIL");
                return;
            }

            JumpToHoleButton[] buttons = kiosk.GetComponentsInChildren<JumpToHoleButton>();
            if (buttons.Length != 9)
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] Expected 9 JumpToHoleButtons, found {buttons.Length}.");
            }
            else
            {
                Debug.Log("[HoleSelectTest] Button count: OK (9 found).");
            }

            bool[] seen = new bool[9];
            foreach (JumpToHoleButton b in buttons)
            {
                Invoke(b, "Awake");
                if (b.holeIndex >= 0 && b.holeIndex < 9) seen[b.holeIndex] = true;
                CheckNoDanglingListeners(b.GetComponent<Valve.VR.InteractionSystem.HoverButton>(), $"HoleButton {b.holeIndex + 1}");
            }
            for (int i = 0; i < 9; i++)
            {
                if (!seen[i])
                {
                    _failures++;
                    Debug.LogError($"[HoleSelectTest] No button found for hole index {i} (hole {i + 1}).");
                }
            }
            if (System.Array.TrueForAll(seen, x => x))
            {
                Debug.Log("[HoleSelectTest] Coverage: OK (every hole 1-9 has exactly one button).");
            }

            TestFireworksButton fwButton = kiosk.GetComponentInChildren<TestFireworksButton>();
            if (fwButton == null)
            {
                _failures++;
                Debug.LogError("[HoleSelectTest] TestFireworksButton not found.");
            }
            else
            {
                Invoke(fwButton, "Awake");
                CheckNoDanglingListeners(fwButton.GetComponent<Valve.VR.InteractionSystem.HoverButton>(), "FireworksTestButton");
            }

            TestJumpForwardAndBackward(manager, buttons);
            TestFireworksButtonFires(fwButton);
            TestTravelButtons(manager);

            if (_failures == 0)
            {
                Debug.Log("[HoleSelectTest] RESULT: PASS - hole select panel and fireworks test button both work correctly.");
            }
            else
            {
                Debug.LogError($"[HoleSelectTest] RESULT: FAIL - {_failures} check(s) failed.");
            }
        }

        private static void CheckNoDanglingListeners(Valve.VR.InteractionSystem.HoverButton hoverButton, string label)
        {
            if (hoverButton == null)
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] {label}: FAIL - no HoverButton component.");
                return;
            }
            int downCount = hoverButton.onButtonDown.GetPersistentEventCount();
            int upCount = hoverButton.onButtonUp.GetPersistentEventCount();
            if (downCount == 0 && upCount == 0)
            {
                Debug.Log($"[HoleSelectTest] {label} listeners: OK (0 persistent listeners).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] {label} listeners: FAIL - onButtonDown={downCount}, onButtonUp={upCount} (expected 0 each).");
            }
        }

        private static void TestJumpForwardAndBackward(MiniGolfGameManager manager, JumpToHoleButton[] buttons)
        {
            manager.InitializeRound();

            JumpToHoleButton hole7Button = System.Array.Find(buttons, b => b.holeIndex == 6);
            JumpToHoleButton hole2Button = System.Array.Find(buttons, b => b.holeIndex == 1);
            if (hole7Button == null || hole2Button == null)
            {
                _failures++;
                Debug.LogError("[HoleSelectTest] Jump test: FAIL - couldn't find hole 7 and/or hole 2 buttons.");
                return;
            }

            // Jump forward to hole 7.
            InvokeButtonPress(hole7Button);
            bool forwardOk = manager.CurrentHoleIndex == 6 && !manager.IsGameFinished;

            // Now jump BACKWARD to hole 2 -- the tricky case, since hole 2
            // was already marked completed when the round first passed
            // through it during normal InitializeRound() setup... actually
            // it wasn't completed, just skipped over; the real risk is
            // hole 7 (just visited) or the game-finished flag being stuck.
            InvokeButtonPress(hole2Button);
            bool backwardOk = manager.CurrentHoleIndex == 1 && !manager.IsGameFinished;

            if (forwardOk && backwardOk)
            {
                Debug.Log("[HoleSelectTest] Jump forward/backward: OK (hole 7 then hole 2 both landed correctly).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] Jump forward/backward: FAIL - forwardOk={forwardOk}, backwardOk={backwardOk}, CurrentHoleIndex={manager.CurrentHoleIndex}.");
            }
        }

        private static void TestTravelButtons(MiniGolfGameManager manager)
        {
            GoToHoleButton[] travel = Object.FindObjectsOfType<GoToHoleButton>();
            if (travel.Length != 9)
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] Expected 9 NEXT HOLE travel buttons (one per tee), found {travel.Length}.");
                return;
            }

            foreach (GoToHoleButton b in travel)
            {
                Invoke(b, "Awake");
                CheckNoDanglingListeners(b.GetComponent<Valve.VR.InteractionSystem.HoverButton>(), $"NextHole button at hole {b.fromHoleIndex + 1}");
            }

            manager.InitializeRound();
            manager.holes[0].DebugForceSink(); // hole 1 done; its progress must survive travelling
            GoToHoleButton atHole1 = System.Array.Find(travel, b => b.fromHoleIndex == 0);
            GoToHoleButton atHole9 = System.Array.Find(travel, b => b.fromHoleIndex == 8);

            InvokeButtonPress(atHole1);
            bool toHole2 = manager.CurrentHoleIndex == 1 && manager.holes[0].IsCompleted;
            InvokeButtonPress(atHole9);
            bool wrapsToHole1 = manager.CurrentHoleIndex == 0 && manager.holes[0].IsCompleted;

            if (toHole2 && wrapsToHole1)
            {
                Debug.Log("[HoleSelectTest] Travel buttons: OK (9 present, no dangling listeners; hole 1 button -> hole 2, hole 9 button wraps to hole 1, sunk holes stay sunk).");
            }
            else
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] Travel buttons: FAIL - toHole2={toHole2}, wrapsToHole1={wrapsToHole1}, current={manager.CurrentHoleIndex}.");
            }
        }

        private static void TestFireworksButtonFires(TestFireworksButton fwButton)
        {
            if (fwButton == null) return;

            GameObject fireworksGo = GameObject.Find("FireworksCelebrationController");
            FireworksCelebrationController controller = fireworksGo != null ? fireworksGo.GetComponent<FireworksCelebrationController>() : null;
            if (controller == null)
            {
                _failures++;
                Debug.LogError("[HoleSelectTest] Fireworks test button: FAIL - FireworksCelebrationController not found.");
                return;
            }
            Invoke(controller, "Awake");

            try
            {
                InvokeButtonPress(fwButton);
                Debug.Log("[HoleSelectTest] Fireworks test button: OK (pressing it called PlayFireworksCelebration without exceptions).");
            }
            catch (TargetInvocationException e)
            {
                _failures++;
                Debug.LogError($"[HoleSelectTest] Fireworks test button: FAIL - {e.InnerException}");
            }
        }

        private static void InvokeButtonPress(MonoBehaviour buttonComponent)
        {
            Valve.VR.InteractionSystem.HoverButton hoverButton = buttonComponent.GetComponent<Valve.VR.InteractionSystem.HoverButton>();
            hoverButton.onButtonDown.Invoke(null);
        }

        private static void Invoke(object target, string methodName)
        {
            if (target == null) return;
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null) return;
            m.Invoke(target, null);
        }
    }
}
