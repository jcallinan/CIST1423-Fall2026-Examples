# Week 10 — Interactive AR: place, select, and swap

> **Chapter:** 6, interactive augmented reality using touch controls and C# UI.
> **Unity:** 2022.3.62f3. This is a source-first lab: create the scene and primitive prefabs in the Editor; no opaque asset downloads are needed.

## Lab goal

Build a small AR tabletop where a tap places a primitive on a detected surface. Tap a placed object to select it, then use screen UI to swap its variant, delete it, clear the scene, or show/hide detected planes. The same scripts have a mouse-and-ground-plane fallback for Editor testing.

## 1. Open the project

1. Open this folder in Unity Hub using **2022.3.62f3**. Let Package Manager resolve the manifest.
2. The manifest pins **AR Foundation 5.1.5**, **ARCore XR Plugin 5.1.5**, **ARKit XR Plugin 5.1.5**, **XR Plug-in Management 4.4.0**, and **Unity UI 1.0.0**. AR Foundation 5.1.5 is the Unity 2022.3-compatible package line used here. The Google ARCore and Apple ARKit plugins are provider packages; enable only the provider for your build target.
3. In **Edit → Project Settings → XR Plug-in Management**, install/enable the Android provider for Android or the iOS provider for iOS. Enable **ARCore** under Android or **ARKit** under iOS. Resolve any Project Validation items shown for that platform.

If you are adding this lab to an existing Unity project instead, install the same package versions through Package Manager and copy `Assets/Scripts` into that project's `Assets` folder.

## 2. Build a scene from primitives

1. Create and save a scene named `InteractiveAR`.
2. Add an **AR Session** (`GameObject → XR → AR Session`). Add an **XR Origin (Mobile AR)** from the XR menu. Menu wording can vary slightly by package version; the required pieces are an `XROrigin` with an AR camera.
3. On the XR Origin, add **AR Plane Manager** and **AR Raycast Manager**. Assign a plane prefab to AR Plane Manager if you want visible plane boundaries. Keep the AR camera tagged `MainCamera`.
4. Create at least two variants from built-in primitives (for example, a cube and a sphere). Give each a useful name, scale it to a tabletop size (about 0.1–0.2 Unity units), and ensure it has a Collider. Drag each from the Hierarchy into `Assets/Prefabs` to create a prefab, then remove its scene instance.
5. Create an empty `PlacementController` object and attach `ARPlacementController`. Assign the XR Origin's AR Raycast Manager and AR Plane Manager, the AR camera, and your prefabs in the same order you want the Swap button to cycle through.
6. Create a Screen Space - Overlay Canvas with four uGUI Buttons named **Swap**, **Delete**, **Reset**, and **Planes**, plus a uGUI Text for status. Assign them to the controller fields. Create an EventSystem if the Canvas did not add one. Button listeners are connected by the script; do not add duplicate On Click handlers.
7. Press Play on a phone/tablet build to scan and place. The first prefab is the initial variant; Swap advances to the next variant and replaces the selected object in place.

### What the scripts do

| Script | Responsibility |
| --- | --- |
| `Assets/Scripts/ARPlacementController.cs` | Reads touch or editor mouse taps, selects objects by collider, raycasts detected AR planes, places/swaps/deletes/resets objects, and wires up the UI buttons. |
| `Assets/Scripts/ARPlaceable.cs` | Marks a selectable object and gives the current selection a small scale highlight. |

## 3. Editor and 2D fallback

You can test the interaction loop without an AR device:

1. Make a simple test scene camera, tag it `MainCamera`, and aim it toward the horizontal plane at the configured `Fallback Ground Height` (default `0`). For example, place the camera at `(0, 1.5, -4)` facing toward positive Z, with the ground at Y `0`.
2. Add the controller, primitive prefabs, Canvas, and EventSystem as above, but leave **AR Raycast Manager** and **AR Plane Manager** unassigned. The controller intersects mouse rays with the fallback ground plane and uses normal physics raycasts to select placed objects.
3. Press Play. Left-click the ground to place, click a placed object to select, and use the UI buttons. In this mode, the Planes button only changes the status message because there are no AR plane visuals.

When an AR Raycast Manager is assigned, a missed AR plane hit does **not** silently place an object on the fallback plane. Scan a surface until its plane is detected, then tap it.

## 4. Device build and test

### Android / ARCore

1. Use an ARCore-supported Android device. In **File → Build Settings**, select Android and switch platform if needed.
2. In XR Plug-in Management, enable **ARCore** for Android. Check Player Settings for the required Android API level and camera permission; accept the camera prompt on first launch.
3. Connect the phone with USB debugging enabled, select it in **Run Device**, and Build and Run. Move the phone slowly until a plane appears, then tap to place.

### iOS / ARKit

1. Use an ARKit-capable iPhone or iPad. Switch the build target to iOS and enable **ARKit** in XR Plug-in Management.
2. Set a unique Bundle Identifier and a clear camera usage description in Player Settings. Build the Xcode project, open it in Xcode, select your signing team/device, then build and run.

### Interaction checks

- [ ] A detected plane becomes visible while scanning.
- [ ] Tapping the plane places the current primitive; the selected item gets a subtle size highlight.
- [ ] Tapping another placed item changes selection.
- [ ] Swap replaces the selected primitive at the same location; repeated presses cycle through all assigned variants.
- [ ] Delete removes only the selected item; Reset clears all placed items and returns to the first variant.
- [ ] Planes toggles plane visualization without stopping plane detection.
- [ ] The mouse fallback supports place, select, swap, delete, and reset in the Editor.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| No detected surface | Confirm the correct provider is enabled for the active build target; use a supported device, grant camera access, and move slowly over a textured, well-lit surface. |
| Taps place nothing | Wait for a detected plane; verify the AR Raycast Manager is assigned and enabled. In fallback mode, verify the camera ray can reach the configured ground height. |
| Objects cannot be selected | Ensure each prefab has a Collider and an `ARPlaceable` is on its root or parent. Confirm the UI EventSystem is present. |
| UI buttons do nothing | Assign the Button fields and status Text on the controller. The buttons must be uGUI Buttons on a Canvas. |
| Compiler cannot find AR types | Let Package Manager finish resolving the pinned dependencies and check for package errors in the Console. |

## Deliverable

Add a screenshot from the device or Editor and answer:

- Which surface did you place on, and which prefab variants did you build?
- What changes in the selection highlight when you tap a different object?
- What did you test on-device versus in the Editor fallback?
- What interaction would you add next (for example, rotate, scale, or reposition)?