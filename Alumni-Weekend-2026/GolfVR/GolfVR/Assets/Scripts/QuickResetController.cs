using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// Beginner-friendly convenience buttons for first-time VR players: one
    /// snaps the putter to float right in front of them ready to grab, the
    /// other resets the ball to the floor right in front of them, in case
    /// either ends up dropped somewhere awkward or hard to physically reach.
    ///
    /// Repurposes the default SteamVR "SnapTurnLeft"/"SnapTurnRight" actions
    /// (already bound to a button on every supported controller type) since
    /// this project doesn't use snap-turning.
    /// </summary>
    public class QuickResetController : MonoBehaviour
    {
        public GolfPutter putter;
        public GolfBall ball;

        [Tooltip("How far in front of the player to place the reset item")]
        public float forwardDistance = 0.5f;

        [Tooltip("Height to float the putter's root at; its grip sits ~0.65m above its root, so this lands the grip around a natural hand-off height")]
        public float putterRootHeight = 0.65f;

        private SteamVR_Action_Boolean _snapPutterAction;
        private SteamVR_Action_Boolean _resetBallAction;

        private void Awake()
        {
            _snapPutterAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("SnapTurnRight");
            _resetBallAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("SnapTurnLeft");
        }

        private void Update()
        {
            if (_snapPutterAction != null && _snapPutterAction.GetStateDown(SteamVR_Input_Sources.Any))
            {
                SnapPutterToPlayer();
            }

            if (_resetBallAction != null && _resetBallAction.GetStateDown(SteamVR_Input_Sources.Any))
            {
                ResetBallToPlayer();
            }
        }

        private void SnapPutterToPlayer()
        {
            if (putter == null || putter.IsHeld || Player.instance == null) return;

            Vector3 pos = Player.instance.feetPositionGuess
                        + Player.instance.bodyDirectionGuess.normalized * forwardDistance
                        + Vector3.up * putterRootHeight;

            putter.ResetToPosition(pos, Quaternion.identity);
        }

        private void ResetBallToPlayer()
        {
            GolfBall target = MiniGolfGameManager.Instance != null && MiniGolfGameManager.Instance.CurrentBall != null
                ? MiniGolfGameManager.Instance.CurrentBall
                : ball;
            if (target == null || Player.instance == null) return;

            Vector3 pos = Player.instance.feetPositionGuess
                        + Player.instance.bodyDirectionGuess.normalized * forwardDistance;

            target.SpawnAtTee(pos);
        }
    }
}
