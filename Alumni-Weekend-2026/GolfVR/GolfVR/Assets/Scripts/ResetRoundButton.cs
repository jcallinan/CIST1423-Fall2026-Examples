using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button (built on SteamVR's own HoverButton, the same
    /// component its sample kiosk buttons use) that resets the whole course
    /// for the next group of players -- so event staff can reset between
    /// groups by walking up and pressing it, without relaunching the app or
    /// needing a headset-free debug shortcut.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class ResetRoundButton : MonoBehaviour
    {
        public AudioSource audioSource;

        [Tooltip("Color the button cap flashes to while pressed, before reverting to its real color (not a hardcoded one, unlike the stock SteamVR sample ButtonEffect)")]
        public Color pressedFlashColor = Color.cyan;

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
                MiniGolfGameManager.Instance.ResetForNextGroup();
                Debug.Log("[GolfVR] Course reset for the next group via ResetRoundButton.");
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = pressedFlashColor;
            }

            if (hand != null)
            {
                hand.TriggerHapticPulse(1000);
            }

            if (audioSource != null && _generatedConfirmClip != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(_generatedConfirmClip, 0.8f);
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
        /// Short two-note ascending "confirm" chime, same AudioClip.Create
        /// technique used for this project's other procedural sounds.
        /// </summary>
        private void GenerateConfirmAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] frequencies = { 587.33f, 880.00f }; // D5 -> A5
            float noteDuration = duration / frequencies.Length;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float freq = frequencies[noteIndex];
                float noteT = t % noteDuration;
                float env = Mathf.Exp(-noteT * 8f);

                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }

            _generatedConfirmClip = AudioClip.Create("ResetConfirmProcedural", numSamples, 1, sampleRate, false);
            _generatedConfirmClip.SetData(samples, 0);
        }
    }
}
