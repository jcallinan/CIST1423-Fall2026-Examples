# Meta's WebXR samples: what to read and why

WebXR runs VR and MR apps in a headset's **browser**. There is nothing to install,
you ship a URL, and the same page works on Quest, on a PC headset through SteamVR
(Chrome/Edge), and as a normal 3D page on a laptop. Meta publishes several open-source
examples. This folder is a guided tour of the ones worth your time, in the order to read them.

> Unity is still our main engine (see [Week 4](../Week-04-SteamVR-Setup) /
> [Week 5](../Week-05-SteamVR-Interactions)). Read these samples to see the *same
> ideas* (rigs, grab, rays, UI that follows you, passthrough) with nothing hidden
> behind an editor. Our Alumni Weekend [`panther-builder`](../Alumni-Weekend-2026/panther-builder)
> is built on the first one, and [`Week-07-WebXR-WaterWorks`](../Week-07-WebXR-WaterWorks) forks that again
> into a multiplayer water-system builder.

## The WebXR Showcases (meta-quest/webxr-showcases)

Repo: <https://github.com/meta-quest/webxr-showcases> (MIT). All four share one
stack: **three.js (`super-three`) + `elics` ECS + `ratk` + `gamepad-wrapper`**,
built with webpack. Learn the pattern once and you can read all four.

| Sample | Type | What it teaches | Extra libraries | Live demo |
| --- | --- | --- | --- | --- |
| **Sneaker Builder** → [full walkthrough](Sneaker-Builder.md) | MR shopping | Grab by re-parenting, two-handed "hold + point" customization, material swapping on named parts, follow-UI, dropping on the floor to check size, ECS sound events | troika text, three-mesh-bvh, howler | [open](https://meta-quest.github.io/webxr-showcases/sneaker-builder) |
| **Chairs Etc.** | MR shopping | Place, rotate, and drag furniture on **real detected floors** (plane detection + hit-test), physics, a spatial UI panel toolkit, product search | `@dimforge/rapier3d` physics, `@pmndrs/uikit` | [open](https://meta-quest.github.io/webxr-showcases/chairs-etc) |
| **RealMeasure** | MR utility | A tape measure between controller points. Precise controller pose use, snapping, settings UI, measurements anchored in the room | troika text | [open](https://meta-quest.github.io/webxr-showcases/realmeasure) |
| **Flap Frenzy** | VR game | **Body movement as input:** flap your arms to fly, spread them to glide, tuck them to dive. Game loop, difficulty ramp, saved high scores | `localforage` (save data) | [open](https://meta-quest.github.io/webxr-showcases/flap-frenzy) |

**Suggested reading order.** Start with Sneaker Builder, which covers the full
pattern. Then read Flap Frenzy (`src/flap.js`), which gets gameplay out of raw
controller positions. Then Chairs Etc. (`src/furniture.js`, `src/marker.js`) for real
scene understanding and physics. RealMeasure is a good weekend read.

### Running any of them

```bash
git clone https://github.com/meta-quest/webxr-showcases.git
cd webxr-showcases/<sample-folder>
npm install
npm run serve        # https://localhost:8081 : WebXR needs HTTPS, accept the self-signed cert once
```

## The toolkit repos behind them

| Project | What it is | Why you care |
| --- | --- | --- |
| **Reality Accelerator Toolkit (`ratk`)**, <https://github.com/meta-quest/reality-accelerator-toolkit> | Wraps WebXR's MR features (planes, meshes, anchors, hit-test) as three.js objects, plus the Enter AR / Enter VR buttons | Every showcase uses it. `panther-builder` uses its `ARButton` **and** `VRButton` |
| **Immersive Web Emulator (IWE / IWER)**, Chrome extension | Emulates a Quest headset and controllers in desktop Chrome DevTools | Test WebXR without a headset, much like SteamVR's 2D fallback |
| **Immersive Web SDK (IWSDK)**, <https://github.com/facebook/immersive-web-sdk> · docs <https://iwsdk.dev> | Meta's newer, full framework: three.js + ECS with built-in grab, locomotion, spatial UI, Havok physics, and scene understanding. `npm create @iwsdk@latest` scaffolds a project | The showcases hand-roll what IWSDK now ships. Read a showcase to learn *how*, then use IWSDK to build faster |
| **webxr-first-steps**, <https://github.com/meta-quest/webxr-first-steps> | Meta's beginner WebXR tutorial project | The "hello world" to do before the showcases, if you have never written three.js |

## How this maps to Unity + SteamVR

| Idea | WebXR showcases | Unity + SteamVR |
| --- | --- | --- |
| Game loop | `renderer.setAnimationLoop` → `world.update()` | `Update()` on every `MonoBehaviour` |
| Code organization | ECS: components are data, systems are logic, in registration order | Components with data and logic together, plus Script Execution Order |
| Player rig | `renderer.xr.getController(i)` / `getControllerGrip(i)` | `Player` prefab, `Hand` |
| "Just pressed" | `isSelecting && !_prevSelecting` (hand-rolled) | `SteamVR_Action_Boolean.GetStateDown` |
| Grab | `controller.attach(obj)` / `scene.attach(obj)` | `Hand.AttachObject` / `DetachObject` |
| Haptics | `gamepadWrapper.getHapticActuator(0).pulse(0.1, 50)` | `hand.TriggerHapticPulse(...)` |
| Passthrough | `alpha: true` renderer + `immersive-ar` session | Not in SteamVR (use the Meta XR SDK) |
| Deploy | `npm run build` → any HTTPS host (GitHub Pages) | Build a Windows `.exe` |
