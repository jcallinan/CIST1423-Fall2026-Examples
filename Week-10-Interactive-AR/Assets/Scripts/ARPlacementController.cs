using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace InteractiveAR
{
    public class ARPlacementController : MonoBehaviour
    {
        static readonly List<ARRaycastHit> RaycastHits = new List<ARRaycastHit>();

        [Header("AR scene")]
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] Camera arCamera;

        [Header("Placeable variants")]
        [SerializeField] GameObject[] objectPrefabs;
        [SerializeField] float fallbackGroundHeight;

        [Header("UI")]
        [SerializeField] Button swapButton;
        [SerializeField] Button previousButton;
        [SerializeField] Button deleteButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button togglePlanesButton;
        [SerializeField] Button placeButton;
        [SerializeField] Text statusText;

        [Header("Placement reticle and details (optional)")]
        [Tooltip("Shown on the surface under the screen center; the Place button drops the current variant there.")]
        [SerializeField] Transform placementIndicator;
        [Tooltip("World-space label that floats above the selected object and shows its details.")]
        [SerializeField] Text detailsLabel;
        [SerializeField] float detailsHeight = 0.08f;
        [SerializeField, Min(0f)] float pinchSensitivity = 0.003f;
        [SerializeField, Min(0f)] float scrollScaleStep = 0.1f;

        readonly List<ARPlaceable> _placedObjects = new List<ARPlaceable>();
        ARPlaceable _selectedObject;
        int _variantIndex;
        bool _showPlanes = true;
        bool _indicatorPoseValid;
        Pose _indicatorPose;
        float _previousPinchDistance;
        Transform _detailsRoot;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (detailsLabel != null)
            {
                // includeInactive: the label's canvas starts hidden, and Graphic.canvas is null while inactive.
                Canvas labelCanvas = detailsLabel.GetComponentInParent<Canvas>(true);
                _detailsRoot = labelCanvas != null ? labelCanvas.transform : detailsLabel.transform;
            }
            if (swapButton != null) swapButton.onClick.AddListener(SwapSelected);
            if (previousButton != null) previousButton.onClick.AddListener(SwapToPrevious);
            if (deleteButton != null) deleteButton.onClick.AddListener(DeleteSelected);
            if (resetButton != null) resetButton.onClick.AddListener(ResetScene);
            if (togglePlanesButton != null) togglePlanesButton.onClick.AddListener(TogglePlanes);
            if (placeButton != null) placeButton.onClick.AddListener(PlaceAtIndicator);

            if (planeManager != null) planeManager.planesChanged += OnPlanesChanged;
            SetStatus(objectPrefabs == null || objectPrefabs.Length == 0
                ? "Add at least one placeable prefab in the Inspector."
                : "Tap a detected plane to place an object.");
            UpdateDetailsLabel();
        }

        void OnDestroy()
        {
            if (swapButton != null) swapButton.onClick.RemoveListener(SwapSelected);
            if (previousButton != null) previousButton.onClick.RemoveListener(SwapToPrevious);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(DeleteSelected);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetScene);
            if (togglePlanesButton != null) togglePlanesButton.onClick.RemoveListener(TogglePlanes);
            if (placeButton != null) placeButton.onClick.RemoveListener(PlaceAtIndicator);
            if (planeManager != null) planeManager.planesChanged -= OnPlanesChanged;
        }

        void Update()
        {
            UpdatePlacementIndicator();

            if (Input.touchCount == 2)
            {
                PinchToScale(Input.GetTouch(0), Input.GetTouch(1));
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && !IsPointerOverUI(touch.fingerId))
                    HandleTap(touch.position);
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                HandleTap(Input.mousePosition);

            // Editor stand-in for pinch: the scroll wheel scales the selected object.
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                ScaleSelected(1f + scroll * scrollScaleStep);
#endif
        }

        void LateUpdate()
        {
            UpdateDetailsLabel();
        }

        public void SwapSelected() => SwapBy(1);

        public void SwapToPrevious() => SwapBy(-1);

        public void SwapBy(int step)
        {
            if (objectPrefabs == null || objectPrefabs.Length == 0)
            {
                SetStatus("Add placeable prefabs in the Inspector first.");
                return;
            }

            _variantIndex = (_variantIndex + step + objectPrefabs.Length) % objectPrefabs.Length;
            if (_selectedObject != null)
            {
                ARPlaceable oldObject = _selectedObject;
                int oldIndex = _placedObjects.IndexOf(oldObject);
                Transform oldTransform = oldObject.transform;
                GameObject replacement = Instantiate(
                    objectPrefabs[_variantIndex], oldTransform.position, oldTransform.rotation);
                ARPlaceable newObject = EnsurePlaceable(replacement);

                if (oldIndex >= 0) _placedObjects[oldIndex] = newObject;
                oldObject.SetSelected(false);
                Destroy(oldObject.gameObject);
                _selectedObject = newObject;
                _selectedObject.SetSelected(true);
                SetStatus("Swapped to " + objectPrefabs[_variantIndex].name + ".");
                return;
            }

            SetStatus("Next placement: " + objectPrefabs[_variantIndex].name + ". Tap a plane to place it.");
        }

        public void DeleteSelected()
        {
            if (_selectedObject == null)
            {
                SetStatus("Tap a placed object to select it first.");
                return;
            }

            _placedObjects.Remove(_selectedObject);
            Destroy(_selectedObject.gameObject);
            _selectedObject = null;
            SetStatus("Object removed. Tap a plane to place another.");
        }

        public void ResetScene()
        {
            for (int i = 0; i < _placedObjects.Count; i++)
            {
                if (_placedObjects[i] != null) Destroy(_placedObjects[i].gameObject);
            }

            _placedObjects.Clear();
            _selectedObject = null;
            _variantIndex = 0;
            SetStatus("Cleared. Tap a plane to place " + CurrentVariantName() + ".");
        }

        public void TogglePlanes()
        {
            _showPlanes = !_showPlanes;
            if (planeManager != null)
            {
                foreach (ARPlane plane in planeManager.trackables)
                    SetPlaneVisible(plane, _showPlanes);
            }

            SetStatus(_showPlanes ? "Plane visualization on." : "Plane visualization off.");
        }

        // The book's flow (Ch. 6 pp. 138-140): aim the reticle with the device, then press Place.
        public void PlaceAtIndicator()
        {
            if (!_indicatorPoseValid)
            {
                SetStatus("No surface under the reticle yet. Move the device until it appears.");
                return;
            }

            PlaceObject(_indicatorPose);
        }

        public void ScaleSelected(float factor)
        {
            if (_selectedObject == null) return;
            _selectedObject.ScaleBy(factor);
            SetStatus("Scale: " + _selectedObject.ScaleFactor.ToString("0.00") + "x");
        }

        void PinchToScale(Touch first, Touch second)
        {
            float distance = Vector2.Distance(first.position, second.position);
            if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
            {
                _previousPinchDistance = distance;
                return;
            }

            if (first.phase != TouchPhase.Moved && second.phase != TouchPhase.Moved) return;

            ScaleSelected(1f + (distance - _previousPinchDistance) * pinchSensitivity);
            _previousPinchDistance = distance;
        }

        void UpdatePlacementIndicator()
        {
            if (arCamera == null) return;

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _indicatorPoseValid = TryGetPlacementPose(screenCenter, arCamera.ScreenPointToRay(screenCenter), out _indicatorPose);

            if (placementIndicator == null) return;
            placementIndicator.gameObject.SetActive(_indicatorPoseValid);
            if (_indicatorPoseValid)
                placementIndicator.SetPositionAndRotation(_indicatorPose.position, _indicatorPose.rotation);
        }

        void UpdateDetailsLabel()
        {
            if (_detailsRoot == null) return;

            bool show = _selectedObject != null && _selectedObject.Details != null
                && !string.IsNullOrEmpty(_selectedObject.Details.displayName);
            Transform labelRoot = _detailsRoot;
            labelRoot.gameObject.SetActive(show);
            if (!show) return;

            PlaceableDetails details = _selectedObject.Details;
            detailsLabel.text = "<b>" + details.displayName + "</b>\n" + details.description;

            // Float above the object and face the camera so it stays readable.
            Bounds bounds = GetBounds(_selectedObject.gameObject);
            labelRoot.position = new Vector3(bounds.center.x, bounds.max.y + detailsHeight, bounds.center.z);
            if (arCamera != null)
                labelRoot.rotation = Quaternion.LookRotation(labelRoot.position - arCamera.transform.position);
        }

        void HandleTap(Vector2 screenPosition)
        {
            if (arCamera == null)
            {
                SetStatus("Assign the AR camera or tag it MainCamera.");
                return;
            }

            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit objectHit))
            {
                ARPlaceable tappedObject = objectHit.collider.GetComponentInParent<ARPlaceable>();
                if (tappedObject != null)
                {
                    SelectObject(tappedObject);
                    return;
                }
            }

            if (TryGetPlacementPose(screenPosition, ray, out Pose pose))
                PlaceObject(pose);
            else
                SetStatus("No plane hit yet. Move the device to scan a surface, then tap it.");
        }

        bool TryGetPlacementPose(Vector2 screenPosition, Ray ray, out Pose pose)
        {
            if (raycastManager != null && raycastManager.Raycast(
                    screenPosition, RaycastHits, TrackableType.PlaneWithinPolygon))
            {
                pose = RaycastHits[0].pose;
                return true;
            }

            if (raycastManager == null)
            {
                Plane fallbackPlane = new Plane(Vector3.up, new Vector3(0f, fallbackGroundHeight, 0f));
                if (fallbackPlane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    Vector3 forward = arCamera.transform.forward;
                    forward.y = 0f;
                    Quaternion rotation = forward.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                        : Quaternion.identity;
                    pose = new Pose(point, rotation);
                    return true;
                }
            }

            pose = default;
            return false;
        }

        void PlaceObject(Pose pose)
        {
            if (objectPrefabs == null || objectPrefabs.Length == 0)
            {
                SetStatus("Add placeable prefabs in the Inspector first.");
                return;
            }

            GameObject instance = Instantiate(objectPrefabs[_variantIndex], pose.position, pose.rotation);
            ARPlaceable placeable = EnsurePlaceable(instance);
            _placedObjects.Add(placeable);
            SelectObject(placeable);
            SetStatus("Placed " + objectPrefabs[_variantIndex].name + ". Tap another plane or object.");
        }

        ARPlaceable EnsurePlaceable(GameObject instance)
        {
            ARPlaceable placeable = instance.GetComponent<ARPlaceable>();
            return placeable != null ? placeable : instance.AddComponent<ARPlaceable>();
        }

        void SelectObject(ARPlaceable placeable)
        {
            if (_selectedObject == placeable) return;
            if (_selectedObject != null) _selectedObject.SetSelected(false);
            _selectedObject = placeable;
            _selectedObject.SetSelected(true);
            SetStatus("Selected " + placeable.gameObject.name + ". Use Swap or Delete.");
        }

        void OnPlanesChanged(ARPlanesChangedEventArgs changes)
        {
            foreach (ARPlane plane in changes.added) SetPlaneVisible(plane, _showPlanes);
            foreach (ARPlane plane in changes.updated) SetPlaneVisible(plane, _showPlanes);
        }

        static void SetPlaneVisible(ARPlane plane, bool visible)
        {
            Renderer[] renderers = plane.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer planeRenderer in renderers)
                planeRenderer.enabled = visible;
        }

        static Bounds GetBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        string CurrentVariantName()
        {
            return objectPrefabs != null && objectPrefabs.Length > 0
                ? objectPrefabs[_variantIndex].name
                : "an object";
        }

        static bool IsPointerOverUI(int pointerId = -1)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
