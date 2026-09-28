using UnityEngine;

namespace GolfVR
{
    public class ObstacleAnimator : MonoBehaviour
    {
        public enum ObstacleType
        {
            ContinuousRotation,
            Oscillation,
            BouncePad,
            BoostPad
        }

        [Header("Obstacle Behavior")]
        public ObstacleType obstacleType = ObstacleType.ContinuousRotation;

        [Header("Rotation Settings")]
        public Vector3 rotationAxis = Vector3.forward;
        public float rotationSpeed = 45f;

        [Header("Oscillation Settings")]
        public Vector3 oscillationAxis = Vector3.up;
        public float oscillationDistance = 1.0f;
        public float oscillationFrequency = 1.5f;

        [Header("Pad Settings")]
        [Tooltip("Force added when ball touches boost or bounce pad")]
        public float padForce = 8f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip activationClip;

        private Vector3 _startLocalPos;

        private void Start()
        {
            _startLocalPos = transform.localPosition;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            switch (obstacleType)
            {
                case ObstacleType.ContinuousRotation:
                    transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
                    break;

                case ObstacleType.Oscillation:
                    float offset = Mathf.Sin(Time.time * oscillationFrequency) * oscillationDistance;
                    transform.localPosition = _startLocalPos + oscillationAxis * offset;
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null) return;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) return;

            if (obstacleType == ObstacleType.BouncePad)
            {
                Vector3 bounceDir = (ball.transform.position - transform.position).normalized;
                bounceDir.y = Mathf.Clamp(bounceDir.y, 0.1f, 0.4f);
                bounceDir.Normalize();
                rb.velocity = Vector3.zero;
                rb.AddForce(bounceDir * padForce, ForceMode.VelocityChange);
                PlaySound();
            }
            else if (obstacleType == ObstacleType.BoostPad)
            {
                Vector3 boostDir = transform.forward;
                boostDir.y = 0f;
                boostDir.Normalize();
                rb.AddForce(boostDir * padForce, ForceMode.VelocityChange);
                PlaySound();
            }
        }

        private void PlaySound()
        {
            if (audioSource != null && activationClip != null)
            {
                audioSource.PlayOneShot(activationClip, 0.8f);
            }
        }
    }
}
