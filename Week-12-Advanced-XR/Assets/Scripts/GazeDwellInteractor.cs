using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Week12AdvancedXR
{
    public sealed class GazeDwellInteractor : MonoBehaviour
    {
        [SerializeField] Camera gazeCamera;
        [SerializeField, Min(0.1f)] float dwellDuration = 1.2f;
        [SerializeField, Min(0.1f)] float maxDistance = 10f;
        [SerializeField] LayerMask targetLayers = ~0;
        [SerializeField] Image dwellIndicator;
        [SerializeField] bool useMousePosition;

        GazeDwellTarget currentTarget;
        float dwellElapsed;
        bool completed;

        void Awake()
        {
            if (gazeCamera == null) gazeCamera = GetComponent<Camera>();
            if (gazeCamera == null) gazeCamera = Camera.main;
            SetIndicator(0f);
        }

        void Update()
        {
            if (gazeCamera == null)
            {
                SetTarget(null);
                return;
            }

            Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
            Mouse mouse = Mouse.current;
            if (useMousePosition && mouse != null)
                ray = gazeCamera.ScreenPointToRay(mouse.position.ReadValue());

            GazeDwellTarget hitTarget = null;
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore))
                hitTarget = hit.collider.GetComponentInParent<GazeDwellTarget>();

            SetTarget(hitTarget);
            UpdateIndicatorPosition(mouse);

            if (currentTarget == null || completed) return;

            dwellElapsed += Time.deltaTime;
            SetIndicator(dwellElapsed / Mathf.Max(0.1f, dwellDuration));
            if (dwellElapsed >= dwellDuration)
            {
                completed = true;
                currentTarget.CompleteDwell();
            }
        }

        void SetTarget(GazeDwellTarget nextTarget)
        {
            if (currentTarget == nextTarget) return;

            if (currentTarget != null) currentTarget.EndGaze();
            currentTarget = nextTarget;
            dwellElapsed = 0f;
            completed = false;
            SetIndicator(0f);

            if (currentTarget != null) currentTarget.BeginGaze();
        }

        void SetIndicator(float amount)
        {
            if (dwellIndicator != null) dwellIndicator.fillAmount = Mathf.Clamp01(amount);
        }

        void UpdateIndicatorPosition(Mouse mouse)
        {
            if (dwellIndicator == null || dwellIndicator.canvas == null) return;
            if (dwellIndicator.canvas.renderMode != RenderMode.ScreenSpaceOverlay) return;

            Vector2 screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (useMousePosition && mouse != null)
                screenPosition = mouse.position.ReadValue();

            dwellIndicator.rectTransform.position = screenPosition;
        }

        void OnDisable()
        {
            if (currentTarget != null) currentTarget.EndGaze();
            currentTarget = null;
            dwellElapsed = 0f;
            completed = false;
            SetIndicator(0f);
        }
    }
}