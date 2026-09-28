# Week 4 — VR Development in Unity I: SteamVR project setup

> **Syllabus:** Ch. 3 (first half). The textbook uses the XR Interaction Toolkit (XRI).
> This course uses **SteamVR** instead: the VR Lab in Marilyn Horne Hall runs HTC
> Vive Pro headsets through SteamVR, and our Alumni Weekend project
> ([`GolfVR`](../Alumni-Weekend-2026/GolfVR)) is a SteamVR project. The concepts in
> the chapter still apply. This page maps each XRI step to its SteamVR equivalent.

**Lab:** Configure a new SteamVR project and open the Interaction System sample scene.
**Deliverable:** A Markdown write-up of your setup steps (template at the bottom), plus your pushed project and `README.md`.

---

## 1. What does VR development involve?

A VR app is still a Unity app. It adds three things:

| Concern | What it means | In SteamVR |
| --- | --- | --- |
| **Rendering** | Draw the scene twice per frame (one image per eye) at 90 Hz, with no dropped frames | The OpenVR XR plug-in takes over the camera |
| **Tracking** | The headset and both controllers have a position and rotation (6DOF) every frame | `SteamVR_Behaviour_Pose` on each hand, and the `Player` prefab |
| **Input** | Map physical buttons to *actions* such as "grab" or "teleport" instead of reading raw buttons | SteamVR Input: `actions.json` plus a bindings file for each controller |

Most of the work is **interaction design**: how do you pick something up, move
around, or press a button when there is no mouse? The rest of this unit covers that.

## 2. Headset approaches: which SDK?

| Approach | Runs on | Unity package | When to use it |
| --- | --- | --- | --- |
| **SteamVR / OpenVR** *(this course)* | PC VR: Vive, Index, Quest via Link/Steam Link, WMR | SteamVR Unity Plugin 2.8 + OpenVR XR Plugin | Lab headsets. Includes a ready-made hands/teleport/grab framework (the **Interaction System**) |
| **OpenXR + XR Interaction Toolkit** *(textbook)* | Nearly everything, including standalone Quest | `com.unity.xr.openxr` + `com.unity.xr.interaction.toolkit` | Cross-vendor projects and Android/Quest builds |
| **Meta XR SDK** | Quest only (standalone or Link) | Meta XR All-in-One SDK | Quest-only features: passthrough, scene API, hand tracking |
| **WebXR** | Any headset browser | None (JavaScript + three.js) | No install needed. See [`Meta-WebXR-Samples`](../Meta-WebXR-Samples) and [`panther-builder`](../Alumni-Weekend-2026/panther-builder) |

> **Key idea:** OpenVR and OpenXR are both *runtimes*: the layer between Unity and
> the headset. SteamVR itself can act as an OpenXR runtime too. But the **SteamVR
> Unity Plugin's input and Interaction System only work when the *OpenVR* loader is
> active.** That is why the order of loaders in step 4 below matters.

## 3. Before you start

- [ ] **Steam** is installed, and **SteamVR** is installed from Steam → Library → Tools.
- [ ] Your headset shows the SteamVR home environment (green tracking icons in the SteamVR status window).
- [ ] **Unity 2022.3 LTS**. GolfVR uses `2022.3.62f3`, so use that version if you can.
- [ ] **Quest users:** connect with Meta Quest Link or Steam Link first. After that, SteamVR treats the Quest like any other PC headset.

## 4. Lab: create the project

### 4.1 New project
1. Unity Hub → **New project** → **3D (Built-In Render Pipeline)** core template.
   Name it `Week04-SteamVR-<yourname>`.
   > ⚠️ Do not choose URP. The Interaction System's hand, highlight, and teleport
   > materials use Built-In shaders and turn **pink** in URP. You can convert them
   > later, but it is not worth the trouble in Week 4.

### 4.2 Import the SteamVR Unity Plugin
2. Either:
   - **Asset Store:** search "SteamVR Plugin" (Valve Corporation), then **Add to My Assets** → Package Manager → **My Assets** → Import, *or*
   - **GitHub:** download `steamvr_2_8_0.unitypackage` from
     <https://github.com/ValveSoftware/steamvr_unity_plugin/releases> and choose Assets → Import Package → Custom Package.
3. Import everything. When the **Valve.VR.SteamVR_UnitySettingsWindow** pops up,
   click **Accept All** (it sets recommended project settings).
4. The plugin installs the **OpenVR XR Plugin** (`com.valvesoftware.unity.openvr`)
   for you. You can confirm this in `Packages/manifest.json`. GolfVR's manifest has:
   ```json
   "com.valvesoftware.unity.openvr": "file:../Assets/SteamVR/OpenVRUnityXRPackage/Editor/com.valvesoftware.unity.openvr-1.2.1.tgz"
   ```

