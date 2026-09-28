using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button (same SteamVR HoverButton foundation as
    /// ResetRoundButton) that jumps straight to one specific hole -- for a
    /// "pick any hole" panel used during in-headset testing, so a specific
    /// hole's fairway/geometry/sink logic can be checked directly without
    /// playing through every prior hole. Also works as a per-hole "put the
    /// ball and putter back" button when pressed for whichever hole is
    /// already active.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class JumpToHoleButton : MonoBehaviour
    {
        [Tooltip("0-based hole index this button jumps to (0 = Hole 1)")]
        public int holeIndex;

        public AudioSource audioSource;

        private AudioClip _generatedConfirmClip;
        private Renderer[] _renderers;
        private Color[] _originalColors;

        private void Awake()
        {
            HoverButton hoverButton = GetComponent<HoverButton>();
            hoverButton.onButtonDown.AddListener(OnPressed);
            hoverButton.onButtonUp.AddListener(OnReleased);

            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalColors[i] = _renderers[i].material.color;
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.playOnAwake = false;
            }

            GenerateConfirmAudioClip();
        }

        private void OnPressed(Hand hand)
        {
            if (MiniGolfGameManager.Instance != null)
            {
                MiniGolfGameManager.Instance.JumpToHole(holeIndex);
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = Color.white;
            }

            if (hand != null)
            {
                hand.TriggerHapticPulse(800);
            }

            if (audioSource != null && _generatedConfirmClip != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(_generatedConfirmClip, 0.7f);
            }
        }

        private void OnReleased(Hand hand)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }

        /// <summary>
        /// Short single-blip confirm tone, same AudioClip.Create technique
        /// used for this project's other procedural sounds.
        /// </summary>
        private void GenerateConfirmAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float freq = 720f + holeIndex * 40f; // slightly different pitch per hole, handy audio confirmation of which one fired
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 25f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }

            _generatedConfirmClip = AudioClip.Create("HoleJumpConfirmProcedural", numSamples, 1, sampleRate, false);
            _generatedConfirmClip.SetData(samples, 0);
        }
    }
}
