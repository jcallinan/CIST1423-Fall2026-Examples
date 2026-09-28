using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace GolfVR.EditorTools
{
    /// <summary>
    /// Fires vertical "teleport pointer" rays (the same Linecast-first-hit rule the
    /// SteamVR Teleport arc uses) over a grid of the playable area and reports
    /// every spot where the first thing hit is NOT an unlocked teleport area --
    /// those are the spots where the pointer shows red. Coplanar solid + proxy
    /// colliders make that flicker. Args: -testScene path, -gridMin, -gridMax.
    /// </summary>
    public static class TeleportCoverageTest
    {
        [MenuItem("Tools/GolfVR/Teleport Coverage Test")]
        public static void Run()
        {
            string scene = GetArg("-testScene") ?? "Assets/Scenes/ICARUS_v1.unity";
            float min = float.Parse(GetArg("-gridMin") ?? "-16", System.Globalization.CultureInfo.InvariantCulture);
            float max = float.Parse(GetArg("-gridMax") ?? "16", System.Globalization.CultureInfo.InvariantCulture);
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            Physics.SyncTransforms();

            int total = 0, green = 0, red = 0, locked = 0, nothing = 0;
            var offenders = new Dictionary<string, int>();
            for (float x = min; x <= max; x += 0.2f)
            {
                for (float z = min; z <= max; z += 0.2f)
                {
                    total++;
                    Vector3 a = new Vector3(x, 3f, z), b = new Vector3(x, -3f, z);
                    if (!Physics.Linecast(a, b, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) { nothing++; continue; }
                    if (hit.collider.GetComponent<IgnoreTeleportTrace>() != null) continue;
                    var marker = hit.collider.GetComponentInParent<TeleportMarkerBase>();
                    if (marker == null)
                    {
                        // ground-level solids like rocks/signs/table legs are legitimately not teleportable; only count near-ground hits
                        if (hit.point.y < 0.3f)
                        {
                            red++;
                            string key = hit.collider.name + " y=" + hit.point.y.ToString("F2");
                            offenders[key] = offenders.ContainsKey(key) ? offenders[key] + 1 : 1;
                        }
                    }
                    else if (marker.locked) locked++;
                    else green++;
                }
            }

            Debug.Log($"[TeleportCov] scene {scene}: {total} samples -> green {green}, RED-at-ground {red}, locked {locked}, empty {nothing}");
            foreach (var kv in offenders) Debug.Log($"[TeleportCov]   red at ground: {kv.Key} x{kv.Value}");
            Debug.Log(red == 0 && locked == 0 ? "[TeleportCov] RESULT: PASS" : "[TeleportCov] RESULT: FAIL");
        }

        private static string GetArg(string flag)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == flag) return a[i + 1];
            return null;
        }
    }
}
