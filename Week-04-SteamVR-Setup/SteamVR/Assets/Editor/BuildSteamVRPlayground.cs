using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valve.VR.InteractionSystem;
using SteamVRPlayground;

namespace SteamVRPlaygroundEditor
{
    /// <summary>
    /// Menu item that builds the Week 5 SteamVR Playground from primitives and the stock SteamVR
    /// Interaction System prefabs, so nothing is hand-placed. Re-running it deletes and rebuilds
    /// SteamVR_Playground, so it is always safe. Needs the SteamVR Unity Plugin (Week 4) in a Built-In render pipeline project.
    ///
    /// Stations (the player starts at the origin, looking +Z):
    ///   1 Grab & throw table    2 Throw target (score, particles, chime)
    ///   3 Push button (recolour) 4 Lever (LinearDrive scales a statue)
    ///   5 Valve wheel (CircularDrive controls a water stream)  6 Reset kiosk
    ///   + teleport area, three teleport pads, and the Menu button spawner
    /// </summary>
    public static class BuildSteamVRPlayground
    {
        const string PlayerPrefab = "Assets/SteamVR/InteractionSystem/Core/Prefabs/Player.prefab";
        const string TeleportingPrefab = "Assets/SteamVR/InteractionSystem/Teleport/Prefabs/Teleporting.prefab";
        const string TeleportPointPrefab = "Assets/SteamVR/InteractionSystem/Teleport/Prefabs/TeleportPoint.prefab";

        static readonly Color PittBlue = new Color32(0x00, 0x35, 0x94, 0xff);
        static readonly Color PittGold = new Color32(0xff, 0xb8, 0x1c, 0xff);
        static readonly Color Navy = new Color32(0x0e, 0x28, 0x41, 0xff);
        static readonly Color Slate = new Color(0.22f, 0.25f, 0.29f);
        static readonly Color Steel = new Color(0.55f, 0.58f, 0.62f);
        static readonly Color PipeBlue = new Color32(0x2f, 0x6f, 0xd6, 0xff);
        static readonly Color ValveRed = new Color32(0xc8, 0x34, 0x2b, 0xff);

        [MenuItem("SteamVR Playground/Build Test Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab) == null)
            {
                EditorUtility.DisplayDialog("SteamVR Playground",
                    "Couldn't find the SteamVR Interaction System. Import the SteamVR Unity Plugin first (see Week 4).",
                    "OK");
                return;
            }

            var old = GameObject.Find("SteamVR_Playground");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("SteamVR_Playground").transform;

            // The Player prefab brings its own VR camera; a leftover Main Camera would fight it.
            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null) Object.DestroyImmediate(mainCam);
            FindOrInstantiate(PlayerPrefab, "Player").transform.position = Vector3.zero;
            FindOrInstantiate(TeleportingPrefab, "Teleporting");
            SetUpSun();

            BuildGround(root);
            var target = BuildTarget(root, new Vector3(0f, 0f, 5.2f));
            BuildTable(root, new Vector3(0f, 0f, 1.3f));
            BuildColorButton(root, new Vector3(2.4f, 0f, 1.7f));
            BuildLever(root, new Vector3(-2.4f, 0f, 1.7f));
            BuildValve(root, new Vector3(-3.3f, 0f, 4.4f));
            BuildResetKiosk(root, new Vector3(3.3f, 0f, 4.4f), target);

            PlaceTeleportPoint(root, "TeleportPoint_Start", new Vector3(0f, 0f, -1.2f));
            PlaceTeleportPoint(root, "TeleportPoint_Valve", new Vector3(-2.1f, 0f, 3.2f));
            PlaceTeleportPoint(root, "TeleportPoint_Reset", new Vector3(2.1f, 0f, 3.2f));

            root.gameObject.AddComponent<MenuButtonSpawner>();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Week5] SteamVR Playground built (6 stations). Save the scene, put on the headset, press Play.");
        }

        // ---------------------------------------------------------------------------------------
        // Stations
        // ---------------------------------------------------------------------------------------

