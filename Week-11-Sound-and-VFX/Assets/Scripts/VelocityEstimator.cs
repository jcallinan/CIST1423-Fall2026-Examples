using UnityEngine;

namespace Chapter7SoundVFX
{
    // Estimates how fast this object is moving by averaging its position change over the last few frames.
    // It works for anything that moves without a Rigidbody: a tracked controller, a hand, or a Character
    // Controller. The book uses Valve's VelocityEstimator from the SteamVR plugin for the same job (Ch. 7 pp. 164-167).
    public class VelocityEstimator : MonoBehaviour
    {
        [SerializeField, Range(1, 30)] int averageFrames = 5;

        Vector3[] samples;
        int sampleCount;
        Vector3 previousPosition;

        public Vector3 Velocity
        {
            get
            {
                int count = Mathf.Min(sampleCount, samples.Length);
                if (count == 0) return Vector3.zero;

                Vector3 sum = Vector3.zero;
                for (int i = 0; i < count; i++) sum += samples[i];
                return sum / count;
            }
        }

        public float Speed => Velocity.magnitude;

        void Awake()
        {
            samples = new Vector3[averageFrames];
        }

        void OnEnable()
        {
            sampleCount = 0;
            previousPosition = transform.position;
        }

        // LateUpdate runs after movement scripts, so each sample sees this frame's final position.
        void LateUpdate()
        {
            if (Time.deltaTime <= 0f) return;

            samples[sampleCount % samples.Length] = (transform.position - previousPosition) / Time.deltaTime;
            sampleCount++;
            previousPosition = transform.position;
        }
    }
}
