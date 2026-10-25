using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using Object = UnityEngine.Object;

namespace Week12AdvancedXR.Editor
{
    /// <summary>
    /// The book's Chapter 8 hand- and eye-tracking scene (pp. 183-193), built in two steps because the
    /// imported samples must compile before the scene can use them:
    ///
    ///   1. Week 12 > Import Hand Tracking Samples - XRI Starter Assets, XR Device Simulator and Hands
    ///      Interaction Demo, plus the XR Hands HandVisualizer sample (book p. 184, steps 3-4).
    ///   2. Week 12 > Build Hands and Gaze Scene - turns on OpenXR with the Hand Interaction Profile and the
    ///      Hand Tracking Subsystem (pp. 184-185, steps 6-8), then builds Assets/Scenes/HandsAndGaze.unity:
    ///        XR Interaction Hands Setup  - hand-tracked XR Origin with a Gaze Interactor (head-gaze fallback)
    ///        Hand Tracking Cube          - XR Grab Interactable; pinch to grab it (p. 185)
    ///        Gaze Cube 1                 - gaze hover recolors it and shows a sphere (pp. 190-192)
    ///        Gaze Cube 2                 - its label reads Hovered / Selected / Deselected (pp. 192-193)
    ///
    /// Everything else is wired with Inspector events, as in the book; there is no scene-specific script.
    /// Without a headset, the XR Device Simulator appears in Play mode; press H to switch it to hands.
    /// </summary>
    public static class BuildHandsAndGazeScene
    {
        const string ScenePath = "Assets/Scenes/HandsAndGaze.unity";
        const string XriPackage = "com.unity.xr.interaction.toolkit";
        const string HandsPackage = "com.unity.xr.hands";

        [MenuItem("Week 12/Import Hand Tracking Samples")]
        public static void ImportSamples()
        {
            ImportSample(XriPackage, "Starter Assets");
            ImportSample(XriPackage, "XR Device Simulator");
            ImportSample(HandsPackage, "HandVisualizer");
            ImportSample(XriPackage, "Hands Interaction Demo");
            AssetDatabase.Refresh();
            Debug.Log("[Week12] Samples imported. When Unity finishes compiling, run Week 12 > Build Hands and Gaze Scene.");
        }

        [MenuItem("Week 12/Build Hands and Gaze Scene")]
        public static void Build()
        {
            var setupPrefab = FindAsset<GameObject>("XR Interaction Hands Setup t:Prefab");
            var simulatorPrefab = FindAsset<GameObject>("XR Device Simulator t:Prefab");
            if (setupPrefab == null || simulatorPrefab == null)
            {
                Debug.LogError("[Week12] Run Week 12 > Import Hand Tracking Samples first.");
                return;
            }

            ConfigureOpenXR();
            UseSimulatorInEditor(simulatorPrefab);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera")); // the rig brings its own camera

            var setup = (GameObject)PrefabUtility.InstantiatePrefab(setupPrefab);
            setup.transform.position = Vector3.zero;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground Floor";
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Ground Floor", new Color(0.36f, 0.38f, 0.42f));

            // Hand tracking (p. 185): a small cube to pinch and grab.
            var grabCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grabCube.name = "Hand Tracking Cube";
            grabCube.transform.position = new Vector3(0f, 1.25f, 1f);
            grabCube.transform.localScale = Vector3.one * 0.1f;
            grabCube.GetComponent<Renderer>().sharedMaterial = Mat("Hand Tracking Cube", new Color(0.13f, 0.6f, 0.55f));
            grabCube.AddComponent<XRGrabInteractable>();
            var body = grabCube.GetComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = true; // stays put until grabbed, as in the book

            // Eye/head gaze (pp. 190-193).
            var idle = Mat("Gaze Idle", new Color(0.75f, 0.2f, 0.18f));
            var hovered = Mat("Gaze Hovered", new Color(0.93f, 0.85f, 0.45f));
            BuildGazeCube1(new Vector3(-0.45f, 1.2f, 2.2f), idle, hovered);
            BuildGazeCube2(new Vector3(0.45f, 1.2f, 2.2f), idle, hovered);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Week12] Built " + ScenePath + ". Press Play, then H in the XR Device Simulator for hands.");
        }

        // Cube 1: First Hover Entered -> yellow + show the sphere; Last Hover Exited -> red + hide it.
        static void BuildGazeCube1(Vector3 position, Material idle, Material hovered)
        {
            var cube = GazeCube("Gaze Cube 1", position, idle, out var interactable);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Gaze Sphere";
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.position = position + Vector3.up * 0.35f;
            sphere.transform.localScale = Vector3.one * 0.18f;
            sphere.GetComponent<Renderer>().sharedMaterial = Mat("Gaze Sphere", new Color(0.15f, 0.4f, 0.95f));
            sphere.SetActive(false);

            var renderer = cube.GetComponent<Renderer>();
            AddMaterial(interactable.firstHoverEntered, renderer, hovered);
            UnityEventTools.AddBoolPersistentListener(interactable.firstHoverEntered, sphere.SetActive, true);
            AddMaterial(interactable.lastHoverExited, renderer, idle);
            UnityEventTools.AddBoolPersistentListener(interactable.lastHoverExited, sphere.SetActive, false);
        }

