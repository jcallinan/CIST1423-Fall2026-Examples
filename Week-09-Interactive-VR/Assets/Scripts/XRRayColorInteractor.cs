using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRRayColorInteractor : MonoBehaviour
{
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private Camera fallbackCamera;
    [SerializeField] private ColorToggleTarget keyboardTarget;
    [SerializeField, Min(0.1f)] private float maxDistance = 10f;
    [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;

    private readonly List<InputDevice> devices = new List<InputDevice>();
    private InputDevice controller;
    private bool triggerWasPressed;

    private void Awake()
    {
        if (fallbackCamera == null)
        {
            fallbackCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (TryGetControllerRay(out Ray controllerRay, out bool triggerPressed))
        {
            if (triggerPressed && !triggerWasPressed)
            {
                TryToggleTarget(controllerRay);
            }

            triggerWasPressed = triggerPressed;
        }
        else
        {
            triggerWasPressed = false;
        }

        if (Input.GetMouseButtonDown(0) && fallbackCamera != null)
        {
            Ray mouseRay = fallbackCamera.ScreenPointToRay(Input.mousePosition);
            TryToggleTarget(mouseRay);
        }

        if (Input.GetKeyDown(KeyCode.Space) && keyboardTarget != null)
        {
            keyboardTarget.Toggle();
        }
    }

    private bool TryGetControllerRay(out Ray ray, out bool triggerPressed)
    {
        ray = default;
        triggerPressed = false;

        if (!controller.isValid)
        {
            CacheController();
        }

        if (!controller.isValid || trackingSpace == null ||
            !controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition) ||
            !controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation) ||
            !controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed))
        {
            controller = default;
            return false;
        }

        Vector3 worldPosition = trackingSpace.TransformPoint(localPosition);
        Quaternion worldRotation = trackingSpace.rotation * localRotation;
        ray = new Ray(worldPosition, worldRotation * Vector3.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);
        return true;
    }

    private void CacheController()
    {
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
            devices);

        if (devices.Count == 0)
        {
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left,
                devices);
        }

        if (devices.Count > 0)
        {
            controller = devices[0];
        }
    }

    private void TryToggleTarget(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionLayers))
        {
            ColorToggleTarget target = hit.collider.GetComponentInParent<ColorToggleTarget>();
            if (target != null)
            {
                target.Toggle();
            }
        }
    }
}