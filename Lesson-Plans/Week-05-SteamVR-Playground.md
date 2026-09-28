# Week 5 Lesson Plan: VR Development in Unity II (the SteamVR Playground)

**Syllabus:** Ch. 3 (second half): rig, teleport, grab, UI and buttons, gaze and climbing, testing.
**Student handout:** [`Week-05-SteamVR-Interactions/README.md`](../Week-05-SteamVR-Interactions/README.md)
**Deck:** *Week 5 Virtual Reality SteamVR Setup, the Interaction System and GolfVR*, from "Touring
Interactions_Example" to the end.

![The Playground's six stations](../Week-05-SteamVR-Interactions/docs/week05-playground-overview.png)

## Objectives

By the end of class, every student can:

1. Match each XRI concept in the textbook to its SteamVR component (Player, TeleportArea/Point,
   Interactable/Throwable, HoverButton, LinearDrive, CircularDrive). *(O3)*
2. Describe the four steps of a grab (hover → attach with flags → hold → release with velocity). *(O3)*
3. Use a drive's `LinearMapping` (0–1) to control something else from a script. *(O2, O3)*
4. Choose correctly between reacting to Interaction System **events** and reading an **action** yourself. *(O3)*
5. Test a VR scene in a headset *and* with the 2D fallback. *(O3)*

## Before class

- [ ] On the demo PC: Week 4 project + this week's `Assets/Scripts` and `Assets/Editor` → **SteamVR Playground → Build Test Scene** → save. Test all six stations in a headset.
- [ ] Mirror the headset view to the projector (SteamVR → *Display VR View*).
- [ ] Print or display the station card below at each lab station.
- [ ] Have GolfVR's `GolfPutter.cs` open in the IDE for the case-study segment.

## Timeline (150 min)

| Min | Segment | What you do | What students do |
| --- | --- | --- | --- |
| 0–10 | Review (Week 4) | "Name an action and its binding." "Why the OpenVR loader first?" Show one good setup write-up | Answer; fix their own write-ups |
| 10–30 | **Tour: Interactions_Example** | In the headset, projected. Stop at each station; the class calls out the XRI name, you name the SteamVR component (slide "XRI → SteamVR") | Fill in the mapping table in their notes |
| 30–40 | **How grabbing works** | Slide "How grabbing actually works". Demo: remove `TurnOnKinematic` from a Throwable and grab it through the table | Predict first: "What happens when a held cube meets the table?" |
| 40–50 | **Build the Playground** | Live: copy the folders, run the menu command, show the six stations and their signs (slide "What Build Test Scene produces") | Do the same on their machine |
| 50–55 | Lab briefing | Rotation: 12 minutes per station pair, in station order. Each pair fills in the station cards | — |
| 55–115 | **Lab: station rotation** | Circulate with the station guide below; sign off each station | Predict → try → explain → change (the station cards) |
| 115–130 | **Code reading: GolfPutter** | Slide "Reading GolfPutter.cs". Ask the two discussion questions | Pairs answer, then share |
| 130–145 | Share-out | Each pair shows their "change it" edit from one station | Show |
| 145–150 | Exit ticket | "Event or action: a *reset* button on the controller?" (action) · "What does `LinearMapping.value` hold?" (0–1 position of the drive) | Hand in |

## Station guide

Each station follows the same loop: **predict** (before touching) → **try** → **explain** (in the
Interaction System's own words) → **change it** (one edit, re-test).

### 1 · Grab & throw table
- **Predict:** "Will the can fly as fast as the ball if you throw with the same arm speed?" (Yes: `Throwable` copies the hand's velocity; mass doesn't matter for the release speed.)
- **Explain:** read `GrabReporter.cs`, the two `Interactable` events and `hand.TriggerHapticPulse`.
- **Change it:** make the haptic pulse three times longer; change the label to show the throw speed in km/h.

### 2 · Target
- **Predict:** "Does a cube *placed* against the board count as a hit?" (No: `minHitSpeed` 1.2 m/s filters out gentle contact.)
- **Explain:** why does `OnCollisionEnter` fire on the **board** when the board has no Rigidbody? (The thrown object has one; a collision needs just one Rigidbody.) This ties back to Week 3's collider table.
- **Change it:** 3 points for the bullseye: compare the hit point's distance from the board centre.

### 3 · Push button
- **Explain:** `HoverButton` moves `movingPart` as your hand pushes; `onButtonDown` is a `UnityEvent<Hand>`, so you get the hand for haptics.
- **Change it:** add a second listener *in the Inspector* (no code) that turns the target's sign off and on.

### 4 · Lever (LinearDrive)
- **Predict:** "The knob is in the middle. What scale is the statue?" (1.0×: `Lerp(0.4, 1.6, 0.5)`.)
- **Explain:** `LinearDrive` only writes a number (0–1) into `LinearMapping`; *our* script decides what it means. This is the textbook's Ch. 5 "scale with a slider", done with a physical lever.
- **Change it:** drive the statue's **colour** instead of its scale (`Color.Lerp`).

### 5 · Valve wheel (CircularDrive)
- **Predict:** "How many turns from shut to full flow?" (Two: `maxAngle = 720`.)
- **Explain:** the same `LinearMapping` idea, from rotation. `ValveFlow` sets the particle emission rate.
- **Change it:** make the stream stop suddenly below 10% open (a "stuck" valve), or add a looping water sound whose volume follows the flow.
- **Bridge:** this is the WaterWorks gate valve (Week 7), built with Unity instead of the web.

### 6 · Reset kiosk
- **Explain:** `ResettablePose` remembers start poses in `Awake`; the kiosk calls `ResetPose()` on all of them, but **skips anything in a hand**. Why? (Yanking an object out of someone's grip in VR is disorienting.)
- **Bridge:** GolfVR's staff kiosk resets the course between visitors without anyone removing a headset.

### Bonus · Menu button
- **Explain:** this is the only station that **reads an action** (`Menu`) instead of reacting to an event. When is that the right choice?

## Discussion prompts (with answers)

| Ask | Listen for |
| --- | --- |
| "Why estimate the putter's velocity by hand instead of reading its Rigidbody?" (GolfPutter) | A held object is kinematic, so its Rigidbody velocity isn't meaningful |
| "The textbook lists gaze and climbing. Where are they in SteamVR?" | Not built in: gaze = a raycast from the VR camera (Week 12); climbing needs a custom attachment |
| "What would make the Playground unsafe or uncomfortable?" | Throwing at other people, teleporting into walls, flashing effects |

## Common problems during lab

| Symptom | Likely cause |
| --- | --- |
| Can't grab anything | The hand hover sphere isn't reaching: the object has no collider, or SteamVR Input wasn't generated |
| Lever knob jumps to one end | `startPosition`/`endPosition` were swapped or moved after building. Re-run the builder |
| Wheel won't turn | Grabbing the hub, not the rim; `CircularDrive` uses the rim's box collider |
| No water | The valve is at 0%; turn it. Magenta water = the particle material is missing (re-run the builder in the Editor, not at runtime) |
| Labels mirrored or blank | Old builder version. Pull the latest and rebuild |

## Homework (from the syllabus): teleport + grab test, 10 points

| Criterion | Pts |
| --- | --- |
| Teleport: area and at least one pad, with a screenshot or clip | 2 |
| Grab/throw: GrabReporter Console lines pasted; a target score of 3 or more shown | 2 |
| Button, lever and valve each used, with one sentence each on what the script does | 3 |
| One "change it" edit from any station, with the diff or code snippet | 2 |
| README + code pushed | 1 |

## Extension

- Add a seventh station: a **drawer** (`LinearDrive` along Z) that reveals a hidden object.
- Use `SteamVR_Action_Vibration` directly for a pattern (three short pulses) when the target is hit.
- Make the Playground two-handed: the valve only turns while the other hand holds a "wrench" Throwable.
