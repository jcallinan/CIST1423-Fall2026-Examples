using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu item that builds Assets/Scenes/ComfortReviewSample.unity: a small standing-height room with a
/// label to read at a distance, plus the "Comfort Review" object from README section 1, step 4.
/// Use it to practice with the checklist; for the graded lab, copy ComfortReviewChecklist.cs into your
/// Week 4-12 project and review that scene instead. Re-running it overwrites the scene and materials.
/// </summary>
public static class BuildComfortReviewScene
{
    const string ScenePath = "Assets/Scenes/ComfortReviewSample.unity";

    [MenuItem("Week 13/Build Comfort Review Sample Scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camera = GameObject.Find("Main Camera").transform;
        camera.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.identity);

        Primitive(PrimitiveType.Plane, "Floor", Vector3.zero, Vector3.one, new Color(0.34f, 0.36f, 0.4f));
        Primitive(PrimitiveType.Cube, "Back Wall", new Vector3(0f, 1.5f, 4f), new Vector3(8f, 3f, 0.2f), new Color(0.75f, 0.78f, 0.82f));
        Primitive(PrimitiveType.Cube, "Counter", new Vector3(0f, 0.5f, 1.6f), new Vector3(1.4f, 1f, 0.6f), new Color(0.45f, 0.33f, 0.22f));
        Primitive(PrimitiveType.Cube, "High Shelf Item", new Vector3(0.9f, 2.2f, 3.6f), Vector3.one * 0.25f, new Color(0.85f, 0.3f, 0.24f));

        // A label to check for readability (UI-01) and a far-away exit prompt (ACCESS-01 / RECOVER-01).
        Sign("Task Label", "Place the cup on the counter", new Vector3(0f, 1.55f, 2f), 0.0012f, 36);
        Sign("Exit Sign", "Menu button: pause / exit", new Vector3(-1.8f, 2.4f, 3.85f), 0.0008f, 28);

        var review = new GameObject("Comfort Review");
        Undo.AddComponent<ComfortReviewChecklist>(review); // Undo.AddComponent runs Reset(), which seeds the starter checks

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = review;
        Debug.Log("[Week13] Built " + ScenePath + ". Select Comfort Review to fill in the checklist.");
    }

    static void Sign(string name, string text, Vector3 position, float scale, int fontSize)
    {
        var canvas = new GameObject(name, typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = (RectTransform)canvas.transform;
        rect.sizeDelta = new Vector2(700f, 100f);
        rect.position = position;
        rect.localScale = Vector3.one * scale;
        var background = new GameObject("Background", typeof(Image)).GetComponent<Image>();
        background.rectTransform.SetParent(rect, false);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        background.color = new Color(0.05f, 0.1f, 0.2f, 0.9f);
        var label = new GameObject("Text", typeof(Text)).GetComponent<Text>();
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = Vector2.zero;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
    }

    static void Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        string path = "Assets/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        go.GetComponent<Renderer>().sharedMaterial = material;
    }
}
