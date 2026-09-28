# Week 4 Lesson Plan: VR Development in Unity I (SteamVR setup)

**Syllabus:** Ch. 3 (first half). We use SteamVR instead of the XR Interaction Toolkit (XRI); Week 4's
README maps each textbook step to its SteamVR equivalent.
**Student handout:** [`Week-04-SteamVR-Setup/README.md`](../Week-04-SteamVR-Setup/README.md)
**Deck:** *Week 5 Virtual Reality SteamVR Setup, the Interaction System and GolfVR*, from the title
through "Common first-day problems".

## Objectives

By the end of class, every student can:

1. Name the three jobs VR adds to a Unity app: rendering, tracking, input. *(O1)*
2. Say what OpenVR and OpenXR each are, and why the SteamVR plugin needs the **OpenVR loader first**. *(O1)*
3. Create a Built-In pipeline project with the SteamVR plugin, and set up XR Plug-in Management. *(O3)*
4. Explain the difference between a SteamVR **action** and a controller button, and generate the action classes. *(O3)*
5. Run `Interactions_Example`, in a headset or with the 2D fallback. *(O3)*

## Before class

- [ ] Download `steamvr_2_8_0.unitypackage` once to a shared folder, so 20 students don't each download it on lab Wi-Fi.
- [ ] Do the whole setup yourself on one lab PC this week, with the Unity version the lab has installed.
- [ ] Open [`Alumni-Weekend-2026/GolfVR/GolfVR`](../Alumni-Weekend-2026/GolfVR/GolfVR) on the demo PC: it is the "finished" example you'll compare against.
- [ ] Write the three loader states on the board (see minute 25).

## Timeline (150 min)

| Min | Segment | What you do | What students do |
| --- | --- | --- | --- |
| 0–10 | Review (Ch. 1–2) | Three quick questions: "Collider vs trigger?", "What does a light probe do?", "What does the Transform hold?" | Answer on cards |
| 10–20 | **Why SteamVR?** | Slides "What does VR development involve?" and "Headset approaches". Hold up a Vive wand and a Quest controller | Note which SDK runs on which headset |
| 20–30 | **Runtimes and loaders** | Board sketch: Unity → *loader* → *runtime* → headset. Three states: OpenVR only ✅ · OpenVR then OpenXR ✅ (GolfVR) · OpenXR first ❌ (the SteamVR plugin's input goes quiet) | Predict what breaks in each |
| 30–40 | **Live demo: steps 1–3** | New Built-In project → import plugin → **Accept All** → XR Plug-in Management → OpenVR first. Say every click aloud | Watch; don't follow along yet |
| 40–50 | **Actions, not buttons** | Slide "SteamVR Input: the default action set". Open the SteamVR Input window → *Save and generate* → show `SteamVR_Actions.default_GrabPinch` in code | Find the "Teleport" action and say which button it is on the Vive wand |
| 50–55 | Lab briefing + safety | Read the safety list in [the overview](README.md#safety-and-comfort-read-aloud-in-week-4-repeat-in-week-5). Pairs; rotation timer | Form pairs |
| 55–125 | **Lab** | Circulate. Look for the five first-day problems (below). Sign off "sample runs" per pair | Follow README §4, steps 1–14 |
| 125–140 | **Share-out** | Two pairs: "what broke and how did you fix it?" Put fixes on the board | Add fixes to their write-ups |
| 140–150 | Exit ticket | "Why does the SteamVR plugin need the OpenVR loader first?" + "What's one action and its Vive binding?" | Hand in |

## Demo script: the four clicks everyone gets wrong

1. **Template:** "3D **(Built-In Render Pipeline)**, not URP. Here's why." Show a URP project with the
   pink Interaction System materials. A 10-second screenshot is enough.
2. **Accept All.** "This window only appears once. If you close it, the recommended settings are gone."
3. **Loader order.** Tick OpenVR *and* OpenXR, and drag OpenVR to the top. "GolfVR is set up exactly like this:
   `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` lists OpenVRLoader, then OpenXRLoader."
4. **Save and generate.** Before and after: code using `SteamVR_Actions.default_GrabPinch` doesn't
   compile, then does.

## Discussion prompts (with answers)

| Ask | Listen for |
| --- | --- |
| "The textbook uses XRI. Why might a lab choose SteamVR instead?" | The Vive Pro 2s run SteamVR; the Interaction System comes with hands, teleport and grab; GolfVR already uses it |
| "What happens if the Quest user and the Vive user share the same code?" | Actions + a bindings file per controller: the code doesn't change |
| "Why build in Built-In, not URP?" | The Interaction System's materials use Built-In shaders |

## Common problems during lab

| Symptom | Fix | Prevent next time |
| --- | --- | --- |
| Game view works, headset is black | OpenVR loader unchecked or below OpenXR | Board sketch at minute 20 |
| Hands appear, no button works | Skipped *Save and generate* | Put it on the "done" checklist |
| Everything pink | URP project | Demo click 1 |
| Missing `actions.json` errors | Delete `StreamingAssets/SteamVR`, re-copy the example JSONs | — |
| Quest shows the Meta home screen | Connect Link first, then start SteamVR | Tape a note on the Quest station |

## Formative checks

- **Minute 30:** thumbs up/down: "OpenXR first: will the SteamVR hands work?" (No.)
- **Minute 50:** each pair names the action for teleport and its button.
- **Lab sign-off:** instructor or TA sees `Interactions_Example` running (headset or 2D fallback).

## Homework (from the syllabus): setup write-up, 10 points

| Criterion | Pts |
| --- | --- |
| Environment filled in (Unity version, plugin version, headset, pipeline) | 2 |
| Steps listed in order, in their own words | 3 |
| Loader order and stereo mode stated, with the reason for OpenVR first | 2 |
| One action + its binding, and one rebind idea with a reason | 2 |
| Screenshot of the sample running; pushed to GitHub | 1 |

The template is at the bottom of the Week 4 README.

## Extension (fast finishers)

- Open the binding UI and rebind *Teleport* to the grip; test it; put it back.
- Open GolfVR's `actions.json`, then find where `QuickResetController.cs` reads `SnapTurnRight`. What does GolfVR use it for?
