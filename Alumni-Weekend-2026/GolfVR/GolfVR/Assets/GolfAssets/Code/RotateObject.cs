using UnityEngine;

namespace YourNamespaceName
{
    public class RotateObject : MonoBehaviour
    {
        // Public variables to adjust the rotation speed and direction
        public Vector3 rotationDirection = Vector3.up; // Default rotation around the Y-axis
        public float rotationSpeed = 30f; // Rotation speed in degrees per second

        // Public variables to control the oscillation
        public bool enableOscillation = false; // Toggle for oscillation
        public float oscillationAmplitude = 0.5f; // Amplitude of the oscillation
        public float oscillationFrequency = 1f; // Frequency of the oscillation

        private Vector3 initialPosition; // Store the initial position for oscillation

        void Start()
        {
            // Store the initial position of the object
            initialPosition = transform.position;
        }

        void Update()
        {
            // Rotate the object
            Rotate();

            // Oscillate the object if enabled
            if (enableOscillation)
            {
                Oscillate();
            }
        }

        void Rotate()
        {
            // Calculate the rotation amount based on the speed and time
            float rotationAmount = rotationSpeed * Time.deltaTime;

            // Apply the rotation to the object
            transform.Rotate(rotationDirection * rotationAmount);
        }

        void Oscillate()
        {
            // Calculate the new Y position using a sine wave for smooth oscillation
            float newY = initialPosition.y + Mathf.Sin(Time.time * oscillationFrequency) * oscillationAmplitude;

            // Apply the new position to the object
            transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
        }
    }
}
