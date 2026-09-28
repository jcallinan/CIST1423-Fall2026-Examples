using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// A physical push-button built on SteamVR's HoverButton (the same pattern as GolfVR's
    /// ResetRoundButton): each press cycles the target through the Panther finishes used by
    /// the panther-builder WebXR app -- Royal Blue, Athletic Gold, Bronze, Marble, Onyx.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class PittColorButton : MonoBehaviour
    {
        public Renderer target;

        // Same hex values as panther-builder/src/constants.js PANTHER_FINISHES
        static readonly Color[] Finishes =
        {
            new Color32(0x00, 0x35, 0x94, 0xff), // Pitt Royal Blue
            new Color32(0xff, 0xb8, 0x1c, 0xff), // Pitt Athletic Gold
            new Color32(0x7b, 0x53, 0x2d, 0xff), // Cast Bronze
            new Color32(0xea, 0xe6, 0xdf, 0xff), // White Marble
            new Color32(0x18, 0x18, 0x1a, 0xff), // Bradford Onyx
        };

        int _index = -1;

        void Awake() => GetComponent<HoverButton>().onButtonDown.AddListener(OnPressed);

        void OnPressed(Hand hand)
        {
            if (target == null) return;
            _index = (_index + 1) % Finishes.Length;
            target.material.color = Finishes[_index];
            if (hand != null) hand.TriggerHapticPulse(1000);
        }
    }
}
