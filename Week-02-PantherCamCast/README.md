# Week 2 — Panther Cam & Cast starter scene
These are built via a script an
Editor menu command assembles primitives, not hand-placed GameObjects.

## Use it

1. Copy `Assets/` into a Unity project (URP), or drop these two folders into an
   existing one.
2. In the Editor: **Panther Cam Cast > Build Starter Scene**.
3. Press Play. `Space` casts, hold `Left Mouse` (or `R`) to reel once the bobber
   starts dipping.

## What's here

| File | What it does |
| --- | --- |
| `Scripts/CastAndReelController.cs` | The state machine: Idle → Casting → Waiting → Biting → Reeling → Caught. Input-agnostic on purpose. |
| `Scripts/Bobber.cs` | Visual feedback only — bobs on the water, dips on a bite. |
| `Scripts/FishingRodInput.cs` | Desktop stand-in input. Swap for an XRI grab + trigger script later; `CastAndReelController` never has to change. |
| `Editor/BuildPantherCamCastScene.cs` | Idempotent scene builder — ground (dock), water plane, a gold placeholder cube standing in for the Panther, rod pivot, bobber, camera. |

## Next steps for this project

- Swap `Panther_PLACEHOLDER` for the real scanned-and-imported Panther model once
  the photogrammetry pipeline (see Week 2 slides) produces one.
- Add haptics/audio on bite and catch.
- VR conversion: new grab+trigger input script implementing the same `Cast()` /
  `Reel(float)` calls `FishingRodInput` makes today.
