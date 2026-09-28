using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// Drop on anything with a SteamVR Interactable (Throwable adds one for you). Logs every
    /// grab/release, buzzes the hand that grabbed it, and keeps a grab count on an optional
    /// floating TextMesh -- evidence for the Week 5 "document your grab test" deliverable.
    /// Same Interactable events GolfVR's GolfPutter listens to (onAttachedToHand/onDetachedFromHand).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class GrabReporter : MonoBehaviour
    {
        public TextMesh label;

        [Tooltip("Haptic buzz on grab, in microseconds")]
        public ushort hapticMicroseconds = 1500;

        Interactable _interactable;
        int _grabCount;

        void Awake() => _interactable = GetComponent<Interactable>();

        void OnEnable()
        {
            _interactable.onAttachedToHand += OnGrabbed;
            _interactable.onDetachedFromHand += OnReleased;
            UpdateLabel("ready");
        }

        void OnDisable()
        {
            _interactable.onAttachedToHand -= OnGrabbed;
            _interactable.onDetachedFromHand -= OnReleased;
        }

        void OnGrabbed(Hand hand)
        {
            _grabCount++;
            hand.TriggerHapticPulse(hapticMicroseconds);
            Debug.Log($"[Week5] {name} grabbed by {hand.handType} (grab #{_grabCount})");
            UpdateLabel($"held by {hand.handType}");
        }

        void OnReleased(Hand hand)
        {
            var body = GetComponent<Rigidbody>();
            float speed = body != null ? body.velocity.magnitude : 0f;
            Debug.Log($"[Week5] {name} released by {hand.handType} at {speed:0.00} m/s");
            UpdateLabel($"thrown at {speed:0.0} m/s");
        }

        void UpdateLabel(string state)
        {
            if (label != null) label.text = $"{name}\n{state}\ngrabs: {_grabCount}";
        }
    }
}
