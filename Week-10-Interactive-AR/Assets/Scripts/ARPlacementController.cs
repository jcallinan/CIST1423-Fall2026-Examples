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
        [SerializeField] Button deleteButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button togglePlanesButton;
        [SerializeField] Text statusText;

        readonly List<ARPlaceable> _placedObjects = new List<ARPlaceable>();
        ARPlaceable _selectedObject;
        int _variantIndex;
        bool _showPlanes = true;

        void Awake()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (swapButton != null) swapButton.onClick.AddListener(SwapSelected);
            if (deleteButton != null) deleteButton.onClick.AddListener(DeleteSelected);
            if (resetButton != null) resetButton.onClick.AddListener(ResetScene);
            if (togglePlanesButton != null) togglePlanesButton.onClick.AddListener(TogglePlanes);

            if (planeManager != null) planeManager.planesChanged += OnPlanesChanged;
            SetStatus(objectPrefabs == null || objectPrefabs.Length == 0
                ? "Add at least one placeable prefab in the Inspector."
                : "Tap a detected plane to place an object.");
        }

        void OnDestroy()
        {
            if (swapButton != null) swapButton.onClick.RemoveListener(SwapSelected);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(DeleteSelected);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetScene);
            if (togglePlanesButton != null) togglePlanesButton.onClick.RemoveListener(TogglePlanes);
            if (planeManager != null) planeManager.planesChanged -= OnPlanesChanged;
        }

        void Update()
        {
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
#endif
        }

        public void SwapSelected()
        {
            if (objectPrefabs == null || objectPrefabs.Length == 0)
            {
                SetStatus("Add placeable prefabs in the Inspector first.");
                return;
            }

            _variantIndex = (_variantIndex + 1) % objectPrefabs.Length;
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