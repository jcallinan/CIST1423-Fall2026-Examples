using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// Remembers where an object started so the reset kiosk can put it back (GolfVR's
    /// "reset for the next group" idea). Objects spawned at runtime can ask to be destroyed instead.
    /// </summary>
    public class ResettablePose : MonoBehaviour
    {
        [Tooltip("Destroy on reset instead of moving back (e.g. cubes spawned by the Menu button)")]
        public bool destroyOnReset;

        Vector3 _startPosition;
        Quaternion _startRotation;

        void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        public void ResetPose()
        {
            // Never yank something out of a player's hand.
            var interactable = GetComponent<Interactable>();
            if (interactable != null && interactable.attachedToHand != null) return;

            if (destroyOnReset)
            {
                Destroy(gameObject);
                return;
            }

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
