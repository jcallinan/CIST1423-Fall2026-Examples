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

        [Header("Impact speed (book Ch. 7 pp. 164-177)")]
        [Tooltip("Scale the tone's volume and the number of particles by how fast the player arrives. " +
                 "Needs a VelocityEstimator on the entering object or its root.")]
        [SerializeField] bool scaleWithSpeed = true;
        [SerializeField, Min(0f)] float minimumSpeed = 0.5f;
        [SerializeField, Min(0.01f)] float maximumSpeed = 4f;
        [Tooltip("Quietest volume at or below the minimum speed, so a slow approach is still audible.")]
        [SerializeField, Range(0f, 1f)] float minimumVolume = 0.15f;
        [SerializeField, Min(0f)] float particlesPerMeterPerSecond = 10f;

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

            VelocityEstimator estimator = other.GetComponentInParent<VelocityEstimator>();
            if (scaleWithSpeed && estimator != null)
            {
                // InverseLerp maps the speed to 0-1 between the minimum and maximum speeds, as in the book.
                float speed = estimator.Speed;
                float strength = Mathf.InverseLerp(minimumSpeed, maximumSpeed, speed);
                if (audioSource != null && tone != null)
                    audioSource.PlayOneShot(tone, Mathf.Lerp(minimumVolume, 1f, strength));
                if (burst != null)
                    burst.Emit(Mathf.Max(1, Mathf.RoundToInt(speed * particlesPerMeterPerSecond)));
                return;
            }

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
