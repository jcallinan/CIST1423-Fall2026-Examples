using UnityEngine;
using UnityEngine.SceneManagement;

namespace GolfVR
{
    /// <summary>
    /// Keyboard scene switching for the whole app, with nothing to add to any
    /// scene: it creates itself at startup (RuntimeInitializeOnLoadMethod) and
    /// survives scene loads (DontDestroyOnLoad).
    ///
    ///   1  ICARUS_v1               (the 9-hole course; also the start scene)
    ///   2  ICARUS_TwoHole_v1
    ///   3  New_Sample
    ///   4  Dom_v4
    ///   5  ICARUS_DrivingRange_v1
    ///   Esc  quit (stops Play mode in the Editor)
    ///   H    show / hide the key list on the monitor
    ///
    /// Each scene must be enabled in Build Settings.
    /// </summary>
    public class SceneHotkeys : MonoBehaviour
    {
        public static readonly string[] SceneNames =
        {
            "ICARUS_v1", "ICARUS_TwoHole_v1", "New_Sample", "Dom_v4", "ICARUS_DrivingRange_v1"
        };

        private static readonly string[] Labels =
        {
            "ICARUS - 9 holes", "ICARUS - 2 holes", "New Sample", "Dom v4", "Driving range"
        };

        [Tooltip("Seconds the key list stays on screen after a scene loads")]
        public float hintSeconds = 8f;

        private float _hintUntil;
        private bool _hintHidden;
        private GUIStyle _style;
        private string _message;
        private float _messageUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<SceneHotkeys>() != null) return;

            GameObject go = new GameObject("SceneHotkeys");
            go.AddComponent<SceneHotkeys>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _hintUntil = Time.unscaledTime + hintSeconds;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _hintUntil = Time.unscaledTime + hintSeconds;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Quit();
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                _hintHidden = !_hintHidden;
                _hintUntil = Time.unscaledTime + hintSeconds;
            }

            for (int i = 0; i < SceneNames.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    Load(SceneNames[i]);
                    return;
                }
            }
        }

        public void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                _message = $"Scene '{sceneName}' is not in Build Settings";
                _messageUntil = Time.unscaledTime + 4f;
                Debug.LogError($"[GolfVR] Hotkeys: {_message}.");
                return;
            }

            Debug.Log($"[GolfVR] Hotkeys: loading scene '{sceneName}'.");
            SceneManager.LoadScene(sceneName);
        }

        public static void Quit()
        {
            Debug.Log("[GolfVR] Hotkeys: quitting.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGUI()
        {
            bool showHint = !_hintHidden && Time.unscaledTime < _hintUntil;
            bool showMessage = Time.unscaledTime < _messageUntil;
            if (!showHint && !showMessage) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
                _style.normal.textColor = Color.white;
            }

            string current = SceneManager.GetActiveScene().name;
            var sb = new System.Text.StringBuilder();
            if (showHint)
            {
                for (int i = 0; i < SceneNames.Length; i++)
                {
                    bool here = SceneNames[i] == current;
                    sb.Append(here ? "<b><color=#FFD940>" : "");
                    sb.Append($"{i + 1}  {Labels[i]}{(here ? "  (current)" : "")}");
                    sb.Append(here ? "</color></b>\n" : "\n");
                }
                sb.Append("Esc  quit      H  hide/show this list");
            }
            if (showMessage) sb.Append("\n<color=#FF8060>" + _message + "</color>");

            GUI.Box(new Rect(10, 10, 360, 20 + 26 * (SceneNames.Length + 2)), GUIContent.none);
            GUI.Label(new Rect(20, 16, 340, 26 * (SceneNames.Length + 3)), sb.ToString(), _style);
        }
    }
}
