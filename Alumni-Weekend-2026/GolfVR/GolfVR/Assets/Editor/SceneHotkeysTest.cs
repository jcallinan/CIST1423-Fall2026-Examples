using UnityEditor;
using UnityEngine;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Checks the keyboard scene switching set-up: ICARUS_v1 is the first
    /// (start) scene in Build Settings and every scene the 1-5 keys load exists
    /// and is enabled in Build Settings.
    /// </summary>
    public static class SceneHotkeysTest
    {
        [MenuItem("Tools/GolfVR/Scene Hotkeys Test")]
        public static void Run()
        {
            int failures = 0;
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0 || scenes[0].path != "Assets/Scenes/ICARUS_v1.unity" || !scenes[0].enabled)
            {
                failures++;
                Debug.LogError("[HotkeysTest] FAIL: ICARUS_v1 must be the first, enabled scene in Build Settings.");
            }

            foreach (string name in SceneHotkeys.SceneNames)
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    failures++;
                    Debug.LogError($"[HotkeysTest] FAIL: scene {name} does not exist.");
                    continue;
                }

                bool ok = false;
                foreach (var s in scenes) if (s.path == path && s.enabled) ok = true;
                if (!ok)
                {
                    failures++;
                    Debug.LogError($"[HotkeysTest] FAIL: scene {name} is not enabled in Build Settings.");
                }
            }

            foreach (var s in scenes)
            {
                if (s.path.EndsWith("MainMenu.unity"))
                {
                    failures++;
                    Debug.LogError("[HotkeysTest] FAIL: the retired MainMenu scene is still listed in Build Settings.");
                }
            }

            if (failures == 0) Debug.Log("[HotkeysTest] RESULT: PASS - ICARUS_v1 starts; keys 1-5 map to five existing, enabled scenes.");
            else Debug.LogError($"[HotkeysTest] RESULT: FAIL - {failures} check(s) failed.");
        }
    }
}
