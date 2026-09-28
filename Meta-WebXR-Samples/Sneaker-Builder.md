# How Meta's Sneaker Builder is built

> Official page: <https://developers.meta.com/horizon/documentation/web/web-sample-sneaker-builder/>
> Source: <https://github.com/meta-quest/webxr-showcases/tree/main/sneaker-builder> (MIT)
> Live: <https://meta-quest.github.io/webxr-showcases/sneaker-builder>

Sneaker Builder is a mixed-reality shopping demo. A pair of sneakers floats under
a welcome card in your real room (passthrough). You grab a shoe with one controller
and point at it with the other to pick a part (tongue, laces, midsole, and so on)
and a color. Then you set it on the floor to check your size.

It is only ~1,500 lines of JavaScript, and each concept in it maps onto something
you already know from Unity. That makes it a good first read of real WebXR code.

Our [`panther-builder`](../Alumni-Weekend-2026/panther-builder) started as a copy of
this project. See [FROM-SNEAKER-TO-PANTHER.md](../Alumni-Weekend-2026/panther-builder/FROM-SNEAKER-TO-PANTHER.md).

---

## 1. The stack

| Package | Role | Unity analogy |
| --- | --- | --- |
| `three` → **`super-three@0.165`** | three.js (Meta's fork, tuned for Quest) renders the scene and runs the WebXR session through `renderer.xr` | The engine itself |
| **`elics`** | Tiny Entity-Component-System library | `GameObject` + `MonoBehaviour`, split into data and logic |
| **`ratk`** (Reality Accelerator Toolkit) | Turns an HTML button into "Enter AR", requests MR features (planes, meshes, anchors, hit-test) | XR Plug-in Management + AR Foundation |
| `gamepad-wrapper` | Edge detection and haptics on top of the raw WebXR `Gamepad` | SteamVR Input actions |
| `troika-three-text` | Crisp SDF text in 3D | TextMeshPro |
| `three-mesh-bvh` | Fast raycasts against high-poly meshes | Physics raycasts |
| `howler` | Spatial audio | `AudioSource` with `spatialBlend = 1` |
| `webpack` + `webpack-dev-server` (HTTPS) | Bundles `src/` into `dist/`. WebXR **requires HTTPS** | Build Settings |
| `@gltf-transform` (`content/compress.mjs`) | Draco-compresses meshes and KTX2-compresses textures | Import settings / texture compression |

## 2. The architecture: ECS in 60 seconds

```
World
 ├─ Components (pure data)        GlobalComponent, PlayerComponent, GrabComponent,
 │                                FollowComponent, SoundEffectComponent
 ├─ Entities (IDs holding comps)  "the global", "the player", "left shoe", "right shoe", ...
 └─ Systems (logic, run in order every frame, find entities by *query*)
      PlayerSystem → WelcomeSystem → SizingSystem → InlineSystem → ConfigUISystem
      → GrabSystem → FollowSystem → AudioSystem → PointerSystem
```

`src/index.js` is the whole wiring diagram:

```js
const world = new World();
world
  .registerComponent(GlobalComponent) /* ...4 more... */
  .registerSystem(PlayerSystem)        // 1st: read controllers, compute justStartedSelecting
  .registerSystem(WelcomeSystem)
  .registerSystem(SizingSystem)
  .registerSystem(InlineSystem)        // desktop (non-XR) mode
  .registerSystem(ConfigUISystem)      // may switch a controller to "ray" mode...
  .registerSystem(GrabSystem)
  .registerSystem(FollowSystem)
  .registerSystem(AudioSystem)
  .registerSystem(PointerSystem);      // ...last: draw the pointer in whatever mode was chosen

renderer.setAnimationLoop(() => {      // the WebXR-safe game loop (≈ Unity's Update)
  world.update(clock.getDelta(), clock.elapsedTime);
  renderer.render(scene, camera);
});
```

> **Order matters.** `PlayerSystem` runs first, so every later system sees the same
> "trigger was *just* pressed this frame" flags. `PointerSystem` runs last, so it
> draws whatever mode the earlier systems chose. Unity makes you use Script
> Execution Order for this. Here, registration order is the execution order.

A system declares which entities it cares about:

```js
GrabSystem.queries = {
  global:   { required: [GlobalComponent] },
  player:   { required: [PlayerComponent] },
  sneakers: { required: [GrabComponent] },   // any entity with a GrabComponent is grabbable
};
```

## 3. File-by-file

### `scene.js`: renderer and session
- `new WebGLRenderer({ alpha: true })` → a transparent clear color is what lets **passthrough** show behind the 3D content.
- `renderer.xr.enabled = true` hands camera control to the headset while a session is running.
- Lighting comes from an HDR environment map (`venice_sunset_1k.exr` → `PMREMGenerator`).
- On `sessionstart` it asks the Quest for 72 Hz (`session.updateTargetFrameRate(72)`).

### `global.js`: shared loaders
One `LoadingManager`, with `GLTFLoader`, `DRACOLoader` (decoder copied to `vendor/draco`), and `KTX2Loader` (transcoder in `vendor/basis`). That is why `webpack.config.cjs` copies those folders out of `node_modules`.

### `player.js`: controllers → a clean input model
For each of the two WebXR input sources:
- `renderer.xr.getController(i)` → the **target-ray space**, where you point from.
- `renderer.xr.getControllerGrip(i)` → the **grip space**, where your hand is. It holds the controller model.
- On `connected`, it records `handedness` and wraps the `Gamepad` in a `GamepadWrapper`.
- On `selectstart`/`selectend`, it sets `isSelecting`. Every frame it derives
  `justStartedSelecting` / `justStoppedSelecting` (edge detection, the same idea as
  SteamVR's `GetStateDown`/`GetStateUp`).
- It aims a `Raycaster` down the target ray each frame, so any system can raycast with it.
- **Trick:** controller models get a shader that outputs `vec4(0,0,0,0)`. They write
  depth but are fully transparent, so they cut a hole in the virtual scene and your
  *real* controllers show through passthrough.
- The head pose comes from `frame.getViewerPose(...)` and is stored on `player.head`.
  UI anchors to it.

### `grab.js`: proximity grab by re-parenting
- It finds the grabbable nearest to each free controller, converts the controller
  position into the shoe's **local space**, and checks a hand-tuned box around the
  shoe (`localVec3.x > 0.02 - 0.18 && ...`).
- On `justStartedSelecting`: `targetRaySpace.attach(object)`. The three.js
  `attach()` re-parents *while keeping the world transform*, the same as Unity's
  `SetParent(parent, worldPositionStays: true)`.
- On release: `scene.attach(object)`. It sets `justAttached`/`justDetached` for other systems.

### `sneaker.js`: the product model
- It loads `sneaker.gltf` **once**. The right shoe is a clone with `scale.z = -1` (mirror image).
- **12 named parts** (`vamp`, `tongue`, `lace`, `midsole`, `outsole`, `eyestay`, ...). Some have
  `_secondary` and `_stiching` sub-meshes. `constants.js` lists which color sets each part allows.
- Changing a color copies `color/roughness/metalness` onto the part's existing
  material, so there's no material explosion.
- Hover highlight: raycast against the part meshes and bump `emissiveIntensity` to 0.1.
- A floating **config panel** (texture `color_picker_ui.png` + troika text + 12
  swatch tiles from `swatch.glb`) shows the part name, the color name, and the choices.
- **Prefabs** (`default`, `mcdonalds`, `panda`, `stone`) are whole-shoe color presets.

### `configUI.js`: two-handed customization
The core interaction idea: **one hand holds, the other hand points.**
- While a shoe is held, every *free* controller switches to `POINTER_MODE.Ray`.
- Ray hits a shoe part → select that part (click sound, 50 ms haptic pulse).
- Ray hits a swatch on the panel → apply the color (confirm sound, 100 ms pulse).
- The panel only shows while a shoe is held *and* a free hand exists.

### `size.js`: set it down to size it
- Drop a shoe **below 0.5 m** and it glides to the floor (`lerp`/`slerp` toward a
  surface target), turning to face you.
- Once it is on the floor, a sizing panel and a cloned sock-liner appear. Ray the
  top or bottom of the panel (`intersect.uv.y > 0.7` / `< 0.3`) to go up or down half a size.
- US men's/women's sizes → a length table in cm → `children[0].scale.setScalar(len / defaultLen)`.

### `follow.js`, `welcome.js`, `pointer.js`, `audio.js`: small reusable systems
- **Follow:** lerps an object toward a target once it drifts past a threshold and
  turns it to face you. This is "lazy follow" UI, which is more comfortable than head-locking.
- **Welcome:** a card anchored 0.5 m in front of your head, with the shoes hanging
  below it. The first grab detaches them into the world and hides the card.
- **Pointer:** a 4-prong "claw" that closes with trigger pressure (`gamepad.buttons[0].value`)
  and stretches into a ray in Ray mode.
- **Audio:** to play a sound, *create an entity* with a `SoundEffectComponent`.
  `AudioSystem` plays it at the right 3D position relative to the head, then
  destroys the entity. This is the ECS way to send a one-shot event.

### `landing.js`: the 2D web page
- Not in XR yet? The page shows an auto-rotating 3D preview (`OrbitControls`) and
  HTML buttons. ratk turns `#ar-button` into an **Enter AR** button that requires
  `hit-test`, `plane-detection`, `mesh-detection`, and `anchors`.
- Unsupported browser (such as a desktop PC)? It shows a "send to headset" button
  that opens `oculus.com/open_url/?url=...`.
- HTML → 3D communication happens through `data-*` attributes on DOM elements
  (for example `sizeOptions.dataset.value`), which the ECS systems poll each frame.

## 4. Run it yourself

```bash
git clone https://github.com/meta-quest/webxr-showcases.git
cd webxr-showcases/sneaker-builder
npm install
npm run serve          # https://localhost:8081 (accept the self-signed cert)
```

- **Quest:** put the headset on the same Wi-Fi and open `https://<your-PC-IP>:8081` in the Quest Browser.
- **No headset:** install the **Immersive Web Emulator** Chrome extension, open DevTools → WebXR tab, and pick a Quest 3.

## 5. Questions to answer while you read

1. Why does `GrabSystem` run *after* `ConfigUISystem`? What would break if you swapped them?
2. `sneaker.js` mirrors the right shoe with `scale.z = -1`. What does `size.js` have to do because of that? (Hint: search for `shoeId === 'right'`.)
3. Find the two places the code calls the raw WebXR API (`XRFrame`/`XRSession`) instead of going through three.js. Why couldn't three.js do it for them?
4. Rewrite the `AudioSystem` "event entity" idea as a Unity pattern. Would you use a `UnityEvent`, a C# `event`, or something else?