### 4.3 XR Plug-in Management
5. **Edit → Project Settings → XR Plug-in Management** → **PC, Mac & Linux Standalone** tab.
6. Check **OpenVR Loader**. If **OpenXR** is also checked, make sure OpenVR is
   **listed first**. GolfVR's `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`
   lists `OpenVRLoader` and then `OpenXRLoader` as a fallback.
7. **XR Plug-in Management → OpenVR:** set *Stereo Rendering Mode* to **Single Pass Instanced**,
   which is faster, and *Mirror View Mode* to **Right**, so the Game view shows one eye.

### 4.4 SteamVR Input (actions & bindings)
8. **Window → SteamVR Input.** When it asks to copy example JSON files, click **Yes**.
9. Look at the action sets it created. The `default` set is what the Interaction System uses:

   | Action | Type | Vive Wand default | What uses it |
   | --- | --- | --- | --- |
   | `GrabPinch` | Boolean | Trigger | Picking things up |
   | `GrabGrip` | Boolean | Grip button | Picking things up (grip) |
   | `Teleport` | Boolean | Trackpad (center) | `Teleport` prefab |
   | `InteractUI` | Boolean | Trigger | UI buttons |
   | `SnapTurnLeft` / `SnapTurnRight` | Boolean | Trackpad (left/right edge) | Snap turning (GolfVR repurposes these, see Week 5) |
   | `Haptic` | Vibration (out) | — | `Hand.TriggerHapticPulse` |
   | `Pose`, `SkeletonLeftHand/RightHand` | Pose/Skeleton | — | Hand tracking and finger animation |

10. Click **Save and generate**. This writes `Assets/StreamingAssets/SteamVR/actions.json`
    and generates C# classes under `Assets/SteamVR_Input/`, so you can write
    `SteamVR_Actions.default_GrabPinch` in code.
11. *(Optional)* **Open binding UI** opens SteamVR's web binding editor, where you
    can remap buttons for each controller type. Bindings are saved as
    `bindings_<controller>.json` next to `actions.json`.

### 4.5 Run the sample
12. Open `Assets/SteamVR/InteractionSystem/Samples/Interactions_Example.unity`.
13. Put the headset on, then press **Play**. You should see your hands, a teleport
    arc (press the trackpad), and a room full of grabbable and throwable objects.
    Week 5 walks through this scene.
14. Also try `Assets/SteamVR/Simple Sample.unity`, the plugin's minimal scene
    (a floor, a sphere, and a tracked rig). Compare its Hierarchy with the full
    Interactions_Example to see how little a scene needs to "work in VR".

### 4.6 No headset? 2D fallback
The Interaction System's `Player` prefab has **Allow Toggle To 2D** turned on by
default. If SteamVR cannot start, it switches to a keyboard-and-mouse "fallback"
player (WASD to move, mouse to look, click to grab). It is limited, but you can
test scripts at home with it. (This fills the same role as XRI's *XR Device Simulator*.)

## 5. Common first-day problems

| Symptom | Likely cause / fix |
| --- | --- |
| Game view shows the scene but the headset is black | OpenVR Loader not checked, or OpenXR listed above it (§4.3) |
| Hands appear but no button does anything | You skipped **Save and generate** in the SteamVR Input window (§4.4) |
| Everything from the Interaction System is pink | The project is URP (§4.1) |
| `SteamVR_Input` errors about a missing `actions.json` | Delete `Assets/StreamingAssets/SteamVR`, then rerun step 8 |
| Quest shows the Oculus home, not SteamVR | Launch SteamVR from the desktop *after* connecting Link |

## 6. Read along: how the lab project does it

[`Alumni-Weekend-2026/GolfVR`](../Alumni-Weekend-2026/GolfVR) was set up with exactly these
steps, so open it next to your own project and compare:

- `Packages/manifest.json`: OpenVR XR plug-in (with OpenXR installed as well)
- `Assets/XR/Loaders/`: `OpenVRLoader.asset` and `OpenXRLoader.asset`
- `Assets/StreamingAssets/SteamVR/actions.json`, plus `bindings_vive_controller.json` for the Vive Wand
- `Assets/SteamVR/InteractionSystem/`: the stock Interaction System, unmodified

---

## Deliverable template

Copy this into your project's `README.md`, fill it in, add a screenshot, then push.

```markdown
# Week 4 — SteamVR project setup (<your name>)

## Environment
- Unity version:
- SteamVR plugin version:
- Headset / controllers:
- Render pipeline:

## Steps I followed
1. ...

## XR Plug-in Management
- Loaders enabled (in order):
- Stereo rendering mode:

## SteamVR Input
- Action set(s) used:
- One action I would rebind, and why:

## Screenshot
![Interactions_Example running](Docs/week04.png)

## Problems I hit and how I fixed them
-
```
