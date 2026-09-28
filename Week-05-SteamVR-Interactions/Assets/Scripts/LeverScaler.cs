using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// Reads a SteamVR LinearDrive's LinearMapping (0..1) and scales a target with it -- the
    /// textbook's Ch. 5 "scale an object with a slider", done with a physical lever instead of UI,
    /// and the same idea as panther-builder's scale slider.
    /// </summary>
    public class LeverScaler : MonoBehaviour
    {
        public LinearMapping mapping;
        public Transform target;
        public float minScale = 0.4f;
        public float maxScale = 1.6f;
        public TextMesh label;

        Vector3 _baseScale;
        float _lastValue = -1f;

        void Awake()
        {
            if (target != null) _baseScale = target.localScale;
        }

        void Update()
        {
            if (mapping == null || target == null || Mathf.Approximately(mapping.value, _lastValue)) return;
            _lastValue = mapping.value;
            float s = Mathf.Lerp(minScale, maxScale, mapping.value);
            target.localScale = _baseScale * s;
            if (label != null) label.text = $"LEVER\nScale {s:0.0}x";
        }
    }
}
