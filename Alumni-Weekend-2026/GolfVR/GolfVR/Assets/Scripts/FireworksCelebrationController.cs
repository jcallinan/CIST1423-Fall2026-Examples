using System.Collections;
using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// Plays a brief day-to-night fireworks show whenever a hole is sunk:
    /// the sky darkens to night, a few firework bursts go off with horn
    /// blasts, then everything fades back to day. Runs independently of the
    /// hole-transition/scoreboard flow in MiniGolfGameManager -- it's a
    /// visual/audio overlay, not something gameplay waits on.
    /// </summary>
    public class FireworksCelebrationController : MonoBehaviour
    {
        public static FireworksCelebrationController Instance { get; private set; }

        [Header("Night Sky")]
        [Tooltip("Skybox material to switch to during the show; falls back to just dimming the lights if left null")]
        public Material nightSkybox;

        [Tooltip("Seconds to fade from day to night, and night back to day")]
        public float dayNightFadeDuration = 1.0f;

        [Tooltip("Seconds the fireworks show stays at full night before fading back to day")]
        public float nightHoldDuration = 3.0f;

        [Header("Fireworks")]
        public int burstCount = 5;
        public float burstAreaRadiusX = 10f;
        public float burstAreaRadiusZ = 10f;
        public float burstHeight = 9f;
        public Vector3 burstAreaCenter = Vector3.zero;

        [Header("Horn Audio")]
        public AudioSource audioSource;

        private Light _sun;
        private Material _daySkybox;
        private float _dayAmbientIntensity;
        private float _daySunIntensity;
        private AudioClip _generatedHornClip;
        private bool _isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sun = RenderSettings.sun;
            _daySkybox = RenderSettings.skybox;
            _dayAmbientIntensity = RenderSettings.ambientIntensity;
            _daySunIntensity = _sun != null ? _sun.intensity : 1f;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // heard everywhere on the course
                audioSource.playOnAwake = false;
            }

            GenerateHornAudioClip();
        }

        public void PlayFireworksCelebration()
        {
            if (_isPlaying) return;
            StartCoroutine(CelebrationRoutine());
        }

        private IEnumerator CelebrationRoutine()
        {
            _isPlaying = true;

            yield return StartCoroutine(FadeToNight());

            for (int i = 0; i < burstCount; i++)
            {
                Vector3 pos = burstAreaCenter + new Vector3(
                    Random.Range(-burstAreaRadiusX, burstAreaRadiusX),
                    burstHeight + Random.Range(-1.5f, 1.5f),
                    Random.Range(-burstAreaRadiusZ, burstAreaRadiusZ));

                SpawnFireworkBurst(pos);

                if (i == 0 || i == burstCount / 2)
                {
                    PlayHornBlast();
                }

                yield return new WaitForSeconds(nightHoldDuration / burstCount);
            }

            yield return StartCoroutine(FadeToDay());

            _isPlaying = false;
        }

        private IEnumerator FadeToNight()
        {
            float elapsed = 0f;
            bool skyboxSwapped = false;

            while (elapsed < dayNightFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dayNightFadeDuration);

                if (_sun != null) _sun.intensity = Mathf.Lerp(_daySunIntensity, _daySunIntensity * 0.03f, t);
                RenderSettings.ambientIntensity = Mathf.Lerp(_dayAmbientIntensity, _dayAmbientIntensity * 0.15f, t);

                if (!skyboxSwapped && nightSkybox != null && t > 0.5f)
                {
                    RenderSettings.skybox = nightSkybox;
                    DynamicGI.UpdateEnvironment();
                    skyboxSwapped = true;
                }

                yield return null;
            }
        }

        private IEnumerator FadeToDay()
        {
            float elapsed = 0f;
            bool skyboxSwapped = false;

            while (elapsed < dayNightFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dayNightFadeDuration);

                if (_sun != null) _sun.intensity = Mathf.Lerp(_daySunIntensity * 0.03f, _daySunIntensity, t);
                RenderSettings.ambientIntensity = Mathf.Lerp(_dayAmbientIntensity * 0.15f, _dayAmbientIntensity, t);

                if (!skyboxSwapped && t > 0.5f)
                {
                    RenderSettings.skybox = _daySkybox;
                    DynamicGI.UpdateEnvironment();
                    skyboxSwapped = true;
                }

                yield return null;
            }

            // Snap back exactly in case of any floating point drift.
            if (_sun != null) _sun.intensity = _daySunIntensity;
            RenderSettings.ambientIntensity = _dayAmbientIntensity;
            RenderSettings.skybox = _daySkybox;
            DynamicGI.UpdateEnvironment();
        }

        private void SpawnFireworkBurst(Vector3 position)
        {
            GameObject burstObj = new GameObject("FireworkBurst");
            burstObj.transform.position = position;

            ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
            Color burstColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 1f, 1f);

            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1.4f;
            main.startSpeed = 6.0f;
            main.startSize = 0.15f;
            main.gravityModifier = 0.6f;
            main.startColor = burstColor;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(burstColor, 0f), new GradientColorKey(Color.white, 0.3f), new GradientColorKey(burstColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            // Deliberately leave the renderer material as whatever Unity
            // assigns a freshly-created ParticleSystem by default (the same
            // approach GolfHole's confetti uses) rather than assigning a
            // custom shader/texture that may not render as expected.

            Light flash = burstObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = burstColor;
            flash.range = 14f;
            flash.intensity = 2.5f;
            StartCoroutine(FadeOutLight(flash, 0.4f));

            ps.Play();
            Destroy(burstObj, 3f);
        }

        private IEnumerator FadeOutLight(Light light, float duration)
        {
            float elapsed = 0f;
            float startIntensity = light.intensity;
            while (elapsed < duration && light != null)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
                yield return null;
            }
        }

        private void PlayHornBlast()
        {
            if (audioSource == null || _generatedHornClip == null) return;
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(_generatedHornClip, 0.8f);
        }

        /// <summary>
        /// Two-tone brass-horn "ta-da" blast, built the same way as the
        /// project's other procedural audio (bounce/fanfare clips).
        /// </summary>
        private void GenerateHornAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.9f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] frequencies = { 233.08f, 233.08f, 349.23f }; // Bb3, Bb3, F4 -- classic fanfare horn call
            float noteDuration = 0.3f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float freq = frequencies[noteIndex];
                float noteT = t % noteDuration;

                float attack = Mathf.Clamp01(noteT / 0.02f);
                float release = Mathf.Clamp01((noteDuration - noteT) / 0.08f);
                float env = attack * release;

                // Sawtooth-ish brass timbre via summed harmonics
                float v = 0f;
                for (int h = 1; h <= 5; h++)
                {
                    v += Mathf.Sin(2f * Mathf.PI * freq * h * t) / h;
                }

                samples[i] = v * env * 0.35f;
            }

            _generatedHornClip = AudioClip.Create("HornBlastProcedural", numSamples, 1, sampleRate, false);
            _generatedHornClip.SetData(samples, 0);
        }
    }
}
