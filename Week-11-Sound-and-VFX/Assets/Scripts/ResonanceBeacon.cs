using UnityEngine;

namespace Chapter7SoundVFX
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class ResonanceBeacon : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] ParticleSystem burst;
        [SerializeField, Min(20f)] float frequencyHz = 440f;
        [SerializeField, Range(0.08f, 4f)] float toneSeconds = 0.8f;
        [SerializeField, Min(0f)] float retriggerCooldown = 0.6f;

        AudioClip tone;
        float nextTriggerTime;

        void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            tone = CreateTone(frequencyHz, toneSeconds);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.transform.root.CompareTag("Player")) return;
            if (Time.time < nextTriggerTime) return;

            nextTriggerTime = Time.time + retriggerCooldown;
            if (burst != null) burst.Play();
            if (audioSource != null && tone != null) audioSource.PlayOneShot(tone);
        }

        void OnDestroy()
        {
            if (tone != null) Destroy(tone);
        }

        static AudioClip CreateTone(float frequency, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                float envelope = Mathf.Exp(-4.5f * time)
                    * Mathf.Min(1f, time / 0.015f)
                    * Mathf.Min(1f, (duration - time) / 0.025f);
                float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float secondHarmonic = Mathf.Sin(2f * Mathf.PI * frequency * 2f * time) * 0.25f;
                samples[i] = (fundamental + secondHarmonic) * envelope * 0.35f;
            }

            var clip = AudioClip.Create("ResonanceTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}