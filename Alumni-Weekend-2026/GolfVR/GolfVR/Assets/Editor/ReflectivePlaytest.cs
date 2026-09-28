using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Drives a full 9-hole round entirely in Edit mode (no Play mode, no
    /// Test Framework needed) by reflection-invoking the same private
    /// lifecycle methods Unity would normally call, and puppeting the ball
    /// into each cup via the real GolfHole.OnTriggerStay code path. Manually
    /// pumps MiniGolfGameManager's private hole-transition coroutine to
    /// completion synchronously, since Edit mode has no frame scheduler to
    /// advance a real coroutine's WaitForSeconds.
    /// </summary>
    public static class ReflectivePlaytest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tools/GolfVR/Run Reflective Full-Course Playtest")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = Object.FindObjectOfType<GolfBall>();
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();
            if (manager == null || ball == null)
            {
                Debug.LogError("[Playtest] Could not find MiniGolfGameManager or GolfBall in the scene.");
                return;
            }

            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>())
            {
                Invoke(b, "Awake");
                Invoke(b, "Start");
            }

            if (putter != null)
            {
                Invoke(putter, "Awake");
            }

            foreach (GolfHole h in manager.holes)
            {
                if (h != null) Invoke(h, "Awake");
            }

            Invoke(manager, "Awake");
            Invoke(manager, "Start"); // -> InitializeRound() -> SetupHole(0, repositionPutter: false)

            for (int i = 0; i < manager.holes.Length; i++)
            {
                GolfHole hole = manager.holes[i];
                if (hole == null)
                {
                    Debug.LogError($"[Playtest] FAIL: holes[{i}] is null.");
                    return;
                }

                // Every hole has its own ball; drop it into its own cup.
                GolfBall holeBall = manager.GetBallForHole(i);
                if (holeBall == null)
                {
                    Debug.LogError($"[Playtest] FAIL: hole {i + 1} has no ball.");
                    return;
                }
                Collider ballCollider = holeBall.GetComponent<Collider>();
                holeBall.transform.position = hole.transform.position;
                InvokeWithArgs(hole, "OnTriggerStay", new object[] { ballCollider });

                bool completed = (bool)GetProp(hole, "IsCompleted");
                if (!completed)
                {
                    Debug.LogError($"[Playtest] FAIL: hole {i + 1} (par {hole.par}) did not register as sunk.");
                    return;
                }

                Debug.Log($"[Playtest] Hole {i + 1} (par {hole.par}) sunk OK.");

            }

            bool finished = (bool)GetProp(manager, "IsGameFinished");
            if (finished)
            {
                Debug.Log("[Playtest] RESULT: PASS - all 9 holes sunk and game reported finished.");
            }
            else
            {
                Debug.LogError("[Playtest] RESULT: FAIL - game did not report finished after all 9 holes.");
            }
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null)
            {
                Debug.LogWarning($"[Playtest] Method {methodName} not found on {target.GetType().Name}");
                return;
            }
            m.Invoke(target, null);
        }

        private static void InvokeWithArgs(object target, string methodName, object[] args)
        {
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            m.Invoke(target, args);
        }

        private static object GetProp(object target, string propName)
        {
            PropertyInfo p = target.GetType().GetProperty(propName);
            return p.GetValue(target);
        }
    }
}
