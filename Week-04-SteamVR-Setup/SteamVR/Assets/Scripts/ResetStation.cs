using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// The "reset for the next player" kiosk from GolfVR, in miniature: pressing this HoverButton
    /// puts every ResettablePose object back, removes spawned cubes, and zeroes the target score.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class ResetStation : MonoBehaviour
    {
        public ThrowTarget target;

        AudioSource _audio;
        AudioClip _confirm;

        void Awake()
        {
            GetComponent<HoverButton>().onButtonDown.AddListener(OnPressed);
            _audio = PlaygroundAudio.SourceOn(gameObject);
            _confirm = PlaygroundAudio.Chime("ResetConfirm", 0.15f, 587.33f, 880f); // D5 -> A5, like GolfVR
        }

        void OnPressed(Hand hand)
        {
            foreach (var item in FindObjectsOfType<ResettablePose>()) item.ResetPose();
            if (target != null) target.ResetScore();
            _audio.PlayOneShot(_confirm);
            if (hand != null) hand.TriggerHapticPulse(1000);
            Debug.Log("[Week5] Playground reset");
        }
    }
}
