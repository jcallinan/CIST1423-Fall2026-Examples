using UnityEngine;
using UnityEngine.Events;

namespace Week12AdvancedXR
{
    [RequireComponent(typeof(Collider))]
    public sealed class GazeDwellTarget : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color idleColor = new Color(0.18f, 0.55f, 0.72f);
        [SerializeField] Color gazeColor = new Color(1f, 0.72f, 0.18f);
        [SerializeField] Color selectedColor = new Color(0.32f, 0.72f, 0.38f);
        [SerializeField] UnityEvent onDwellComplete;

        MaterialPropertyBlock propertyBlock;
        int colorPropertyId;
        bool selected;

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();

            if (targetRenderer != null && targetRenderer.sharedMaterial != null)
            {
                colorPropertyId = targetRenderer.sharedMaterial.HasProperty("_BaseColor")
                    ? Shader.PropertyToID("_BaseColor")
                    : Shader.PropertyToID("_Color");
            }

            ApplyColor(idleColor);
        }

        public void BeginGaze()
        {
            ApplyColor(gazeColor);
        }

        public void EndGaze()
        {
            ApplyColor(selected ? selectedColor : idleColor);
        }

        public void CompleteDwell()
        {
            selected = !selected;
            ApplyColor(selected ? selectedColor : idleColor);
            onDwellComplete?.Invoke();
        }

        void ApplyColor(Color color)
        {
            if (targetRenderer == null || colorPropertyId == 0) return;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}