        static void BuildGround(Transform root)
        {
            var floor = Prim(root, "Floor", PrimitiveType.Plane, Vector3.zero, new Vector3(3f, 1f, 3f), Slate);
            floor.GetComponent<Renderer>().sharedMaterial.SetFloat("_Glossiness", 0.1f);

            // Teleport area: its own mesh just above the floor (Valve's sample does the same). At runtime
            // the Teleporting prefab swaps in its glowing material and only shows it while you aim.
            var area = Prim(root, "TeleportArea", PrimitiveType.Plane, new Vector3(0f, 0.005f, 1.8f), new Vector3(1.2f, 1f, 1.1f), Slate * 1.1f);
            area.AddComponent<TeleportArea>();
        }

        static void BuildTable(Transform root, Vector3 at)
        {
            var station = Station(root, "1_GrabAndThrow", at, PittBlue);
            Prim(station, "Table", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.8f, 0.6f), new Color(0.45f, 0.3f, 0.18f));
            MakeThrowable(station, "Cube", PrimitiveType.Cube, new Vector3(-0.45f, 0.86f, 0f), PittBlue);
            MakeThrowable(station, "Ball", PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0f), PittGold);
            MakeThrowable(station, "Can", PrimitiveType.Cylinder, new Vector3(0.45f, 0.9f, 0f), Color.white);
            Sign(station, new Vector3(-1.05f, 0f, 0.1f), "1  GRAB & THROW", "Trigger to grab. Throw at the target.");
        }

        static ThrowTarget BuildTarget(Transform root, Vector3 at)
        {
            var station = Station(root, "2_Target", at, PittGold);
            foreach (float x in new[] { -0.55f, 0.55f })
                Prim(station, "Post", PrimitiveType.Cylinder, new Vector3(x, 0.7f, 0.05f), new Vector3(0.06f, 0.7f, 0.06f), Steel);

            var board = new GameObject("Board");
            board.transform.SetParent(station, false);
            board.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var box = board.AddComponent<BoxCollider>();
            box.size = new Vector3(1.1f, 1.1f, 0.06f);
            float[] radii = { 1.0f, 0.72f, 0.46f, 0.22f };
            Color[] colors = { Color.white, ValveRed, Color.white, ValveRed };
            for (int i = 0; i < radii.Length; i++)
            {
                var ring = Prim(board.transform, "Ring" + i, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.012f * i), new Vector3(radii[i], 0.01f, radii[i]), colors[i]);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Object.DestroyImmediate(ring.GetComponent<Collider>()); // the board's box collider does the work
            }
            var target = board.AddComponent<ThrowTarget>();
            target.burst = ThrowTarget.CreateBurst(board.transform);
            UseParticleMaterial(target.burst);
            target.scoreLabel = Sign(station, new Vector3(1.25f, 0f, 0f), "2  TARGET", "Throw from the table.");
            target.scoreLabel.text = "TARGET\nHits: 0";
            return target;
        }

        static void BuildColorButton(Transform root, Vector3 at)
        {
            var station = Station(root, "3_PushButton", at, ValveRed);
            var button = MakeButton(station, "ColorButton", Vector3.zero, new Color(1f, 0.45f, 0.1f));
            var standIn = Prim(station, "Panther_PLACEHOLDER", PrimitiveType.Capsule, new Vector3(0.65f, 0.5f, 0.35f), new Vector3(0.4f, 0.5f, 0.4f), Color.white);
            button.AddComponent<PittColorButton>().target = standIn.GetComponent<Renderer>();
            Sign(station, new Vector3(0f, 0f, 0.6f), "3  PUSH BUTTON", "Push it down with your hand.");
        }

        static void BuildLever(Transform root, Vector3 at)
        {
            var station = Station(root, "4_Lever", at, new Color(0.18f, 0.55f, 0.34f));
            Prim(station, "Pedestal", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.9f, 0.3f), Navy);
            Prim(station, "Rail", PrimitiveType.Cube, new Vector3(0f, 0.93f, 0f), new Vector3(0.5f, 0.03f, 0.05f), Steel);
            var start = Empty(station, "LeverStart", new Vector3(-0.22f, 0.98f, 0f));
            var end = Empty(station, "LeverEnd", new Vector3(0.22f, 0.98f, 0f));

            var handle = Prim(station, "LeverHandle", PrimitiveType.Sphere, Vector3.Lerp(start.localPosition, end.localPosition, 0.5f), Vector3.one * 0.08f, PittGold);
            var mapping = handle.AddComponent<LinearMapping>();
            mapping.value = 0.5f;
            var drive = handle.AddComponent<LinearDrive>(); // adds the Interactable it requires
            drive.startPosition = start;
            drive.endPosition = end;
            drive.linearMapping = mapping;

            var plinth = Prim(station, "Plinth", PrimitiveType.Cylinder, new Vector3(-0.75f, 0.1f, 0.35f), new Vector3(0.5f, 0.1f, 0.5f), Steel);
            var statue = Prim(station, "Statue", PrimitiveType.Capsule, new Vector3(-0.75f, 0.2f, 0.35f), Vector3.one, PittBlue);
            // Pivot at the statue's feet so it grows upward from the plinth.
            var pivot = Empty(station, "StatuePivot", new Vector3(-0.75f, 0.2f, 0.35f));
            statue.transform.SetParent(pivot, false);
            statue.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            statue.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);

            var scaler = station.gameObject.AddComponent<LeverScaler>();
            scaler.mapping = mapping;
            scaler.target = pivot;
            scaler.label = Sign(station, new Vector3(0.2f, 0f, 0.6f), "4  LEVER", "Slide the gold knob.");
            scaler.label.text = "LEVER\nScale 1.0x";
            Object.DestroyImmediate(plinth.GetComponent<Collider>());
        }

        static void BuildValve(Transform root, Vector3 at)
        {
            var station = Station(root, "5_Valve", at, PipeBlue);
            Prim(station, "Riser", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.12f, 0.5f, 0.12f), PipeBlue);
            var spout = Prim(station, "Spout", PrimitiveType.Cylinder, new Vector3(0.25f, 0.8f, 0f), new Vector3(0.09f, 0.25f, 0.09f), PipeBlue);
            spout.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Prim(station, "Basin", PrimitiveType.Cylinder, new Vector3(0.5f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.5f), Steel);

            // Handwheel: a rim, four spokes and a hub, turned by CircularDrive around Y.
            var wheel = Empty(station, "Handwheel", new Vector3(0f, 1.08f, 0f));
            var rim = Prim(wheel, "Rim", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.36f, 0.015f, 0.36f), ValveRed);
            Object.DestroyImmediate(rim.GetComponent<Collider>());
            var grip = rim.AddComponent<BoxCollider>(); // a flat box is a better hand target than a squashed capsule
            grip.size = new Vector3(1f, 4f, 1f);
            for (int i = 0; i < 2; i++)
            {
                var spoke = Prim(wheel, "Spoke" + i, PrimitiveType.Cube, Vector3.zero, new Vector3(0.34f, 0.02f, 0.03f), ValveRed);
                spoke.transform.localRotation = Quaternion.Euler(0f, 90f * i, 0f);
                Object.DestroyImmediate(spoke.GetComponent<Collider>());
            }
            Prim(wheel, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.07f, 0.03f, 0.07f), Steel);

            var mapping = wheel.gameObject.AddComponent<LinearMapping>();
            var drive = wheel.gameObject.AddComponent<CircularDrive>(); // adds the Interactable it requires
            drive.axisOfRotation = CircularDrive.Axis_t.YAxis;
            drive.childCollider = grip;
            drive.linearMapping = mapping;
            drive.limited = true;
            drive.minAngle = 0f;
            drive.maxAngle = 720f; // two full turns from shut to open, like a real gate valve
            drive.rotateGameObject = true;

            var stream = new GameObject("WaterStream");
            stream.transform.SetParent(station, false);
            stream.transform.localPosition = new Vector3(0.5f, 0.8f, 0f);
            stream.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // emit straight down
            var ps = stream.AddComponent<ParticleSystem>();
            ValveFlow.ConfigureStream(ps);
            UseParticleMaterial(ps);

            var flow = station.gameObject.AddComponent<ValveFlow>();
            flow.mapping = mapping;
            flow.water = ps;
            flow.label = Sign(station, new Vector3(-0.35f, 0f, 0.6f), "5  VALVE", "Grab the red wheel and turn it.");
            flow.label.text = "VALVE\n0% open";
        }

        static void BuildResetKiosk(Transform root, Vector3 at, ThrowTarget target)
        {
            var station = Station(root, "6_Reset", at, Steel);
            var button = MakeButton(station, "ResetButton", Vector3.zero, ValveRed);
            button.AddComponent<ResetStation>().target = target;
            Sign(station, new Vector3(0f, 0f, 0.5f), "6  RESET", "Puts everything back.");
        }

        // ---------------------------------------------------------------------------------------
        // Pieces
        // ---------------------------------------------------------------------------------------

        /// <summary>A station root with a coloured floor pad, rotated to face the start point.</summary>
        static Transform Station(Transform root, string name, Vector3 at, Color padColor)
        {
            var station = Empty(root, name, at);
            var toPlayer = -at;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) station.rotation = Quaternion.LookRotation(-toPlayer.normalized, Vector3.up);
            var pad = Prim(station, "FloorPad", PrimitiveType.Cylinder, new Vector3(0f, 0.004f, 0f), new Vector3(1.9f, 0.004f, 1.9f), Color.Lerp(Slate, padColor, 0.35f));
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            return station;
        }

        /// <summary>A sign on a post: title in Pitt gold, one instruction line. Returns the title TextMesh.</summary>
        static TextMesh Sign(Transform station, Vector3 local, string title, string instruction)
        {
            var sign = Empty(station, "Sign", local);
            Prim(sign, "Post", PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0.03f), new Vector3(0.04f, 0.75f, 0.04f), Steel);
            var board = Prim(sign, "Board", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0.03f), new Vector3(0.9f, 0.42f, 0.03f), Navy);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            var titleMesh = Text(sign, "Title", new Vector3(0f, 1.74f, 0f), title, 0.011f, PittGold);
            Text(sign, "Instruction", new Vector3(0f, 1.56f, 0f), instruction, 0.006f, Color.white);
            return titleMesh;
        }

        static GameObject MakeButton(Transform station, string name, Vector3 local, Color capColor)
        {
            Prim(station, name + "_Pedestal", PrimitiveType.Cube, local + new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.9f, 0.3f), Navy);
            var button = Empty(station, name, local + new Vector3(0f, 0.92f, 0f)).gameObject;
            var cap = Prim(button.transform, "Cap", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.14f, 0.02f, 0.14f), capColor);
            var hover = button.AddComponent<HoverButton>(); // adds the Interactable it requires
            hover.movingPart = cap.transform;
            hover.localMoveDistance = new Vector3(0f, -0.03f, 0f);
            return button;
        }

        static void MakeThrowable(Transform station, string name, PrimitiveType type, Vector3 local, Color color)
        {
            var go = Prim(station, name, type, local, Vector3.one * 0.12f, color);
            go.AddComponent<Throwable>(); // adds Interactable + Rigidbody
            go.AddComponent<ResettablePose>();
            var reporter = go.AddComponent<GrabReporter>();
            reporter.label = Text(station, name + "_Label", local + new Vector3(0f, 0.25f, 0f), name + "\nready", 0.01f, Color.white);
        }

        static void PlaceTeleportPoint(Transform root, string name, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeleportPointPrefab);
            var point = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            point.name = name;
            point.transform.position = pos;
        }

        /// <summary>A ParticleSystem made from code in batch mode has no material (renders magenta);
        /// give it Unity's built-in particle material, which only the Editor can look up.</summary>
        static void UseParticleMaterial(ParticleSystem ps)
        {
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        }

        static void SetUpSun()
        {
            var sun = Object.FindObjectOfType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        static GameObject FindOrInstantiate(string prefabPath, string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            go.name = name;
            return go;
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
            return go;
        }

        static Transform Empty(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go.transform;
        }

        /// <summary>A TextMesh with a real font (one added from code has none, and draws nothing).
        /// TextMesh reads correctly from its -Z side, which is the side facing the player here.</summary>
        static TextMesh Text(Transform parent, string name, Vector3 local, string text, float size, Color color)
        {
            var go = Empty(parent, name, local).gameObject;
            var mesh = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            else
                Debug.LogWarning($"SteamVR Playground: the Editor's built-in font (LegacyRuntime.ttf) failed to load, " +
                    $"so sign \"{name}\" will build without visible text. The rest of the station still works. " +
                    "Restart the Editor and rebuild the scene to retry.");
            mesh.font = font;
            mesh.text = text;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return mesh;
        }
    }
}
