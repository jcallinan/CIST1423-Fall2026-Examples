using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PantherCamCast;

namespace PantherCamCastEditor
{
    /// <summary>
    /// Menu item that builds the bare-bones Panther Cam & Cast starter scene out of primitives --
    /// same ground+prop+object pattern as the Week 2 primitives lab. Idempotent: skips objects
    /// that already exist, safe to re-run after a script change (same convention Rhodium's
    /// Setup menu uses).
    /// </summary>
    public static class BuildPantherCamCastScene
    {
        [MenuItem("Panther Cam Cast/Build Starter Scene")]
        public static void Build()
        {
            var root = GameObject.Find("PantherCamCast_Starter");
            if (root == null)
            {
                root = new GameObject("PantherCamCast_Starter");
            }

            // --- Dock (ground) ---
            var dock = FindOrCreate(root.transform, "Dock", PrimitiveType.Cube);
            dock.transform.localPosition = new Vector3(0, -0.25f, -2f);
            dock.transform.localScale = new Vector3(3f, 0.5f, 4f);

            // --- Water (pond) ---
            var water = FindOrCreate(root.transform, "Water", PrimitiveType.Plane);
            water.transform.localPosition = new Vector3(0, -0.2f, 4f);
            water.transform.localScale = new Vector3(3f, 1f, 3f);
            var rend = water.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.15f, 0.35f, 0.55f, 0.85f);
                rend.sharedMaterial = mat;
            }

            // --- Panther placeholder (swap for the scanned model later) ---
            var trophy = FindOrCreate(root.transform, "Panther_PLACEHOLDER", PrimitiveType.Cube);
            trophy.transform.localPosition = new Vector3(-1.5f, 0.5f, -2f);
            trophy.transform.localScale = new Vector3(0.6f, 1f, 1.2f);
            var trophyRend = trophy.GetComponent<Renderer>();
            if (trophyRend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.72f, 0.53f, 0.04f); // bronze/gold stand-in
                trophyRend.sharedMaterial = mat;
            }

            // --- Rod pivot + bobber ---
            var rodPivot = FindOrCreateEmpty(root.transform, "RodPivot");
            rodPivot.transform.localPosition = new Vector3(0.8f, 1f, -1f);

            var bobber = FindOrCreate(root.transform, "Bobber", PrimitiveType.Sphere);
            bobber.transform.localScale = Vector3.one * 0.15f;
            bobber.transform.localPosition = new Vector3(0, -0.2f, 4f);

            // --- Gameplay controller (lives on the pivot) ---
            var controller = rodPivot.GetComponent<CastAndReelController>();
            if (controller == null) controller = rodPivot.AddComponent<CastAndReelController>();

            var bobberScript = rodPivot.GetComponent<Bobber>();
            if (bobberScript == null) bobberScript = rodPivot.AddComponent<Bobber>();
            bobberScript.bobberVisual = bobber.transform;

            var input = rodPivot.GetComponent<FishingRodInput>();
            if (input == null) rodPivot.AddComponent<FishingRodInput>();

            // --- Camera ---
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera", typeof(Camera));
                camGo.tag = "MainCamera";
            }
            camGo.transform.position = new Vector3(0, 2f, -6f);
            camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Panther Cam & Cast starter scene built. Press Space to cast, hold Left Mouse to reel once the bobber dips.");
        }

        static GameObject FindOrCreate(Transform parent, string name, PrimitiveType type)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject FindOrCreateEmpty(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
