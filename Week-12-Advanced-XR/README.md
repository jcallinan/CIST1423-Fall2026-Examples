# Week 12 — Advanced XR: head-gaze dwell

**Chapter:** 8 — hand tracking, gaze, and multiplayer  
**Unity:** 2022.3.62f3  
**Feature:** Look at a target for 1.2 seconds to toggle its selected state. This lab demonstrates head-gaze dwell; it does not implement hand tracking or multiplayer.

## Packages

- Unity XR Interaction Toolkit **2.5.4** (`com.unity.xr.interaction.toolkit`)
- Input System, installed as an XRI dependency (`com.unity.inputsystem`)
- For a headset: XR Plug-in Management and OpenXR Plug-in, with the OpenXR loader enabled for the target platform

Create a **3D (Built-In Render Pipeline)** project in Unity 2022.3.62f3, add XRI 2.5.4 through Package Manager, then copy `Assets/Scripts` into the project's `Assets` folder. XRI provides the XR Origin and camera; the gaze scripts use a physics ray from that camera and do not depend on a headset-specific gaze API. No scene, package manifest, generated project files, or Unity build output is included.

## Build the test scene

1. Add an **XR Origin (VR)** to a new scene using the XRI Starter Assets or the XRI XR Origin setup. Ensure its camera has a **Camera** component and the XR Origin is active. For a desktop-only test, a regular Camera also works.
2. Add a **Physics Raycaster** only if you use UI targets; the provided 3D targets use Unity physics raycasts directly.
3. Create a sphere or cube in front of the camera. Keep its collider enabled, add `GazeDwellTarget`, and leave its renderer assigned automatically or assign it in the Inspector.
4. Select the XR camera and add `GazeDwellInteractor`. Leave **Use Mouse Position** off for headset head-gaze. Set **Dwell Duration** to `1.2` seconds.
5. Optional progress ring: add a UI Image with **Image Type = Filled** and **Fill Method = Radial 360**, then assign it to **Dwell Indicator**. For a headset, put the ring on a World Space Canvas parented to the camera and position it in front of the view. For the mouse fallback, use a Screen Space - Overlay Canvas; the ring follows the cursor.
6. Press Play. Keep the target in the center of view until the dwell completes. It turns green; looking at it again toggles it back. The target's **On Dwell Complete** UnityEvent can trigger another scene action.

## Desktop fallback

No headset is needed. Enable **Use Mouse Position** on `GazeDwellInteractor`, then move the cursor over a target and hold it there for the dwell duration. The project must have the Input System package active; if Unity prompts for an input handling mode, select **Input System Package (New)** and restart the editor.

## Test checklist

- [ ] A target with a collider changes to its gaze color while aimed at.
- [ ] Leaving the target before 1.2 seconds clears the progress ring and does not select it.
- [ ] Holding gaze for 1.2 seconds selects the target and invokes its UnityEvent once.
- [ ] Looking away and dwelling again toggles the target back to its idle state.
- [ ] The mouse fallback can dwell on targets in the Game view without a connected headset.
- [ ] With a headset, the XR Origin camera drives gaze and the optional world-space progress ring is visible.

## Scripts

- `Assets/Scripts/GazeDwellInteractor.cs` — camera raycast, dwell timer, optional fill indicator, and editor mouse fallback.
- `Assets/Scripts/GazeDwellTarget.cs` — gaze/selection visual states and a configurable completion event.

This is a local interaction prototype only. No network transport, shared state, or remote avatars are implemented, so it does not claim to be a multiplayer example.