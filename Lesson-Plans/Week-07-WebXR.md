# Week 7 Lesson Plan: WebXR Building (replaces AR Development in Unity II)

**Why the change:** AR II's topics (plane detection, touch placement, anchors, testing, deploying) all
exist in WebXR. One WebXR build also runs on every device in the lab: Quest passthrough, a Vive Pro 2
through SteamVR, and laptops. **Exam 2 (Ch. 3–4) stays in this week.**

**Decks:** *Week 7 … WebXR Part 1 (Sneaker Builder → Panther Customizer)* and *Week 7 … WebXR Part 2
(Building WaterWorks)*.
**Student materials:** [`Meta-WebXR-Samples/Sneaker-Builder.md`](../Meta-WebXR-Samples/Sneaker-Builder.md) ·
[`panther-builder/FROM-SNEAKER-TO-PANTHER.md`](../Alumni-Weekend-2026/panther-builder/FROM-SNEAKER-TO-PANTHER.md) ·
[`Week-07-WebXR-WaterWorks/README.md`](../Week-07-WebXR-WaterWorks/README.md)

| Part 1: read someone else's app | Part 2: build and share one |
| --- | --- |
| ![panther-builder](../Alumni-Weekend-2026/panther-builder/docs/panther-builder-gold.png) | ![WaterWorks multiplayer](../Week-07-WebXR-WaterWorks/docs/waterworks-multiplayer.png) |

## Objectives

By the end of class, every student can:

1. Map AR Foundation's plane detection / raycast / anchor to WebXR's `plane-detection` / `hit-test` / `anchors`. *(O1)*
2. Explain the ECS pattern in Meta's samples: components are data, systems are logic, registration order is execution order. *(O2)*
3. Separate an app's **engine layer** from its **product layer**, using the Sneaker → Panther → WaterWorks forks as evidence. *(O2, O4)*
4. Run a multi-device WebXR session in the lab and add parts together. *(O3)*
5. Add one new part type (a builder function + ports) and see it work in the shelf, palette and snapping. *(O2)*

## Make Part 1 pre-reading (assign in Week 6)

The exam takes 35 minutes of this class, so move the reading out of it:

> **Due before Week 7:** read `Meta-WebXR-Samples/Sneaker-Builder.md` and answer its four reading
> questions (one paragraph each). Skim `FROM-SNEAKER-TO-PANTHER.md` §1–2. Try the live Panther
> Customizer on your phone or laptop: <https://jcallinan.github.io/Alumni-Weekend-2026/>

## Before class

- [ ] Lab PC: `cd Week-07-WebXR-WaterWorks && npm install && npm test` (expect 25 passing tests). Then `npm start` and note the printed `https://<IP>:8443` URL.
- [ ] Check a Quest can reach that URL on the lab Wi-Fi (guest networks often block device-to-device traffic). Accept the certificate once on every device.
- [ ] Vive Pro 2 station: SteamVR running, then Chrome/Edge at the same URL → **Build in VR**.
- [ ] Write the URL on the board as a QR code (any QR generator).
- [ ] Backup: [`docs/`](../Week-07-WebXR-WaterWorks/docs) screenshots, and two laptops in one room as the fallback demo.

## Timeline (150 min)

