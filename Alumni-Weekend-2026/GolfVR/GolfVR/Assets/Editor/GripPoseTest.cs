using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Verifies where the putter ends up relative to a hand after the
    /// SteamVR SnapOnAttach logic runs. The attach maths is copied from
    /// Hand.AttachObject (attachmentOffset branch) and applied to a stand-in
    /// hand pose, so this checks the same geometry a real controller gets:
    ///  - the Grip point (top of the shaft) lands exactly on the hand,
    ///  - the club head hangs below it at the full shaft length,
    ///  - a forward-lean preset swings the head out in FRONT of the hand
    ///    (+Z in hand space) by exactly that angle,
    ///  - none of that depends on how the putter happened to be lying when
    ///    it was grabbed.
    /// Also checks the hand model is not dragged to the putter pivot
    /// (Interactable.handFollowTransform) and that the grip is sticky.
    /// </summary>
    public static class GripPoseTest
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int _failures;

        [MenuItem("Tools/GolfVR/Grip Pose Test")]
        public static void Run()
        {
            _failures = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();
            putter.GetType().GetMethod("Awake", PrivateInstance).Invoke(putter, null);

            Transform grip = putter.gripPoint;
            Transform head = putter.clubHead;
            float shaftLength = Vector3.Distance(grip.position, head.position);
            Debug.Log($"[GripPose] shaft length (grip to head) = {shaftLength:F3} m");
            if (shaftLength < 0.8f || shaftLength > 1.0f)
            {
                Fail($"shaft length {shaftLength:F3} m is outside the 0.8-1.0 m putter range");
            }

            Quaternion[] lyingPoses = { Quaternion.Euler(0f, 0f, 90f), Quaternion.identity, Quaternion.Euler(30f, 200f, 75f) };
            Quaternion[] handPoses = { Quaternion.identity, Quaternion.Euler(20f, 135f, 0f), Quaternion.Euler(-35f, -60f, 10f) };
            Vector3 handPos = new Vector3(3f, 1.1f, -2f);

            foreach (float pitch in putter.gripPitchPresets)
            {
                putter.gripPitchDegrees = pitch;
                putter.ApplyGripPose();

                foreach (Quaternion lying in lyingPoses)
                {
                    foreach (Quaternion handRot in handPoses)
                    {
                        putter.transform.SetPositionAndRotation(new Vector3(-11f, 1f, -9f), lying);

                        // --- Hand.AttachObject, attachmentOffset != null branch ---
                        Quaternion rotDiff = Quaternion.Inverse(grip.rotation) * putter.transform.rotation;
                        putter.transform.rotation = handRot * rotDiff;
                        Vector3 posDiff = putter.transform.position - grip.position;
                        putter.transform.position = handPos + posDiff;
                        // -----------------------------------------------------------

                        Vector3 gripInHand = Quaternion.Inverse(handRot) * (grip.position - handPos);
                        Vector3 headInHand = Quaternion.Inverse(handRot) * (head.position - handPos);

                        if (gripInHand.magnitude > 0.001f)
                        {
                            Fail($"pitch {pitch}: grip is {gripInHand.magnitude:F3} m from the hand (should be 0)");
                        }

                        Vector3 shaftDown = (headInHand - gripInHand).normalized;
                        float expectedY = -Mathf.Cos(pitch * Mathf.Deg2Rad);
                        float expectedZ = Mathf.Sin(pitch * Mathf.Deg2Rad);
                        float angleErr = Vector3.Angle(shaftDown, new Vector3(0f, expectedY, expectedZ));
                        // The head sits slightly off the shaft's end, so allow a small error.
                        if (angleErr > 3.0f)
                        {
                            Fail($"pitch {pitch}, lying {lying.eulerAngles}, hand {handRot.eulerAngles}: shaft direction in hand space is {shaftDown.ToString("F2")}, expected (0, {expectedY:F2}, {expectedZ:F2}) -- {angleErr:F1} deg off");
                        }
                    }
                }
            }

            // With a level hand at the default lean, where does the head hang?
            putter.gripPitchDegrees = 15f;
            putter.ApplyGripPose();
            putter.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Quaternion levelRotDiff = Quaternion.Inverse(grip.rotation) * putter.transform.rotation;
            putter.transform.rotation = levelRotDiff;
            Vector3 levelPosDiff = putter.transform.position - grip.position;
            putter.transform.position = levelPosDiff;
            Vector3 defaultHeadInHand = head.position;
            Debug.Log($"[GripPose] default 15 deg lean: head is {defaultHeadInHand.ToString("F2")} from the hand (x right, y up, z forward)");
            if (defaultHeadInHand.y > -0.75f || defaultHeadInHand.y < -0.95f)
            {
                Fail($"with a level hand the head should hang ~0.87 m below it; got {defaultHeadInHand.y:F2}");
            }
            if (defaultHeadInHand.z < 0.15f)
            {
                Fail($"the head should sit out in front of the hand at the default lean; got z={defaultHeadInHand.z:F2}");
            }

            Valve.VR.InteractionSystem.Interactable interactable = putter.GetComponent<Valve.VR.InteractionSystem.Interactable>();
            if (interactable.handFollowTransform)
            {
                Fail("Interactable.handFollowTransform is still on: the hand model would be dragged to the putter pivot (club head) instead of staying on the controller at the grip.");
            }
            else
            {
                Debug.Log("[GripPose] handFollowTransform is off: hand model stays on the controller at the grip.");
            }

            Valve.VR.InteractionSystem.Throwable throwable = putter.GetComponent<Valve.VR.InteractionSystem.Throwable>();
            if (!throwable.stickyGrip)
            {
                Fail("Throwable.stickyGrip is off: the putter would drop when the grip button is released.");
            }
            if (throwable.attachmentOffset != grip)
            {
                Fail("Throwable.attachmentOffset is not the Grip transform.");
            }

            if (_failures == 0)
            {
                Debug.Log("[GripPose] RESULT: PASS - grip lands on the hand, head hangs below and ahead at the chosen lean, hand model stays put, grip is sticky.");
            }
            else
            {
                Debug.LogError($"[GripPose] RESULT: FAIL - {_failures} check(s) failed.");
            }
        }

        private static void Fail(string message)
        {
            _failures++;
            Debug.LogError("[GripPose] FAIL: " + message);
        }
    }
}
