# CIST 1423 Lesson Plans (Fall 2026)

Instructor-facing plans for teaching the examples in this repo. They follow the syllabus's class
format (5–10 min review → ~30 min topic discussion → lab/homework time) and its weekly topics. There
is one change: **Week 7 teaches WebXR building instead of AR Development in Unity II.**

> **Assumed session length: 150 minutes** (Wednesdays, 6:00 PM, VR Lab, Room 113). The syllabus gives
> the start time only. If your block is shorter, cut the "Extension" segments first, then shorten lab time.
> Never cut the exit ticket.

| Plan | Covers | Deck(s) in `Slide-Decks/` | Code |
| --- | --- | --- | --- |
| [Week 4: SteamVR setup](Week-04-SteamVR-Setup.md) | Ch. 3a: VR project setup, OpenVR vs OpenXR, SteamVR Input | Week 5 SteamVR deck, first half (to "Common first-day problems") | [`Week-04-SteamVR-Setup`](../Week-04-SteamVR-Setup) |
| [Week 5: the SteamVR Playground](Week-05-SteamVR-Playground.md) | Ch. 3b: Interaction System, teleport, grab, buttons, levers, dials | Week 5 SteamVR deck, second half (from "Touring Interactions_Example") | [`Week-05-SteamVR-Interactions`](../Week-05-SteamVR-Interactions) |
| [Week 7: WebXR building](Week-07-WebXR.md) | Replaces AR II: WebXR, ECS, Sneaker Builder → Panther → WaterWorks | Week 7 Part 1 + Part 2 | [`Meta-WebXR-Samples`](../Meta-WebXR-Samples), [`panther-builder`](../Alumni-Weekend-2026/panther-builder), [`Week-07-WebXR-WaterWorks`](../Week-07-WebXR-WaterWorks) |
| [Week 12: multiplayer](Week-12-Multiplayer.md) | Ch. 8 (multiplayer part): ownership, sync, late joiners, PUN ↔ our relay | Week 12 deck | [`Week-07-WebXR-WaterWorks`](../Week-07-WebXR-WaterWorks) |

The other weeks' decks carry their plan in the speaker notes. The semester overview below gives
each week's objectives and assessment.

---

## Semester at a glance

Course outcomes (syllabus SLOs): **O1** explain VR hardware and interaction concepts · **O2** build Unity
scenes (GameObjects, prefabs, physics, C#) · **O3** implement VR interactions (locomotion, object
manipulation) · **O4** use GitHub for version control and collaboration · **O5** design, build, test and
present an original environment.

| Wk | Topic | By the end, students can… | Outcomes | Assessed by |
| --- | --- | --- | --- | --- |
| 1 | Intro, XR concepts (Ch. 1) | Define VR/AR/MR; set up Unity Hub + GitHub | O1, O4 | One-page concept in Markdown |
| 2 | Editor & Scene I (Ch. 2a) | Build a primitive scene; choose a pipeline | O2 | Basic scene + README |
| 3 | Editor & Scene II (Ch. 2b) | Use colliders, materials, prefabs, Asset Store, lighting and probes | O2 | Extended scene; **Exam 1** |
| 4 | VR I: SteamVR setup | Configure an OpenVR project and SteamVR Input; run the sample | O1, O3 | Setup write-up |
| 5 | VR II: Interaction System | Teleport, grab/throw, press, slide, turn; read actions in code | O3 | Playground test write-up |
| 6 | AR I: AR Foundation (Ch. 4a) | Explain marker vs markerless; place a cube with AR Foundation | O1, O2 | AR scene + reflection |
| 7 | **WebXR building** (replaces AR II) | Read an ECS WebXR app; fork it; run a multi-device session | O2, O3, O4 | Team build + write-up; **Exam 2** |
| 8 | Midterm prototype | Demo a working prototype with a design log | O5 | Prototype + DESIGN-LOG.md |
| 9 | Interactive VR (Ch. 5) | Add a no-code and a C# interaction; diagram them | O2, O3 | Two interactions + UML |
| 10 | Interactive AR (Ch. 6) | Build place + swap menus | O2 | AR menu + reflection |
| 11 | Sound & VFX (Ch. 7) | Use spatial audio, particles, fog | O2 | Audio + particle; **Exam 3** |
| 12 | Advanced XR (Ch. 8) | Explain hand/gaze input and multiplayer sync | O1, O3 | One advanced feature + write-up |
| 13 | Best practices (Ch. 9) | Apply comfort/UX rules; review a PR | O4, O5 | Revised project + collaboration example |
| 14 | Testing & polish | Run a playtest; triage and fix | O5 | Playtest report |
| 15 | Final showcase | Present live | O5 | Showcase (20%) |
| 16 | Final exam | — | all | Final repo + exam |

## Every class, the same shape

| Min | Segment | Notes |
| --- | --- | --- |
| 0–10 | **Review** (syllabus: 5–10 min) | 3 questions on last week, cold-call or Kahoot-style; show one strong homework |
| 10–40 | **Topic** (syllabus: ~30 min) | Slides + one live demo. Stop every ~10 min for a quick check |
| 40–50 | **Lab briefing** | Goal, the "done looks like" picture, pairs, headset rotation order |
| 50–130 | **Lab** | Pairs: one in the headset, one at the keyboard; swap every 15 min |
| 130–145 | **Share-out** | 2–3 pairs show what worked or broke |
| 145–150 | **Exit ticket** | One card or form: "one thing I can do now / one thing I'm stuck on" |

### Headset rotation in the lab

Students outnumber headsets, so pair them and use a visible timer:

- **Driver** (in the headset): tests. **Navigator** (at the keyboard): edits, reads errors, writes the log. Swap every 15 minutes.
- **Stations without a headset** use the SteamVR 2D fallback (Weeks 4–5) or the browser desktop mode (Week 7). Nobody waits idle.
- **Keep a written queue** on the whiteboard for the Vive Pro 2 stations.

## Lab prep checklist (every week with headsets)

- [ ] SteamVR updated **the day before**, not at 5:55 PM. Turn off auto-update during class.
- [ ] Each Vive Pro 2: base stations green, wireless adapter paired, room setup valid.
- [ ] Unity **2022.3.62f3** on every lab PC (GolfVR's version). Project templates cached.
- [ ] For WebXR weeks: Node 20+, `npm install` already run in each example folder, lab Wi-Fi allows device-to-device traffic on port 8443.
- [ ] Sanitising wipes, disposable face covers, spare controller batteries.
- [ ] A tested fallback: screen recordings of each demo in case hardware fails.

## Safety and comfort (read aloud in Week 4, repeat in Week 5)

- **Guardian/chaperone set up** before anyone puts a headset on. The navigator watches the driver's cable and surroundings.
- **Motion sickness:** anyone may stop at any time, no questions. Teleport, not smooth locomotion, for beginners. Sit down if dizzy.
- **Photosensitivity:** warn before effects with flashing (GolfVR's fireworks). Offer a non-flashing option.
- **Hygiene:** wipe headsets and controllers between users.

## Supporting every student

| Situation | Plan |
| --- | --- |
| Can't use a headset (sickness, accessibility, absence) | Full credit via 2D fallback (Unity) or desktop mode (WebXR), plus a clip recorded by a partner |
| No Windows PC at home | Lab hours; the WebXR examples run on any laptop browser |
| Racing ahead | Each plan has an **Extension** list; invite them to review a classmate's PR |
| Stuck on setup | The "common problems" tables in the week READMEs; pair them with a finished pair |
