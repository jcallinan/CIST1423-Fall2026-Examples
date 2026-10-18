using UnityEngine;

namespace InteractiveAR
{
    public class ARPlaceable : MonoBehaviour
    {
        [SerializeField, Min(1f)] float selectedScaleMultiplier = 1.08f;

        Vector3 _baseScale;
        bool _isSelected;

        void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            transform.localScale = _baseScale * (_isSelected ? selectedScaleMultiplier : 1f);
        }
    }
}