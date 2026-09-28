using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Captures a fixed set of documentation screenshots in one headless run
    /// (a fresh SceneCameraCapture call per shot would reopen the scene and
    /// lose any state forced for a later shot, e.g. the banner text or the
    /// night sky). Nothing here is saved back to the scene file -- these are
    /// read-only captures for docs.
    /// </summary>
    public static class DocumentationScreenshots
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static string _outDir;

        [MenuItem("Tools/GolfVR/Capture Documentation Screenshots")]
        public static void Run()
        {
            _outDir = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Screenshots");
            Directory.CreateDirectory(_outDir);

            EditorSceneManager.OpenScene("Assets/Scenes/ICARUS_v1.unity", OpenSceneMode.Single);

            MiniGolfGameManager manager = Object.FindObjectOfType<MiniGolfGameManager>();
            GolfBall ball = Object.FindObjectOfType<GolfBall>();
            GolfPutter putter = Object.FindObjectOfType<GolfPutter>();

            foreach (GolfBall b in Object.FindObjectsOfType<GolfBall>())
            {
                Invoke(b, "Awake");
                Invoke(b, "Start");
            }
            if (putter != null) Invoke(putter, "Awake");
            foreach (GolfHole h in manager.holes)
            {
                if (h != null) Invoke(h, "Awake");
            }
            Invoke(manager, "Awake");
            Invoke(manager, "Start");

            // 1. Course overview, 3/4 aerial angle.
            Shot("01_course_overview", new Vector3(24f, 20f, -26f), new Vector3(1f, 0f, 0f), 55f);

            // 2. Entrance/welcome sign, from the spawn-facing side.
            Shot("02_entrance_sign", new Vector3(-10f, 2f, -8f), new Vector3(-10f, 2.5f, -13f), 50f);

            // 3. Instructions sign + Hole 1 tee sign together. Framed off to
            // the side (not a straight line through the scoreboard, which
            // sits almost exactly on the direct path and reads backwards
            // from behind).
            Shot("03_instructions_and_hole1_sign", new Vector3(-8f, 2.4f, -6f), new Vector3(-12.5f, 1.2f, -10f), 55f);

            // 4. Festive bunting over the entrance plaza.
            Shot("04_festive_entrance_plaza", new Vector3(-8f, 3f, -4f), new Vector3(-15f, 2f, -11f), 60f);

            // 5. Scoreboard + "HOLE COMPLETE" banner. Sinks hole 1 for the
            // shot (via the real DebugSinkCurrentHole path), then pumps just
            // far enough for the banner text to be set.
            CaptureScoreboardShot(manager);

            // 6. Night sky + fireworks show (forced state, not a real fade).
            CaptureFireworksShot(manager);

            Debug.Log($"[DocShots] All screenshots saved to {_outDir}");
        }

        private static void CaptureScoreboardShot(MiniGolfGameManager manager)
        {
            manager.DebugSinkCurrentHole();

            GolfHole hole = manager.holes[0];

            Transform teeLoc = hole.playerTeeLocation;
            Vector3 camPos = teeLoc.position + Vector3.up * 1.2f;
            Vector3 boardPos = teeLoc.position + teeLoc.right * 1.8f + Vector3.up * 1.0f;
            Shot("05_scoreboard_and_banner", camPos, boardPos, 60f);
        }

        private static void CaptureFireworksShot(MiniGolfGameManager manager)
        {
            GameObject fireworksGo = GameObject.Find("FireworksCelebrationController");
            if (fireworksGo == null)
            {
                Debug.LogWarning("[DocShots] FireworksCelebrationController not found, skipping night/fireworks shot.");
                return;
            }
            FireworksCelebrationController controller = fireworksGo.GetComponent<FireworksCelebrationController>();
            Invoke(controller, "Awake");

            RenderSettings.skybox = controller.nightSkybox;
            if (RenderSettings.sun != null) RenderSettings.sun.intensity *= 0.03f;
            RenderSettings.ambientIntensity *= 0.15f;
            DynamicGI.UpdateEnvironment();

            MethodInfo spawnBurst = typeof(FireworksCelebrationController).GetMethod("SpawnFireworkBurst", PrivateInstance);
            Vector3 center = controller.burstAreaCenter + Vector3.up * controller.burstHeight;
            spawnBurst.Invoke(controller, new object[] { center + new Vector3(-4f, 0f, 2f) });
            spawnBurst.Invoke(controller, new object[] { center + new Vector3(3f, 1f, -3f) });
            spawnBurst.Invoke(controller, new object[] { center + new Vector3(0f, -1f, 5f) });

            foreach (ParticleSystem ps in Object.FindObjectsOfType<ParticleSystem>())
            {
                if (ps.gameObject.name == "FireworkBurst") ps.Simulate(0.6f, true, true);
            }

            Shot("06_night_fireworks_show", new Vector3(1.35f, 6f, -30f), new Vector3(1.35f, 10f, 0.18f), 60f);
        }

        private static void Shot(string name, Vector3 pos, Vector3 lookAt, float fov)
        {
            int width = 1280, height = 720;
            GameObject camGo = new GameObject("__TempCaptureCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                string path = Path.Combine(_outDir, name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[DocShots] Saved {path}");
            }
            finally
            {
                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
            }
        }

        private static void Invoke(object target, string methodName)
        {
            if (target == null) return;
            MethodInfo m = target.GetType().GetMethod(methodName, PrivateInstance);
            if (m == null) return;
            m.Invoke(target, null);
        }
    }
}
