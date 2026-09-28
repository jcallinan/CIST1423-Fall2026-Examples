# Week 12 Lesson Plan: Advanced XR (hands, gaze and multiplayer)

**Syllabus:** Ch. 8: hand tracking with XRI; eye- and head-gaze; a VR multiplayer app with PUN
(Network Manager, avatar hands and face, animated hand models).
**Deck:** *Week 12 Virtual Reality Advanced XR - Hands, Gaze and Multiplayer*.
**Code:** [`Week-07-WebXR-WaterWorks`](../Week-07-WebXR-WaterWorks): `server/rooms.mjs`, `src/network.js`, `src/avatars.js`

Students saw WaterWorks as *users* in Week 7. This week they read it as *engineers*, and compare it
with the textbook's PUN app.

## Objectives

By the end of class, every student can:

1. Name the four problems every multiplayer XR app solves: **meeting** (rooms), **authority** (who controls an object), **sync** (positions over time), **presence** (showing people). *(O1)*
2. Explain ownership with a concrete race: two people grab the same part. *(O3)*
3. Explain why clients interpolate between 15 Hz updates instead of teleporting objects. *(O1)*
4. Map PUN concepts (PhotonView ownership, PhotonTransformView, avatar prefab, buffered state) to WaterWorks. *(O1)*
5. Implement head-gaze dwell selection, and name the "Midas touch" problem. *(O3)*

## Before class

- [ ] `npm start` in WaterWorks on the lab PC; test with two laptops and one headset.
- [ ] Prepare 20 index cards for the unplugged activity: 10 "part" cards (Pipe, Pump, …), plus "GRAB", "MOVE" and "RELEASE" cards.
- [ ] In Chrome DevTools → Network → **WS**, confirm you can see the messages (`pose`, `move`) flowing. You'll project this.

## Timeline (150 min)

| Min | Segment | What you do | What students do |
| --- | --- | --- | --- |
| 0–10 | Review (Ch. 7) | "Spatial Blend 1 means?" "Why vary pitch on repeated sounds?" | Answer |
| 10–25 | **Hands and gaze** | Deck: hand tracking table; "Head gaze + dwell" code; the Midas touch problem | Try the "look at it for 1.2 s" dwell rule with a partner pointing at objects in the room |
| 25–45 | **Unplugged: be the network** | Activity below | Act as clients and server |
| 45–60 | **Four problems** | Deck: "Multiplayer, live" and "PUN vs our relay" | Fill in the four-problem table for PUN and for WaterWorks |
| 60–75 | **Live: look inside the wire** | Two laptops + a headset in one room. Project DevTools → WS frames: `welcome`, `pose` ×15/s, `grab`, `move`, `grab-denied` | Call out each message type as it appears |
| 75–85 | **Code reading** | Deck: "The server rule that makes it fair". Then `network.js` `_receive`, the `grab-denied` case | Pairs: trace what happens on the loser's screen |
| 85–90 | Lab briefing | Options below | Pick one |
| 90–135 | **Lab** | Circulate | Build one advanced feature |
| 135–145 | Share-out | 3 demos | Show |
| 145–150 | Exit ticket | "Two people grab the same pipe at the same moment. Who gets it, and what does the other see?" | Hand in |

## Unplugged activity: be the network (20 min)

Teaches authority, latency and late joiners without any code.

1. **Setup.** One student is the **server**, at the front with the part cards. Four students are **clients**
   at the corners of the room. Clients may only communicate by passing written notes via "network" runners
   (two more students). Runners walk slowly: that's latency.
2. **Round 1, no rules.** Two clients both write "I GRAB Pump" at the same time. The server has no rule
   and says yes to both. *Ask:* "Who has the pump?" (Both think they do: that's the bug.)
3. **Round 2, ownership.** New server rule: the first GRAB note to arrive wins; the loser gets a
   "DENIED, Ana has it" note back. Replay. *Ask:* "Why does the server decide, not the clients?"
4. **Round 3, late joiner.** A new client arrives mid-game. *Ask:* "What's the minimum the server must
   send them?" (Everyone present + every part with its current pose: the `welcome` snapshot.)
5. **Round 4, rate.** Runners can only carry 15 notes a second (pretend). *Ask:* "The pump moves smoothly in
   the headset but updates only 15×/s. How?" (Interpolation: `workspace.smooth()` lerps between updates.)

Put the four problem names on the board as they come up: **meeting, authority, sync, presence.**

## Lab options (pick one; all satisfy the syllabus "prototype one advanced feature")

| Option | Where | Done when |
| --- | --- | --- |
| **A. Gaze dwell** (Unity) | New script from the deck's `GazeDwell`, in the Week 5 Playground | Looking at the target for 1.2 s resets its score; a ring fills while you look |
| **B. Hand input** (Unity) | SteamVR skeleton: read finger curl from `Hand.skeleton` | Making a fist over the push-button presses it |
| **C. Multiplayer feature** (WaterWorks) | `src/`, plus a test in `test/` | One of: an undo message, a "who placed this" label, or a room-wide "clear" confirmation |
| **D. PUN comparison** (Unity + Photon, free tier) | Textbook Ch. 8 | Two players see each other's avatar heads; a one-page comparison with WaterWorks |

## Discussion prompts (with answers)

| Ask | Listen for |
| --- | --- |
| "Why not let every client just send its own positions and trust everyone?" | Conflicts (two holders), cheating, and no single truth for late joiners |
| "Why 15 Hz and not 90 Hz like the display?" | Bandwidth; smooth motion comes from interpolation, not from the send rate |
| "What's the cost of the server deciding?" | A round trip before you know you won, so the client grabs optimistically and handles `grab-denied` |

## Homework (from the syllabus), 10 points

| Criterion | Pts |
| --- | --- |
| A working advanced feature (option A–D), shown in a clip or screenshots | 4 |
| Markdown write-up: approach, what you tried, what worked or didn't | 3 |
| The four problems (meeting, authority, sync, presence) mapped to your feature, or why N/A | 2 |
| Pushed to GitHub | 1 |

## Extension

- WaterWorks: add a server-side save and load of a room's layout (JSON).
- Measure the real round-trip time: add a `ping`/`pong` message and show the milliseconds in the people list.
