using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// A valve handwheel (SteamVR CircularDrive) that controls a water stream (ParticleSystem):
    /// turn it and the flow rises with how far open it is. Same valve idea as the WaterWorks
    /// WebXR builder, done with Unity's Interaction System.
    /// </summary>
    public class ValveFlow : MonoBehaviour
    {
        public LinearMapping mapping;      // CircularDrive writes 0 (shut) .. 1 (fully open) here
        public ParticleSystem water;
        public float maxRate = 250f;
        public TextMesh label;

        float _lastValue = -1f;

        void Update()
        {
            if (mapping == null || water == null || Mathf.Approximately(mapping.value, _lastValue)) return;
            _lastValue = mapping.value;
            Apply(water, mapping.value, maxRate);
            if (label != null) label.text = $"VALVE\n{mapping.value * 100f:0}% open";
        }

        /// <summary>Set the stream for an opening of 0..1 (also used by the builder for previews).</summary>
        public static void Apply(ParticleSystem ps, float open, float maxRate)
        {
            var emission = ps.emission;
            emission.rateOverTime = open * maxRate;
            var main = ps.main;
            main.startSpeed = Mathf.Lerp(0.5f, 2.5f, open);
            if (open > 0.01f && !ps.isPlaying) ps.Play();
        }

        /// <summary>Configure a ParticleSystem to look like a falling water stream.</summary>
        public static void ConfigureStream(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSize = 0.035f;
            main.gravityModifier = 1f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.65f, 1f, 0.9f), new Color(0.75f, 0.9f, 1f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4f;
            shape.radius = 0.02f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
        }
    }
}
