using UnityEngine;

public class ColorToggleTarget : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color activeColor = Color.cyan;

    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
    private int colorPropertyId;
    private bool isActive;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
        {
            Debug.LogWarning($"{nameof(ColorToggleTarget)} on {name} needs a Renderer with a material.", this);
            enabled = false;
            return;
        }

        Material material = targetRenderer.sharedMaterial;
        if (material.HasProperty(BaseColorId))
        {
            colorPropertyId = BaseColorId;
        }
        else if (material.HasProperty(ColorId))
        {
            colorPropertyId = ColorId;
        }
        else
        {
            Debug.LogWarning($"The material on {name} has no _BaseColor or _Color property.", this);
            enabled = false;
            return;
        }

        originalColor = material.GetColor(colorPropertyId);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Toggle()
    {
        if (!enabled)
        {
            return;
        }

        isActive = !isActive;
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, isActive ? activeColor : originalColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
        Debug.Log($"{name} toggled {(isActive ? "on" : "off")}.", this);
    }
}