using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace InteractiveAR.Editor
{
    /// <summary>
    /// Menu item that builds the Week 10 primitive prefabs and both scenes, following the README:
    ///
    ///   Assets/Scenes/InteractiveAR.unity                - device scene: AR Session, XR Origin (Mobile AR)
    ///                                                      with AR Plane Manager + AR Raycast Manager
    ///   Assets/Scenes/InteractiveAR_EditorFallback.unity - no AR managers; mouse clicks hit a ground
    ///                                                      plane at Y 0 so the loop can be tested in the Editor
    ///
    /// Both scenes share the Swap / Delete / Reset / Planes UI and the Cube, Sphere, Cylinder prefabs.
    /// Re-running it overwrites the scenes, prefabs, and materials.
    /// </summary>
    public static class BuildInteractiveARScenes
    {
        const string ARScenePath = "Assets/Scenes/InteractiveAR.unity";
        const string FallbackScenePath = "Assets/Scenes/InteractiveAR_EditorFallback.unity";

        [MenuItem("Week 10/Build Interactive AR Scenes")]
        public static void Build()
        {
            EnsureFolder("Scenes");
            EnsureFolder("Prefabs");
            EnsureFolder("Materials");

            GameObject[] variants =
            {
                BuildVariant("Cube", PrimitiveType.Cube, new Vector3(0.15f, 0.15f, 0.15f), new Color(0.0f, 0.21f, 0.58f)),
                BuildVariant("Sphere", PrimitiveType.Sphere, new Vector3(0.15f, 0.15f, 0.15f), new Color(1f, 0.72f, 0.11f)),
                BuildVariant("Cylinder", PrimitiveType.Cylinder, new Vector3(0.1f, 0.1f, 0.1f), new Color(0.8f, 0.2f, 0.17f)),
            };

            BuildARScene(variants);
            BuildFallbackScene(variants);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ARScenePath, true),
                new EditorBuildSettingsScene(FallbackScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Week10] Built " + ARScenePath + " and " + FallbackScenePath + ".");
        }

        static void BuildARScene(GameObject[] variants)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddSun();

            // AR Foundation's own menu items create the same objects students get from GameObject > XR.
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
            var origin = Object.FindObjectOfType<XROrigin>();
            if (Object.FindObjectOfType<ARSession>() == null || origin == null)
            {
                Debug.LogError("[Week10] AR Foundation menu items did not create an AR Session and XR Origin.");
                return;
            }

            var planeManager = origin.gameObject.GetComponent<ARPlaneManager>() ?? origin.gameObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = BuildPlanePrefab();
            var raycastManager = origin.gameObject.GetComponent<ARRaycastManager>() ?? origin.gameObject.AddComponent<ARRaycastManager>();
            var arCamera = origin.Camera;
            arCamera.tag = "MainCamera";

            BuildController(variants, raycastManager, planeManager, arCamera);
            EditorSceneManager.SaveScene(scene, ARScenePath);
        }

        static void BuildFallbackScene(GameObject[] variants)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddSun();

            // Camera looks down at the Y 0 fallback plane (README section 3).
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1f, -1.2f), Quaternion.Euler(40f, 0f, 0f));

            // Visual stand-in for a tabletop. Its collider is removed: the controller places objects with a
            // math plane at Fallback Ground Height, not by hitting this mesh.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Tabletop (visual only)";
            ground.transform.localScale = new Vector3(0.25f, 1f, 0.2f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Tabletop", new Color(0.55f, 0.42f, 0.3f));

            BuildController(variants, null, null, camera);
            EditorSceneManager.SaveScene(scene, FallbackScenePath);
        }

        static void BuildController(GameObject[] variants, ARRaycastManager raycastManager, ARPlaneManager planeManager, Camera camera)
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var status = new GameObject("Status", typeof(Image)).GetComponent<Image>();
            status.color = new Color(0f, 0f, 0f, 0.55f);
            var statusRect = status.rectTransform;
            statusRect.SetParent(canvas.transform, false);
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.sizeDelta = new Vector2(0f, 110f);
            var statusText = new GameObject("Status Text", typeof(Text)).GetComponent<Text>();
            statusText.rectTransform.SetParent(statusRect, false);
            statusText.rectTransform.anchorMin = Vector2.zero;
            statusText.rectTransform.anchorMax = Vector2.one;
            statusText.rectTransform.sizeDelta = new Vector2(-40f, 0f);
            statusText.font = Font;
            statusText.fontSize = 40;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.white;
            statusText.text = "Tap a detected plane to place an object.";

            var buttons = new Button[4];
            string[] labels = { "Swap", "Delete", "Reset", "Planes" };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = DefaultControls.CreateButton(UiResources()).GetComponent<Button>();
                button.name = labels[i];
                var rect = (RectTransform)button.transform;
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2((i + 0.5f) / labels.Length, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(220f, 110f);
                rect.anchoredPosition = new Vector2(0f, 40f);
                var text = button.GetComponentInChildren<Text>();
                text.text = labels[i];
                text.font = Font;
                text.fontSize = 40;
                buttons[i] = button;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var controller = new GameObject("PlacementController").AddComponent<ARPlacementController>();
            var so = new SerializedObject(controller);
            so.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            so.FindProperty("planeManager").objectReferenceValue = planeManager;
            so.FindProperty("arCamera").objectReferenceValue = camera;
            var prefabs = so.FindProperty("objectPrefabs");
            prefabs.arraySize = variants.Length;
            for (int i = 0; i < variants.Length; i++) prefabs.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
            so.FindProperty("swapButton").objectReferenceValue = buttons[0];
            so.FindProperty("deleteButton").objectReferenceValue = buttons[1];
            so.FindProperty("resetButton").objectReferenceValue = buttons[2];
            so.FindProperty("togglePlanesButton").objectReferenceValue = buttons[3];
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Prefab root sits on the surface; the primitive child is lifted so it rests on the plane instead of
        // sinking halfway into it. The selection highlight scales the root, so objects grow from the surface.
        static GameObject BuildVariant(string name, PrimitiveType type, Vector3 scale, Color color)
        {
            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(type);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = scale;
            float height = type == PrimitiveType.Cylinder ? scale.y * 2f : scale.y;
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = Mat(name, color);
            root.AddComponent<ARPlaceable>();

            string path = "Assets/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildPlanePrefab()
        {
            const string path = "Assets/Prefabs/AR Plane.prefab";
            GameObject plane = null;
            if (EditorApplication.ExecuteMenuItem("GameObject/XR/AR Default Plane"))
                plane = Selection.activeGameObject;
            if (plane == null || plane.GetComponent<ARPlane>() == null)
            {
                plane = new GameObject("AR Plane", typeof(ARPlane), typeof(ARPlaneMeshVisualizer),
                    typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(LineRenderer));
            }
            plane.name = "AR Plane";

            // Translucent, unlit fill so the camera feed stays visible through detected planes.
            var fill = Mat("AR Plane", new Color(1f, 0.72f, 0.11f, 0.3f), "Sprites/Default");
            plane.GetComponent<MeshRenderer>().sharedMaterial = fill;
            var line = plane.GetComponent<LineRenderer>();
            line.sharedMaterial = Mat("AR Plane Edge", Color.white, "Sprites/Default");
            line.startWidth = line.endWidth = 0.005f;
            line.startColor = line.endColor = new Color(1f, 0.72f, 0.11f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(plane, path);
            Object.DestroyImmediate(plane);
            return prefab;
        }

        static void AddSun()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static Material Mat(string name, Color color, string shader = "Standard")
        {
            string path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        };

        static void EnsureFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + name)) AssetDatabase.CreateFolder("Assets", name);
        }
    }
}
