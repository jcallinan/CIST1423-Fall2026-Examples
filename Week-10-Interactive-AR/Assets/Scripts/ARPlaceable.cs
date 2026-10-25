using UnityEngine;

namespace InteractiveAR
{
    // Plain data shown above a selected object, like the book's Food class (Ch. 6 p. 134).
    // [System.Serializable] lets Unity save it and edit it in the Inspector.
    [System.Serializable]
    public class PlaceableDetails
    {
        public string displayName;
        [TextArea(1, 3)] public string description;
    }

    public class ARPlaceable : MonoBehaviour
    {
        const float MinScale = 0.1f;
        const float MaxScale = 10f;

        [SerializeField, Min(1f)] float selectedScaleMultiplier = 1.08f;
        [SerializeField] PlaceableDetails details = new PlaceableDetails();

        Vector3 _baseScale;
        bool _isSelected;

        public PlaceableDetails Details => details;
        public float ScaleFactor => _baseScale.x;

        void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            ApplyScale();
        }

        // Pinch to scale (Ch. 6 pp. 140-141): multiply the base size, clamped like the book's 0.1-10 range.
        public void ScaleBy(float factor)
        {
            float next = Mathf.Clamp(_baseScale.x * factor, MinScale, MaxScale);
            _baseScale = Vector3.one * next;
            ApplyScale();
        }

        void ApplyScale()
        {
            transform.localScale = _baseScale * (_isSelected ? selectedScaleMultiplier : 1f);
        }
    }
}