        // Cube 2: a label under the cube says Hovered, Selected, or Deselected.
        static void BuildGazeCube2(Vector3 position, Material idle, Material hovered)
        {
            var cube = GazeCube("Gaze Cube 2", position, idle, out var interactable);

            var canvas = new GameObject("Gaze Cube 2 Label", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = new Vector2(300f, 90f);
            rect.position = position + Vector3.down * 0.32f;
            rect.localScale = Vector3.one * 0.001f;
            var background = new GameObject("Background", typeof(Image)).GetComponent<Image>();
            background.rectTransform.SetParent(rect, false);
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.sizeDelta = Vector2.zero;
            background.color = new Color(0.55f, 0.12f, 0.1f, 0.95f);
            var label = new GameObject("Interactable Text", typeof(Text)).GetComponent<Text>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 44;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "";
            canvas.gameObject.SetActive(false);

            var renderer = cube.GetComponent<Renderer>();
            AddMaterial(interactable.firstHoverEntered, renderer, hovered);
            UnityEventTools.AddBoolPersistentListener(interactable.firstHoverEntered, canvas.gameObject.SetActive, true);
            AddText(interactable.firstHoverEntered, label, "Hovered");
            AddMaterial(interactable.lastHoverExited, renderer, idle);
            UnityEventTools.AddBoolPersistentListener(interactable.lastHoverExited, canvas.gameObject.SetActive, false);
            AddText(interactable.firstSelectEntered, label, "Selected");
            AddText(interactable.lastSelectExited, label, "Deselected");
        }

        static GameObject GazeCube(string name, Vector3 position, Material material, out XRSimpleInteractable interactable)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.3f;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            interactable = cube.AddComponent<XRSimpleInteractable>();
            interactable.allowGazeInteraction = true; // the book's Allow Gaze Interaction checkbox (p. 191)
            return cube;
        }

        // Inspector-style persistent listeners that call property setters (Renderer.sharedMaterial, Text.text).
        static void AddMaterial(UnityEventBase evt, Renderer renderer, Material material)
        {
            var setter = (UnityAction<Material>)Delegate.CreateDelegate(typeof(UnityAction<Material>), renderer, "set_sharedMaterial");
            UnityEventTools.AddObjectPersistentListener(evt, setter, material);
        }

        static void AddText(UnityEventBase evt, Text label, string value)
        {
            var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), label, "set_text");
            UnityEventTools.AddStringPersistentListener(evt, setter, value);
        }

        // Book pp. 184-185, steps 6-8: OpenXR loader for Windows, controller profiles (the lab's Vive Pro, Index,
        // Quest), the Hand Interaction Profile, Hand Interaction Poses, and the Hand Tracking Subsystem. Eye Gaze
        // Interaction lets eye-tracking headsets drive the Gaze Interactor; others fall back to head gaze.
        static void ConfigureOpenXR()
        {
            var group = BuildTargetGroup.Standalone;
            var perTarget = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
                .Select(g => AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault();
            if (perTarget == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var manager = perTarget.ManagerSettingsForBuildTarget(group);
            XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);

            // A freshly created OpenXR settings asset has no features until they are discovered.
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            foreach (var type in new[] { typeof(HTCViveControllerProfile), typeof(ValveIndexControllerProfile),
                         typeof(OculusTouchControllerProfile), typeof(HandInteractionProfile),
                         typeof(HandCommonPosesInteraction), typeof(HandTracking), typeof(EyeGazeInteraction) })
            {
                var feature = settings.GetFeature(type);
                if (feature != null) feature.enabled = true;
                else Debug.LogWarning("[Week12] OpenXR feature not found: " + type.Name);
            }
            EditorUtility.SetDirty(settings);
        }

        // Project Settings > XR Plug-in Management > XR Interaction Toolkit: use the simulator in Play mode
        // (Editor only), so the scene works without a headset and needs no simulator object of its own.
        static void UseSimulatorInEditor(GameObject simulatorPrefab)
        {
            var settingsType = typeof(XRDeviceSimulator).Assembly.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings");
            var instance = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null) as Object;
            if (instance == null)
            {
                Debug.LogWarning("[Week12] Couldn't load XR Device Simulator settings; enable the simulator in Project Settings > XR Plug-in Management > XR Interaction Toolkit.");
                return;
            }
            var so = new SerializedObject(instance);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = simulatorPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(instance);
        }

        static void ImportSample(string package, string displayName)
        {
            var sample = Sample.FindByPackage(package, null).FirstOrDefault(s => s.displayName == displayName);
            if (sample.displayName == null)
            {
                Debug.LogError($"[Week12] Sample '{displayName}' not found in {package}.");
                return;
            }
            if (!sample.isImported)
                sample.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
        }

        static T FindAsset<T>(string filter) where T : Object
        {
            var guid = AssetDatabase.FindAssets(filter).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
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
    }
}
