using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR
{
    /// <summary>
    /// A physical push-button (SteamVR HoverButton) for the driving range:
    /// clears every ball, trail and the shot history, or sends the driver back
    /// to its display table.
    /// </summary>
    [RequireComponent(typeof(HoverButton))]
    public class RangeButton : MonoBehaviour
    {
        public enum RangeAction { ClearBalls, ClubBack }

        public RangeAction action = RangeAction.ClearBalls;
        public AudioSource audioSource;

        private AudioClip _clip;
        private Renderer[] _renderers;
        private Color[] _originalColors;

        private void Awake()
        {
            HoverButton hoverButton = GetComponent<HoverButton>();
            hoverButton.onButtonDown.AddListener(OnPressed);
            hoverButton.onButtonUp.AddListener(OnReleased);

            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _originalColors[i] = _renderers[i].material.color;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
            GenerateClip();
        }

        /// <summary>What a press does; separate from OnPressed so tests can call it directly.</summary>
        public void Perform()
        {
            DrivingRangeManager range = DrivingRangeManager.Instance;
            if (range == null) return;

            if (action == RangeAction.ClearBalls)
            {
                range.ClearAll();
            }
            else if (range.club != null)
            {
                range.club.ReturnHome();
                range.ShowMessage("Driver returned to the table");
            }
        }

        private void OnPressed(Hand hand)
        {
            Perform();

            for (int i = 0; i < _renderers.Length; i++) _renderers[i].material.color = Color.white;
            if (hand != null) hand.TriggerHapticPulse(800);
            if (audioSource != null && _clip != null) audioSource.PlayOneShot(_clip, 0.7f);
        }

        private void OnReleased(Hand hand)
        {
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].material.color = _originalColors[i];
        }

        private void GenerateClip()
        {
            int sampleRate = 44100;
            int n = (int)(sampleRate * 0.15f);
            float[] s = new float[n];
            float f = action == RangeAction.ClearBalls ? 520f : 760f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                s[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 22f) * 0.5f;
            }
            _clip = AudioClip.Create("RangeButtonProcedural", n, 1, sampleRate, false);
            _clip.SetData(s, 0);
        }
    }
}
