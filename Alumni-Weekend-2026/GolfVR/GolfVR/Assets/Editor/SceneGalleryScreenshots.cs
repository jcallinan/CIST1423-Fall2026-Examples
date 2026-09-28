using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Renders a gallery of screenshots of every scene in the app, entirely headless
    /// (temp camera -> RenderTexture -> JPG), into Docs/Screenshots/Scenes/&lt;Scene&gt;/,
    /// and writes Docs/SCENES.md listing them. Generic shots (overview from two sides,
    /// top-down, the player's start view) are auto-framed from the scene's renderers;
    /// the golf scenes add a tee view per hole; the driving range adds down-range views
    /// and simulated drives with their flight paths drawn.
    /// Run: unity run &lt;project&gt; -- -executeMethod GolfVR.EditorTools.SceneGalleryScreenshots.Run
    /// </summary>
    public static class SceneGalleryScreenshots
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const int W = 1280, H = 720;

        private static readonly string[] Scenes = { "ICARUS_v1", "ICARUS_TwoHole_v1", "ICARUS_DrivingRange_v1", "New_Sample", "Dom_v4" };
        private static readonly Dictionary<string, string> Blurb = new Dictionary<string, string>
        {
            { "ICARUS_v1", "The full 9-hole course (press 1). The app's start scene." },
            { "ICARUS_TwoHole_v1", "Holes 1 and 2 only, invisible fence, invisible walls around the fairways (press 2)." },
            { "ICARUS_DrivingRange_v1", "Driving range: driver, flight trails, distance history (press 5)." },
            { "New_Sample", "Sample / experimental scene (press 3)." },
            { "Dom_v4", "Dom's scene (press 4)." },
        };

        private static string _outRoot;
        private static readonly Dictionary<string, List<string>> Shots = new Dictionary<string, List<string>>();

        [MenuItem("Tools/GolfVR/Capture Scene Gallery")]
        public static void Run()
        {
            _outRoot = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Screenshots", "Scenes");
            Shots.Clear();

            foreach (string name in Scenes)
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogWarning("[Gallery] missing scene " + path);
                    continue;
                }

                Debug.Log("[Gallery] === " + name);
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Physics.SyncTransforms();
                Directory.CreateDirectory(Path.Combine(_outRoot, name));
                Shots[name] = new List<string>();

                try
                {
                    if (name == "ICARUS_DrivingRange_v1") RangeShots(name);
                    else GenericShots(name);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Gallery] {name} failed: {e}");
                }
            }

            WriteIndex();
            Debug.Log("[Gallery] done: " + _outRoot);
        }

        // ---------------------------------------------------------------- generic

        private static Bounds SceneBounds()
        {
            bool have = false;
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer r in Object.FindObjectsOfType<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                string n = r.name.ToLower();
                if (n.Contains("sky") || n.Contains("cloud") || n.Contains("floor")) continue;
                if (r.bounds.size.magnitude > 150f) continue;
                if (!have) { b = r.bounds; have = true; }
                else b.Encapsulate(r.bounds);
            }
            if (!have) b = new Bounds(Vector3.zero, new Vector3(20f, 5f, 20f));
            return b;
        }

        private static void GenericShots(string name)
        {
            Bounds b = SceneBounds();
            Vector3 c = b.center;
            float r = Mathf.Max(b.extents.x, b.extents.z, 4f);
            Debug.Log($"[Gallery] {name} bounds center {c} size {b.size}");

            int n = 1;
            Shot(name, ref n, "overview_south_east", c + new Vector3(r * 1.0f, r * 0.85f, -r * 1.25f), c, 55f);
            Shot(name, ref n, "overview_north_west", c + new Vector3(-r * 1.0f, r * 0.85f, r * 1.25f), c, 55f);
            Shot(name, ref n, "top_down", c + new Vector3(0f, r * 2.1f + b.extents.y, -0.01f), c, 50f);

            // The player's start view: first hole's tee (golf scenes) or the VR rig.
            Vector3 eye; Vector3 fwd;
            GolfHole first = null;
            foreach (GolfHole h in Object.FindObjectsOfType<GolfHole>()) if (first == null || h.holeNumber < first.holeNumber) first = h;
            if (first != null && first.playerTeeLocation != null)
            {
                eye = first.playerTeeLocation.position + Vector3.up * 1.7f;
                fwd = first.playerTeeLocation.forward;
            }
            else
            {
                Camera vr = null;
                foreach (Camera cam in Object.FindObjectsOfType<Camera>()) if (cam.name == "VRCamera" || cam.CompareTag("MainCamera")) { vr = cam; break; }
                if (vr != null && vr.gameObject.activeInHierarchy) { eye = vr.transform.position + Vector3.up * 1.6f; fwd = vr.transform.forward; }
                else { eye = new Vector3(c.x, 1.7f, c.z - r); fwd = Vector3.forward; }
            }
            Shot(name, ref n, "start_view", eye, eye + fwd * 10f + Vector3.down * 0.4f, 80f);
            Shot(name, ref n, "start_view_looking_left", eye, eye + Quaternion.Euler(0f, -70f, 0f) * fwd * 10f, 80f);
            Shot(name, ref n, "start_view_looking_right", eye, eye + Quaternion.Euler(0f, 70f, 0f) * fwd * 10f, 80f);

            // One tee view per golf hole.
            var holes = new List<GolfHole>(Object.FindObjectsOfType<GolfHole>());
            holes.Sort((x, y) => x.holeNumber.CompareTo(y.holeNumber));
            foreach (GolfHole h in holes)
            {
                if (h.playerTeeLocation == null) continue;
                Vector3 e = h.playerTeeLocation.position + Vector3.up * 1.5f;
                Vector3 look = h.transform.position + Vector3.up * 0.2f;
                Shot(name, ref n, $"hole_{h.holeNumber}_tee_view", e, look, 70f);
            }
        }

        // ---------------------------------------------------------------- driving range

        private static void RangeShots(string name)
        {
            int n = 1;
            DrivingRangeManager mgr = Object.FindObjectOfType<DrivingRangeManager>();
            DriverClub club = Object.FindObjectOfType<DriverClub>();
            Invoke(mgr, "Awake");
            Invoke(mgr, "Start");
            Physics.SyncTransforms();

            Vector3 tee = mgr.teePoint.position;
            Shot(name, ref n, "tee_view", new Vector3(0f, 1.65f, 0f), tee + new Vector3(0f, 0.5f, 25f), 80f);
            Shot(name, ref n, "tee_closeup_ball_and_driver", new Vector3(0.6f, 1.3f, -0.6f), tee + new Vector3(-0.2f, 0.4f, 0f), 60f);
            Shot(name, ref n, "look_left_history_board_and_buttons", new Vector3(0f, 1.6f, 0f), new Vector3(-1.4f, 1.3f, 1.8f), 80f);
            Shot(name, ref n, "look_right_buttons", new Vector3(0f, 1.6f, 0f), new Vector3(1.9f, 1.0f, 0.8f), 80f);
            Shot(name, ref n, "downrange_markers", new Vector3(0f, 6f, -8f), new Vector3(0f, 8f, 250f), 55f);
            Shot(name, ref n, "aerial_from_behind", new Vector3(30f, 45f, -55f), new Vector3(0f, 0f, 150f), 60f);

            // Simulated drives: three swings, each ball's flight recorded and drawn as a coloured path.
            var paths = new List<List<Vector3>>();
            var colors = new[] { new Color(1f, 0.35f, 0.25f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.9f, 0.2f) };
            float[] clubSpeeds = { 16f, 26f, 36f };
            SimulationMode previous = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                for (int s = 0; s < clubSpeeds.Length; s++)
                {
                    RangeBall ball = null;
                    foreach (RangeBall b in Object.FindObjectsOfType<RangeBall>()) if (b.state == RangeBall.State.Ready) ball = b;
                    if (ball == null) { mgr.SpawnNext(); foreach (RangeBall b in Object.FindObjectsOfType<RangeBall>()) if (b.state == RangeBall.State.Ready) ball = b; }
                    if (ball == null) break;

                    Vector3 dir = new Vector3(0.03f * (s - 1), 0.05f, 1f).normalized;
                    Vector3 start = ball.transform.position - dir * 1.2f;
                    float dt = 1f / 90f;
                    Vector3 prev = start;
                    RangeBall hit = null;
                    for (int i = 0; i < 400 && hit == null; i++)
                    {
                        Vector3 cur = start + dir * (clubSpeeds[s] * dt * (i + 1));
                        Physics.SyncTransforms();
                        hit = club.TrySweepHit(prev, cur, dt);
                        prev = cur;
                    }
                    if (hit == null) continue;

                    var path = new List<Vector3> { ball.transform.position };
                    for (int step = 0; step < 4000 && ball.state != RangeBall.State.Done; step++)
                    {
                        ball.PhysicsTick(0.02f);
                        Physics.Simulate(0.02f);
                        if (step % 2 == 0) path.Add(ball.transform.position);
                    }
                    path.Add(ball.transform.position);
                    paths.Add(path);
                    Debug.Log($"[Gallery] range drive {s + 1}: club {clubSpeeds[s]} m/s -> carry {ball.carryDistance:F0} m, total {ball.totalDistance:F0} m");
                    mgr.SpawnNext();
                }
            }
            finally
            {
                Physics.simulationMode = previous;
            }

            // draw the flights
            var trailMat = new Material(Shader.Find("Sprites/Default"));
            var parent = new GameObject("GalleryFlights");
            for (int i = 0; i < paths.Count; i++)
            {
                var go = new GameObject("Flight" + i);
                go.transform.SetParent(parent.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = trailMat;
                lr.startColor = lr.endColor = colors[i % colors.Length];
                lr.widthMultiplier = 0.25f;
                lr.positionCount = paths[i].Count;
                lr.SetPositions(paths[i].ToArray());
            }

            Shot(name, ref n, "three_drives_side_view", new Vector3(-120f, 40f, 80f), new Vector3(0f, 8f, 90f), 65f);
            Shot(name, ref n, "three_drives_from_behind", new Vector3(0f, 12f, -22f), new Vector3(0f, 10f, 110f), 70f);
            Shot(name, ref n, "three_drives_top_down", new Vector3(0f, 230f, 100f), new Vector3(0f, 0f, 100.1f), 55f);

            // the history board after the shots
            if (mgr.historyText != null)
            {
                Vector3 bp = mgr.historyText.transform.position;
                Vector3 back = -mgr.historyText.transform.forward;
                Shot(name, ref n, "history_board_after_three_drives", bp + back * 2.4f, bp, 55f);
            }

            Object.DestroyImmediate(parent);
        }

        // ---------------------------------------------------------------- helpers

        private static void Shot(string scene, ref int index, string label, Vector3 pos, Vector3 lookAt, float fov)
        {
            string file = $"{index:00}_{label}.jpg";
            string full = Path.Combine(_outRoot, scene, file);

            var camGo = new GameObject("__GalleryCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            Vector3 d = lookAt - pos;
            cam.transform.rotation = Quaternion.LookRotation(d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.Skybox;

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            RenderTexture prev = RenderTexture.active;
            try
            {
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(full, tex.EncodeToJPG(88));
                Shots[scene].Add(file);
            }
            finally
            {
                RenderTexture.active = prev;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
            }
            index++;
        }

        private static void WriteIndex()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Scene gallery");
            sb.AppendLine();
            sb.AppendLine("Screenshots of every scene, rendered headlessly by `Tools/GolfVR/Capture Scene Gallery` (`GolfVR.EditorTools.SceneGalleryScreenshots.Run`). Regenerate them after changing a scene.");
            sb.AppendLine("The 1-5 keys switch between the scenes while the app is running.");
            sb.AppendLine();
            foreach (string name in Scenes)
            {
                if (!Shots.ContainsKey(name)) continue;
                sb.AppendLine($"## {name}");
                sb.AppendLine();
                if (Blurb.ContainsKey(name)) sb.AppendLine(Blurb[name]).AppendLine();
                foreach (string file in Shots[name])
                {
                    string title = Path.GetFileNameWithoutExtension(file).Substring(3).Replace('_', ' ');
                    sb.AppendLine($"**{title}**  ");
                    sb.AppendLine($"![{name} {title}](Screenshots/Scenes/{name}/{file})");
                    sb.AppendLine();
                }
            }
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Docs", "SCENES.md"), sb.ToString());
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo m = target.GetType().GetMethod(name, PrivateInstance);
            if (m != null) m.Invoke(target, null);
        }
    }
}
