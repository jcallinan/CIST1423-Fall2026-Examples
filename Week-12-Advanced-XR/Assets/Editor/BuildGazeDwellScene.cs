using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Week12AdvancedXR.Editor
{
    /// <summary>
    /// Menu item that builds the Week 12 head-gaze dwell scene (Assets/Scenes/GazeDwell.unity), following
    /// the README: an XR Origin (VR) from the XR Interaction Toolkit, three GazeDwellTargets in front of the
    /// camera, and GazeDwellInteractor on the XR camera with a radial progress ring on a World Space canvas
    /// parented to that camera. Re-running it overwrites the scene, materials, and ring sprite.
    ///
    /// For a desktop test without a headset, select Main Camera and enable Use Mouse Position.
    /// </summary>
    public static class BuildGazeDwellScene
    {
        const string ScenePath = "Assets/Scenes/GazeDwell.unity";
        const string RingPath = "Assets/Textures/DwellRing.png";

        [MenuItem("Week 12/Build Gaze Dwell Scene")]
        public static void Build()
        {
            EnsureFolder("Scenes");
            EnsureFolder("Materials");
            EnsureFolder("Textures");
            var ring = BuildRingSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera"));

            if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)") || Object.FindObjectOfType<XROrigin>() == null)
            {
                Debug.LogError("[Week12] XR Interaction Toolkit's GameObject/XR/XR Origin (VR) menu item is missing. Install XRI 2.5.4.");
                return;
            }
            var origin = Object.FindObjectOfType<XROrigin>();
            origin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var camera = origin.Camera;
            camera.nearClipPlane = 0.05f;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Floor", new Color(0.3f, 0.32f, 0.36f));

            // Targets at roughly standing eye height (XR Origin camera height is 1.1176 m without a headset).
            Target(PrimitiveType.Sphere, "Gaze Target Sphere", new Vector3(-1.1f, 1.15f, 2.6f), 0.5f);
            Target(PrimitiveType.Cube, "Gaze Target Cube", new Vector3(0f, 1.15f, 3f), 0.45f);
            Target(PrimitiveType.Cylinder, "Gaze Target Cylinder", new Vector3(1.1f, 1.15f, 2.6f), 0.4f);

            // Progress ring: World Space canvas 1 m in front of the camera, so it stays centered in view.
            var canvas = new GameObject("Dwell Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.SetParent(camera.transform, false);
            canvasRect.localPosition = new Vector3(0f, 0f, 1f);
            canvasRect.sizeDelta = new Vector2(100f, 100f);
            canvasRect.localScale = Vector3.one * 0.001f;
            var indicator = new GameObject("Dwell Indicator", typeof(Image)).GetComponent<Image>();
            indicator.rectTransform.SetParent(canvasRect, false);
            indicator.rectTransform.sizeDelta = new Vector2(80f, 80f);
            indicator.sprite = ring;
            indicator.type = Image.Type.Filled;
            indicator.fillMethod = Image.FillMethod.Radial360;
            indicator.fillOrigin = (int)Image.Origin360.Top;
            indicator.fillClockwise = true;
            indicator.fillAmount = 0f;
            indicator.color = new Color(1f, 0.72f, 0.18f);
            indicator.raycastTarget = false;

            var interactor = camera.gameObject.AddComponent<GazeDwellInteractor>();
            var so = new SerializedObject(interactor);
            so.FindProperty("gazeCamera").objectReferenceValue = camera;
            so.FindProperty("dwellDuration").floatValue = 1.2f;
            so.FindProperty("dwellIndicator").objectReferenceValue = indicator;
            so.FindProperty("useMousePosition").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Week12] Built " + ScenePath + ". Look at a target for 1.2 s to toggle it.");
        }

        static void Target(PrimitiveType type, string name, Vector3 position, float size)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = type == PrimitiveType.Cylinder ? new Vector3(size, size * 0.5f, size) : Vector3.one * size;
            go.GetComponent<Renderer>().sharedMaterial = Mat("Gaze Target", Color.white);
            go.AddComponent<GazeDwellTarget>();
        }

        // 256 px anti-aliased ring, imported as a Sprite for the filled Image.
        static Sprite BuildRingSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float outer = size * 0.5f - 2f, inner = outer - size * 0.12f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                float alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            File.WriteAllBytes(RingPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RingPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RingPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        }

        static Material Mat(string name, Color color)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + name)) AssetDatabase.CreateFolder("Assets", name);
        }
    }
}
