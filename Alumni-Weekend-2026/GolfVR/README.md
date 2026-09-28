# GolfVR • University of Pittsburgh at Bradford Alumni Weekend 2026

A complete 9-hole SteamVR Championship Mini Golf / Putt-Putt experience created for the **University of Pittsburgh at Bradford's Alumni & Family Weekend 2026**, highlighting the campus VR Lab in Marilyn Horne Hall!

## Overview

GolfVR is an interactive virtual reality mini golf game featuring modular stylized courses, SteamVR 6DOF controller putting with realistic physics and haptics, celebratory animations and procedural fanfare upon sinking putts, an in-VR scorecard tracking strokes and par across all 9 holes, and a 3D campus Panther statue overlooking the course.

## The 9-Hole Championship Course (Par 26)

1. **Hole 1: Panther Straightaway (Par 2)**
   - Smooth curved fairway with safety bumpers, ideal for getting familiar with the putter physics.
   - Sinking the putt triggers a victory fanfare and celebration flag animation.
2. **Hole 2: The Windmill Hazard (Par 3)**
   - Features the iconic rotating windmill obstacle (`WindMillFullColorFull`) and banking turns.
   - Requires timing your stroke through the spinning blades and around the banked cushions.
3. **Hole 3: Panther Peak (Par 3)**
   - Elevation ramp (`UpHill`), elevated dogleg fairway, and sunken victory cup.
   - Overlooked by the **Pitt Bradford Panther Statue** (`PittPantherStatue.fbx`) on a campus pedestal!
4. **Hole 4: The Double Bank (Par 2)**
   - Sharp 90-degree corner requiring precision banking off the wooden rail cushions directly toward the cup.
5. **Hole 5: The Hammer Gauntlet (Par 3)**
   - Features the animated swinging hammer obstacle (`HammerAnimationHolder 1`), speed boost pads, and narrow gaps.
6. **Hole 6: The Pipe Tunnel (Par 3)**
   - Pipe transport tunnel hole! Players putt into the pipe entrance, watching the ball travel across the gap and drop cleanly onto the putting green.
7. **Hole 7: The Split Fairway (Par 3)**
   - Two tactical paths: a high-speed narrow bridge with boost pads and no rails, or a safer curved banking path around the outside.
8. **Hole 8: The Bumper Maze (Par 3)**
   - Pinball-inspired obstacle course with cross and triangle bumpers, dynamic bounce pads, and sloped curves.
9. **Hole 9: Grand Finale: Panther's Roar (Par 4)**
   - Multi-tier championship finishing hole! Ascent ramp, elevated S-curve, and downhill roll into the grand sunken cup with victory fanfare, surrounded by celebratory flags and the Pitt Bradford Panther mascot monument!

## Gameplay Features

- **VR Golf Putter (`GolfPutter.cs`)**:
  - Grab/pickup with SteamVR controller hand via `Interactable`.
  - Velocity-based impulse physics striking the ball.
  - SteamVR haptic vibration pulse on ball impact.
  - Procedural sound effect for crisp golf ball impact.
- **Golf Ball Physics (`GolfBall.cs`)**:
  - Damped rolling resistance and clean threshold stopping.
  - Out of bounds and hazard detection with auto-respawn and penalty stroke.
- **Celebration Fanfare & Flag Animation (`GolfHole.cs`)**:
  - Sinking the putt plays a procedural 4-note victory chord fanfare and triggers spinning/waving flag animations.
- **Scorekeeping & Round Flow (`MiniGolfGameManager.cs`)**:
  - Tracks strokes across all 9 holes, computes golf terms (*Hole in One*, *Eagle*, *Birdie*, *Par*, *Bogey*), and automatically advances to the next tee area.
  - Grand Championship celebration upon Hole 9 completion with full 9-hole score summary.
- **In-VR Scoreboard (`ScoreboardUI.cs`)**:
  - In-world billboard styled in Pitt Royal Blue & Athletic Gold showing the live 9-hole scorecard table and current stroke counter.
- **Active Scene**:
  - Dedicated scene [`Assets/Scenes/MiniGolf_AlumniCourse.unity`](file:///c:/GitHub/Alumni-Weekend-2026/Alumni-Weekend-2026/GolfVR/GolfVR/Assets/Scenes/MiniGolf_AlumniCourse.unity) configured as default in `EditorBuildSettings.asset`.

## How to Play / Run in Unity

1. Open Unity Hub and open the project at `GolfVR/GolfVR` (Unity 2022.3 LTS or compatible).
2. Open the scene at `Assets/Scenes/MiniGolf_AlumniCourse.unity`.
3. Connect your SteamVR compatible headset (Meta Quest via Link/AirLink, HTC Vive, Valve Index, etc.).
4. Press **Play** in the Unity Editor to tee off on Hole 1!