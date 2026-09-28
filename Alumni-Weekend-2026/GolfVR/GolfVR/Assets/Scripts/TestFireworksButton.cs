using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button that plays the fireworks celebration directly,
    /// without needing to actually sink a putt first -- so the show (and its
    /// new longer duration) can be tested independently of the putter grip
    /// or sink detection working correctly.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class TestFireworksButton : MonoBehaviour
    {
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
            if (FireworksCelebrationController.Instance != null)
            {
                FireworksCelebrationController.Instance.PlayFireworksCelebration();
                Debug.Log("[GolfVR] Fireworks celebration triggered via TestFireworksButton.");
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

        private void GenerateConfirmAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] frequencies = { 660f, 990f }; // quick ascending "go" chirp
            float noteDuration = duration / frequencies.Length;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float noteT = t % noteDuration;
                float env = Mathf.Exp(-noteT * 20f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequencies[noteIndex] * t) * env * 0.5f;
            }

            _generatedConfirmClip = AudioClip.Create("FireworksTestConfirmProcedural", numSamples, 1, sampleRate, false);
            _generatedConfirmClip.SetData(samples, 0);
        }
    }
}
