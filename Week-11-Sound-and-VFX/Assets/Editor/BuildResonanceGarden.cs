using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Chapter7SoundVFX.Editor
{
    /// <summary>
    /// Menu item that builds the Week 11 Resonance Garden (Assets/Scenes/ResonanceGarden.unity) and its
    /// GardenMixer, following README sections 1-3. Re-running it overwrites the scene, mixer, and materials.
    ///
    ///   BeaconLow / BeaconMid / BeaconHigh - trigger spheres with ResonanceBeacon, a 3D AudioSource routed to
    ///                                        GardenMixer/Master/Feedback, and a one-shot Burst particle system
    ///   Player                             - tagged capsule with a Character Controller and the only Audio Listener
    ///   MixerControls + Slider             - -30..0 dB slider calling MixerLevelController.SetFeedbackLevel
    ///
    /// Unity has no public API for authoring an AudioMixer, so CreateMixer uses the editor's internal
    /// AudioMixerController through reflection. If a Unity upgrade breaks it, create the mixer by hand (README
    /// section 3) and re-run the builder: an existing Assets/Audio/GardenMixer.mixer is reused.
    /// </summary>
    public static class BuildResonanceGarden
    {
        const string ScenePath = "Assets/Scenes/ResonanceGarden.unity";
        const string MixerPath = "Assets/Audio/GardenMixer.mixer";
        const string ExposedParameter = "FeedbackLevelDb";

        [MenuItem("Week 11/Build Resonance Garden")]
        public static void Build()
        {
            EnsureFolder("Scenes");
            EnsureFolder("Materials");
            EnsureFolder("Audio");

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) ?? CreateMixer();
            var feedbackGroups = mixer != null ? mixer.FindMatchingGroups("Feedback") : null;
            if (feedbackGroups == null || feedbackGroups.Length == 0)
            {
                Debug.LogError("[Week11] " + MixerPath + " needs a Feedback group with an exposed " + ExposedParameter + " volume.");
                return;
            }
            var feedback = feedbackGroups[0];

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera"));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "GardenFloor";
            floor.GetComponent<Renderer>().sharedMaterial = Mat("GardenFloor", new Color(0.25f, 0.42f, 0.27f));

            BuildBeacon("BeaconLow", new Vector3(-2f, 1f, 3f), 261.63f, new Color(0.2f, 0.35f, 0.85f), new Color(1f, 0.85f, 0.2f), feedback);
            BuildBeacon("BeaconMid", new Vector3(0f, 1f, 4f), 440f, new Color(0.85f, 0.25f, 0.6f), new Color(0.4f, 1f, 0.9f), feedback);
            BuildBeacon("BeaconHigh", new Vector3(2f, 1f, 3f), 659.25f, new Color(0.95f, 0.55f, 0.1f), new Color(0.6f, 0.75f, 1f), feedback);

            // Desktop player rig: the CharacterController is the player's only collider.
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.AddComponent<CharacterController>();
            player.AddComponent<VelocityEstimator>(); // beacons read it to scale volume and particles by speed
            player.GetComponent<Renderer>().sharedMaterial = Mat("Player", new Color(0.85f, 0.85f, 0.85f));

            BuildFountain(mixer.FindMatchingGroups("Master")[0]);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            camera.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

            var controls = new GameObject("MixerControls").AddComponent<MixerLevelController>();
            var controlsSo = new SerializedObject(controls);
            controlsSo.FindProperty("mixer").objectReferenceValue = mixer;
            controlsSo.FindProperty("exposedVolumeParameter").stringValue = ExposedParameter;
            controlsSo.ApplyModifiedPropertiesWithoutUndo();
            BuildSliderUi(controls);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Week11] Built " + ScenePath + ". Press Play and move Player into a beacon in the Inspector.");
        }

        static void BuildBeacon(string name, Vector3 position, float frequency, Color color, Color burstColor, AudioMixerGroup output)
        {
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = name;
            beacon.transform.position = position;
            beacon.GetComponent<Renderer>().sharedMaterial = Mat(name, color);

            var trigger = beacon.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            var body = beacon.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var source = beacon.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.volume = 1f;
            source.pitch = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.outputAudioMixerGroup = output;

            var burstObject = new GameObject("Burst", typeof(ParticleSystem));
            burstObject.transform.SetParent(beacon.transform, false);
            var burst = burstObject.GetComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);              // Random Between Two Constants
            main.startSize = 0.08f;
            main.startColor = new ParticleSystem.MinMaxGradient(burstColor, Color.white); // Random Between Two Colors
            var fade = burst.colorOverLifetime;                                           // fade out instead of popping off
            fade.enabled = true;
            var fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = fadeGradient;
            var emission = burst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
            var shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            burstObject.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

            var script = beacon.AddComponent<ResonanceBeacon>();
            var so = new SerializedObject(script);
            so.FindProperty("audioSource").objectReferenceValue = source;
            so.FindProperty("burst").objectReferenceValue = burst;
            so.FindProperty("frequencyHz").floatValue = frequency;
            so.FindProperty("toneSeconds").floatValue = 0.8f;
            so.FindProperty("retriggerCooldown").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A fountain behind the beacons that loops an imported .wav clip, the usual way to add sound
        // (Ch. 7 p. 164). The builder writes the file itself so the lab still needs no downloads; swap in any
        // royalty-free water sound by replacing Assets/Audio/FountainLoop.wav.
        static void BuildFountain(AudioMixerGroup output)
        {
            const string clipPath = "Assets/Audio/FountainLoop.wav";
            WriteWaterLoopWav(clipPath, 3f);
            AssetDatabase.ImportAsset(clipPath);
            var importer = (AudioImporter)AssetImporter.GetAtPath(clipPath);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory; // a long loop: keep it compressed in memory
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;                                 // 3D sources are mixed as mono anyway
            importer.SaveAndReimport();

            var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Fountain";
            basin.transform.position = new Vector3(0f, 0.2f, 8f);
            basin.transform.localScale = new Vector3(1.6f, 0.2f, 1.6f);
            basin.GetComponent<Renderer>().sharedMaterial = Mat("Fountain Stone", new Color(0.6f, 0.6f, 0.62f));
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(basin.transform, false);
            water.transform.localPosition = new Vector3(0f, 1f, 0f); // sits on the basin's top face
            water.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
            water.GetComponent<Renderer>().sharedMaterial = Mat("Fountain Water", new Color(0.25f, 0.55f, 0.8f));

            var source = basin.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = 0.5f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 14f;
            source.outputAudioMixerGroup = output; // Master, so the Feedback slider doesn't affect it
        }

        // Soft filtered noise, 16-bit mono PCM. The last 0.25 s fades into the first so the loop has no click.
        static void WriteWaterLoopWav(string path, float seconds)
        {
            const int sampleRate = 44100;
            int count = Mathf.RoundToInt(sampleRate * seconds);
            int fade = sampleRate / 4;
            var random = new System.Random(7);
            var samples = new float[count + fade];
            float low = 0f, slow = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.08f;                 // one-pole low-pass: hiss becomes a rush
                slow += (white - slow) * 0.0004f;             // very slow drift for a gentle swell
                samples[i] = low * (0.55f + Mathf.Abs(slow) * 4f);
            }
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                samples[i] = samples[i] * t + samples[count + i] * (1f - t);
            }

            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);              // PCM
                writer.Write((short)1);              // mono
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);        // byte rate
                writer.Write((short)2);              // block align
                writer.Write((short)16);             // bits per sample
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                    writer.Write((short)Mathf.Clamp(samples[i] * 0.6f * short.MaxValue, short.MinValue, short.MaxValue));
            }
        }

        static void BuildSliderUi(MixerLevelController controls)
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);

            var panel = new GameObject("Mixer Panel", typeof(Image)).GetComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.55f);
            var panelRect = panel.rectTransform;
            panelRect.SetParent(canvas.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(440f, 120f);

            Label(panelRect, "Title", "Feedback level (GardenMixer)", 24, new Vector2(0f, 34f), new Vector2(420f, 36f), TextAnchor.MiddleCenter);
            Label(panelRect, "Min Label", "-30 dB", 20, new Vector2(-170f, -32f), new Vector2(90f, 30f), TextAnchor.MiddleLeft);
            Label(panelRect, "Max Label", "0 dB", 20, new Vector2(185f, -32f), new Vector2(60f, 30f), TextAnchor.MiddleRight);

            var slider = DefaultControls.CreateSlider(new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            }).GetComponent<Slider>();
            slider.name = "Feedback Slider";
            var sliderRect = (RectTransform)slider.transform;
            sliderRect.SetParent(panelRect, false);
            sliderRect.anchoredPosition = new Vector2(0f, -4f);
            sliderRect.sizeDelta = new Vector2(380f, 24f);
            slider.minValue = -30f;
            slider.maxValue = 0f;
            slider.value = 0f;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, controls.SetFeedbackLevel);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static void Label(Transform parent, string name, string text, int size, Vector2 position, Vector2 box, TextAnchor anchor)
        {
            var label = new GameObject(name, typeof(Text)).GetComponent<Text>();
            label.rectTransform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = box;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            label.text = text;
        }

        // GardenMixer: Master > Feedback, with Feedback's Attenuation volume exposed as FeedbackLevelDb.
        static AudioMixer CreateMixer()
        {
            const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            var groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var pathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");

            var mixer = controllerType.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { MixerPath });
            var master = controllerType.GetProperty("masterGroup", Any).GetValue(mixer);
            var group = controllerType.GetMethod("CreateNewGroup", Any).Invoke(mixer, new object[] { "Feedback", false });
            controllerType.GetMethod("AddChildToParent", Any).Invoke(mixer, new[] { group, master });

            var volumeGuid = groupType.GetMethod("GetGUIDForVolume", Any).Invoke(group, null);
            var parameterPath = Activator.CreateInstance(pathType, group, volumeGuid);
            controllerType.GetMethod("AddExposedParameter", Any).Invoke(mixer, new[] { parameterPath });

            var exposedProperty = controllerType.GetProperty("exposedParameters", Any);
            var exposed = (Array)exposedProperty.GetValue(mixer);
            for (int i = 0; i < exposed.Length; i++)
            {
                object parameter = exposed.GetValue(i);
                var guidField = parameter.GetType().GetField("guid", Any);
                if (!guidField.GetValue(parameter).Equals(volumeGuid)) continue;
                parameter.GetType().GetField("name", Any).SetValue(parameter, ExposedParameter);
                exposed.SetValue(parameter, i);
            }
            exposedProperty.SetValue(mixer, exposed);
            controllerType.GetMethod("OnSubAssetChanged", Any)?.Invoke(mixer, null);

            EditorUtility.SetDirty((Object)mixer);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
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
