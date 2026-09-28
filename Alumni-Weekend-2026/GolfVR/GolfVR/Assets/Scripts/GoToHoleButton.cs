using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button at a hole's tee that takes the player to the
    /// NEXT hole's tee (MiniGolfGameManager.GoToHole -- travel only, nothing
    /// is reset). Every hole has its own ball, so this is the whole "move on
    /// to the next hole" flow: finish a hole, press the button, putt the ball
    /// that's waiting at the next one. Built on the same SteamVR HoverButton
    /// as the reset kiosk.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class GoToHoleButton : MonoBehaviour
    {
        [Tooltip("0-based index of the hole this button sits at; pressing it goes to the following hole (the last hole wraps to Hole 1)")]
        public int fromHoleIndex;

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
                MiniGolfGameManager.Instance.GoToHole(fromHoleIndex + 1);
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

        /// <summary>Quick rising two-note "off you go" chirp.</summary>
        private void GenerateConfirmAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] frequencies = { 587f, 880f };
            float noteDuration = duration / frequencies.Length;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float noteT = t % noteDuration;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequencies[noteIndex] * t) * Mathf.Exp(-noteT * 18f) * 0.5f;
            }

            _generatedConfirmClip = AudioClip.Create("GoToHoleConfirmProcedural", numSamples, 1, sampleRate, false);
            _generatedConfirmClip.SetData(samples, 0);
        }
    }
}
