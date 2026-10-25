using UnityEngine;
using UnityEngine.UI;

// Scales a target to a slider's value every frame (the book's BusScaler, Ch. 5 pp. 120-122).
// The scale depends on live user input, so it can't be keyframed in an animation clip.
public class SliderScaler : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Slider slider;

    private Vector3 baseScale;

    private void Awake()
    {
        if (target == null || slider == null)
        {
            Debug.LogWarning($"{nameof(SliderScaler)} on {name} needs a Target and a Slider.", this);
            enabled = false;
            return;
        }

        // Slider value 1 means the target's size as placed in the scene.
        baseScale = target.localScale;
    }

    private void Update()
    {
        target.localScale = baseScale * slider.value;
    }
}
