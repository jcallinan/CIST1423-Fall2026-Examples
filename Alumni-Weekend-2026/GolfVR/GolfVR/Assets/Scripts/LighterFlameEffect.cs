using System.Collections;
using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// Periodically "flicks" the lighter (a quick rotation snap, since the
    /// model is a single fused mesh with no separate lid to open) and ignites
    /// a flame above it for a few seconds before going dark again.
    /// </summary>
    public class LighterFlameEffect : MonoBehaviour
    {
        public Transform flame;

        [Tooltip("Minimum seconds between flame-ups")]
        public float minInterval = 15f;

        [Tooltip("Maximum seconds between flame-ups")]
        public float maxInterval = 28f;

        public float flickDuration = 0.25f;
        public float flickAngle = 25f;
        public float litDuration = 4f;

        private Light _flameLight;
        private Vector3 _flameLitScale;
        private Quaternion _restRotation;

        private void Awake()
        {
            _restRotation = transform.localRotation;

            if (flame != null)
            {
                _flameLitScale = flame.localScale;
                flame.localScale = Vector3.zero;
                _flameLight = flame.GetComponentInChildren<Light>();
                if (_flameLight != null) _flameLight.enabled = false;
            }
        }

        private void Start()
        {
            if (flame != null)
            {
                StartCoroutine(FlickLoop());
            }
        }

        private IEnumerator FlickLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                yield return FlickAndIgnite();
            }
        }

        private IEnumerator FlickAndIgnite()
        {
            Quaternion flicked = _restRotation * Quaternion.Euler(0f, 0f, flickAngle);

            yield return RotateTo(flicked, flickDuration * 0.4f);
            yield return RotateTo(_restRotation, flickDuration * 0.6f);

            if (_flameLight != null) _flameLight.enabled = true;
            yield return ScaleFlame(0f, 1f, 0.15f);

            yield return new WaitForSeconds(litDuration);

            yield return ScaleFlame(1f, 0f, 0.15f);
            if (_flameLight != null) _flameLight.enabled = false;
        }

        private IEnumerator RotateTo(Quaternion target, float duration)
        {
            Quaternion start = transform.localRotation;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(start, target, t / duration);
                yield return null;
            }
            transform.localRotation = target;
        }

        private IEnumerator ScaleFlame(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                flame.localScale = _flameLitScale * Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            flame.localScale = _flameLitScale * to;
        }
    }
}
