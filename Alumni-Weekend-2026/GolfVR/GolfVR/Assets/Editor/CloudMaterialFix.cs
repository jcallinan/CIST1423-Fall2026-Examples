using UnityEditor;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// CloudColr.mat uses a Shader Graph authored for the Universal Render Pipeline,
    /// but this project runs the Built-in Render Pipeline (no URP package installed),
    /// so the shader fails to compile and Unity falls back to the pink error shader.
    /// This swaps it to a Built-in-compatible transparent shader with an equivalent tint.
    /// </summary>
    [InitializeOnLoad]
    public static class CloudMaterialFix
    {
        private const string CloudMaterialPath = "Assets/GolfAssets/Materials/CloudColr.mat";
        private static readonly Color CloudTint = new Color(0.95f, 0.97f, 1f, 0.85f);

        static CloudMaterialFix()
        {
            FixIfBroken();
        }

        [MenuItem("Tools/GolfVR/Fix Broken Cloud Material")]
        private static void FixIfBroken()
        {
            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
            if (cloudMat == null) return;

            Shader shader = cloudMat.shader;
            bool isBroken = shader == null || !shader.isSupported || shader.name == "Hidden/InternalErrorShader";
            if (!isBroken) return;

            Shader replacement = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (replacement == null) return;

            cloudMat.shader = replacement;
            cloudMat.color = CloudTint;

            EditorUtility.SetDirty(cloudMat);
            AssetDatabase.SaveAssets();
            Debug.Log("[GolfVR] CloudColr.mat was using an unsupported URP Shader Graph shader; reassigned to Legacy Shaders/Transparent/Diffuse.");
        }
    }
}
