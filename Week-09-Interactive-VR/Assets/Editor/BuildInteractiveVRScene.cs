using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menu item that builds the Week 9 test scene (Assets/Scenes/InteractiveVR.unity) from primitives,
/// following the README steps. Re-running it overwrites the scene, materials, clips, and controller.
///
///   Color Target  - sphere with ColorToggleTarget, toggled by XRRayColorInteractor
///                   (controller trigger, mouse click in the Game view, or Space)
///   Animated Prop - cube whose Animator plays Pulse when the UI Button's On Click() list calls
///                   Animator.SetTrigger("Pulse"); no gameplay script is attached to the button
/// </summary>
public static class BuildInteractiveVRScene
{
    const string ScenePath = "Assets/Scenes/InteractiveVR.unity";
    const string MaterialsFolder = "Assets/Materials";
    const string AnimationsFolder = "Assets/Animations";
    const float PropScale = 0.25f;

    [MenuItem("Week 9/Build Interactive VR Scene")]
    public static void Build()
    {
        EnsureFolder("Scenes");
        EnsureFolder("Materials");
        EnsureFolder("Animations");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Object.DestroyImmediate(GameObject.Find("Main Camera"));

        // Environment: floor, back wall, and a table in front of the starting camera.
        Primitive(PrimitiveType.Plane, "Floor", Vector3.zero, Vector3.one, Mat("Floor", new Color(0.36f, 0.38f, 0.42f)));
        Primitive(PrimitiveType.Cube, "Back Wall", new Vector3(0f, 1.5f, 4f), new Vector3(10f, 3f, 0.2f), Mat("Wall", new Color(0.55f, 0.63f, 0.74f)));
        var wood = Mat("Table", new Color(0.52f, 0.36f, 0.22f));
        Primitive(PrimitiveType.Cube, "Table Top", new Vector3(0f, 0.75f, 1.5f), new Vector3(1.6f, 0.06f, 0.8f), wood);
        Primitive(PrimitiveType.Cube, "Table Base", new Vector3(0f, 0.36f, 1.5f), new Vector3(1.3f, 0.72f, 0.5f), wood);

        // Rig: XR Origin > Camera Offset (tracking space) > Main Camera. A headset overwrites the camera's
        // local pose; on desktop it stays at standing eye height.
        var origin = new GameObject("XR Origin");
        var offset = new GameObject("Camera Offset").transform;
        offset.SetParent(origin.transform, false);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        camera.transform.SetParent(offset, false);
        camera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camera.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        // C# interaction: Color Target.
        var sphere = Primitive(PrimitiveType.Sphere, "Color Target", new Vector3(0.35f, 0.95f, 1.45f), Vector3.one * 0.32f,
            Mat("Color Target", new Color(0.18f, 0.32f, 0.72f)));
        var target = sphere.AddComponent<ColorToggleTarget>();
        var targetSo = new SerializedObject(target);
        targetSo.FindProperty("targetRenderer").objectReferenceValue = sphere.GetComponent<Renderer>();
        targetSo.FindProperty("activeColor").colorValue = new Color(1f, 0.72f, 0.11f);
        targetSo.ApplyModifiedPropertiesWithoutUndo();

        var interactor = origin.AddComponent<XRRayColorInteractor>();
        var interactorSo = new SerializedObject(interactor);
        interactorSo.FindProperty("trackingSpace").objectReferenceValue = offset;
        interactorSo.FindProperty("fallbackCamera").objectReferenceValue = camera;
        interactorSo.FindProperty("keyboardTarget").objectReferenceValue = target;
        interactorSo.ApplyModifiedPropertiesWithoutUndo();

        // Inspector-event interaction: Animated Prop + Animator Controller.
        var prop = Primitive(PrimitiveType.Cube, "Animated Prop", new Vector3(-0.35f, 0.78f + PropScale * 0.5f, 1.45f),
            Vector3.one * PropScale, Mat("Animated Prop", new Color(0.86f, 0.3f, 0.24f)));
        var animator = prop.AddComponent<Animator>();
        animator.runtimeAnimatorController = BuildController();

        // UI: World Space canvas above the table. A mouse can click it in the Game view; a headset needs
        // the optional XR Interaction Toolkit UI setup described in the README.
        var canvas = BuildCanvas(camera);
        Label(canvas.transform, "Title", "Week 9: Interactive VR", 34, FontStyle.Bold, new Vector2(0f, 95f), new Vector2(560f, 50f));
        Label(canvas.transform, "Instructions",
            "Button: Animator.SetTrigger(\"Pulse\") via On Click()\nSphere: click it, press Space, or aim + trigger",
            20, FontStyle.Normal, new Vector2(0f, 40f), new Vector2(560f, 60f));
        var button = DefaultControls.CreateButton(UiResources()).GetComponent<Button>();
        button.name = "Pulse Button";
        var buttonRect = (RectTransform)button.transform;
        buttonRect.SetParent(canvas.transform, false);
        buttonRect.sizeDelta = new Vector2(260f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -40f);
        var buttonText = button.GetComponentInChildren<Text>();
        buttonText.text = "Pulse the prop";
        buttonText.font = Font;
        buttonText.fontSize = 26;
        UnityEventTools.AddStringPersistentListener(button.onClick, new UnityAction<string>(animator.SetTrigger), "Pulse");

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Week9] Built " + ScenePath + ". Press Play, click the button, then click the sphere or press Space.");
    }

    static RuntimeAnimatorController BuildController()
    {
        var idle = new AnimationClip { name = "Idle" };
        var pulse = new AnimationClip { name = "Pulse" };
        foreach (var axis in new[] { "x", "y", "z" })
        {
            idle.SetCurve("", typeof(Transform), "m_LocalScale." + axis, AnimationCurve.Constant(0f, 1f, PropScale));
            pulse.SetCurve("", typeof(Transform), "m_LocalScale." + axis, new AnimationCurve(
                new Keyframe(0f, PropScale), new Keyframe(0.15f, PropScale * 1.45f), new Keyframe(0.45f, PropScale)));
        }
        SetLoop(idle, true);
        SetLoop(pulse, false);
        Save(idle, AnimationsFolder + "/Idle.anim");
        Save(pulse, AnimationsFolder + "/Pulse.anim");

        const string controllerPath = AnimationsFolder + "/AnimatedProp.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Pulse", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle");
        idleState.motion = idle;
        var pulseState = machine.AddState("Pulse");
        pulseState.motion = pulse;
        machine.defaultState = idleState;

        var toPulse = idleState.AddTransition(pulseState);
        toPulse.AddCondition(AnimatorConditionMode.If, 0f, "Pulse");
        toPulse.hasExitTime = false;
        toPulse.duration = 0.05f;
        var toIdle = pulseState.AddTransition(idleState);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 1f;
        toIdle.duration = 0.05f;
        return controller;
    }

    static Canvas BuildCanvas(Camera eventCamera)
    {
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eventCamera;
        var rect = (RectTransform)canvas.transform;
        rect.sizeDelta = new Vector2(600f, 260f);
        rect.position = new Vector3(0f, 1.5f, 1.75f);
        rect.localScale = Vector3.one * 0.0018f;
        var panel = new GameObject("Panel", typeof(Image)).GetComponent<Image>();
        panel.rectTransform.SetParent(rect, false);
        panel.rectTransform.anchorMin = Vector2.zero;
        panel.rectTransform.anchorMax = Vector2.one;
        panel.rectTransform.sizeDelta = Vector2.zero;
        panel.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);
        return canvas;
    }

    static void Label(Transform parent, string name, string text, int size, FontStyle style, Vector2 position, Vector2 box)
    {
        var label = new GameObject(name, typeof(Text)).GetComponent<Text>();
        label.rectTransform.SetParent(parent, false);
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = box;
        label.font = Font;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
    }

    static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static Material Mat(string name, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
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

    static void SetLoop(AnimationClip clip, bool loop)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static void Save(Object asset, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static DefaultControls.Resources UiResources() => new DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
    };

    static void EnsureFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/" + name)) AssetDatabase.CreateFolder("Assets", name);
    }
}