| Min | Segment | What you do | What students do |
| --- | --- | --- | --- |
| 0–5 | Settle | Collect Part 1 reading answers | — |
| 5–40 | **Exam 2** (Ch. 3–4) | — | Exam |
| 40–60 | **Part 1, compressed** | Deck Part 1: "Why WebXR", the ECS slide, "Sneaker → Panther: the scorecard", "AR → VR fallback". Cold-call from the reading answers | Answer: "Which 5 files didn't change, and why?" |
| 60–70 | **AR II → WebXR** | Deck Part 2 "Why WebXR instead of AR II" table | Fill in the AR Foundation ↔ WebXR column from memory first |
| 70–80 | **Live demo** | `npm start`, open on the projector laptop, then a Quest, then a Vive. Build tank → valve → pump live; show green ports | Watch the snap maths slide right after |
| 80–85 | Lab briefing | Teams of 3, **each on a different device** (headset, second headset or laptop, laptop). One room per team: `?room=team1` | Form teams |
| 85–135 | **Lab: build a water system** | Circulate. Sign off when a team has tank → pump → hydrant connected (all green rings) | Build, screenshot, then the stretch task |
| 135–145 | Share-out | Two teams: "who built what, what snapped, what didn't" | Show |
| 145–150 | Exit ticket | "What is one engine-layer file and one product-layer file in WaterWorks?" | Hand in |

## Demo script (minutes 70–80)

1. **Terminal:** `npm test`. Point at the relay test: "Two clients race for a pump; the second is denied. That's multiplayer fairness in one test."
2. **Laptop:** open `https://<IP>:8443`. Point at the palette, then click **Storage Tank**, then **Gate Valve**. Drag the valve near the tank's outlet: it snaps, and the rings turn green.
3. **Quest:** same URL → **Build in Mixed Reality**. The laptop's avatar appears as a floating head. Reach into the shelf and pull out a **pump**. The laptop sees it glide as it moves.
4. **Race:** the laptop and the Quest grab the same pipe at once. One wins, and the other's grab is refused. Show `server/rooms.mjs` `case 'grab'`.
5. **Valve:** point at the red wheel and click. It drops and darkens on every device.

## Lab task (teams of 3)

1. Everyone joins `?room=teamN`, each on a different device.
2. Build **tank → gate valve → Y-strainer → pump → riser → elbow → pipe → check valve → meter → tee (+ pressure gauge) → pipe → elbow → hydrant**. The WaterWorks README's close-up shows the first part of the run.
3. Screenshot from two devices. Note who placed which parts.
4. **Stretch (pick one):** add a new part type to `src/parts.js` (reducer, 45° elbow, backflow preventer): write a builder with correct `ports`, add it to `PART_TYPES`, and run `npm test`. The port tests check your new part automatically.

## Discussion prompts (with answers)

| Ask | Listen for |
| --- | --- |
| "panther-builder changed 10 of 15 files. Why were the other 5 safe to leave?" | They're the engine: input, pointer, follow, audio, loaders. They never mention sneakers |
| "Why did the Vive Pro 2 need a code change to enter the Panther app?" | Upstream *required* AR features that only the Quest Browser grants. It now falls back to `immersive-vr` |
| "What does the server know that clients don't?" | Who holds each part, and the latest poses; that's what lets late joiners catch up |

## Common problems during lab

| Symptom | Fix |
| --- | --- |
| Headset can't load the page | Same Wi-Fi as the server? Use the `https://` URL with **:8443**, and accept the certificate |
| "Offline (single player)" | The server isn't running, or the page came from `npm run serve` without `npm run relay` |
| Parts don't snap | Ports must be within 12 cm **and** roughly facing each other; turn the part (R/T/E, or your wrist) |
| Can't grab a part | Someone else is holding it. That's the ownership rule working |
| Vive: no "Build in VR" button | Start SteamVR *before* opening the browser, and use Chrome or Edge |

## Homework, 10 points

| Criterion | Pts |
| --- | --- |
| Working connected system (all listed parts, green ports), with screenshots from 2 devices | 4 |
| Write-up: who built what; one thing that snapped well, one that didn't and why | 3 |
| Engine vs product: list 2 files of each in WaterWorks, with one-line reasons | 2 |
| Pushed to GitHub | 1 |
| *(Bonus +2)* New part type with passing tests | +2 |

## Extension

- Exercises 1–5 in the WaterWorks README (flow check, pipe sizes, undo, persistence, hit-test shelf placement).
- Fork `panther-builder` with your own campus scan, and list the files you didn't have to touch.
