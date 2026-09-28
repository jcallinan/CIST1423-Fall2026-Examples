using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// A target board that counts hits from thrown objects. Shows three kinds of feedback at once:
    /// a score on a sign, a particle burst where it was hit, and a rising chime.
    /// Physics reminder (Week 3): the thrown object has the Rigidbody, so OnCollisionEnter fires here.
    /// </summary>
    public class ThrowTarget : MonoBehaviour
    {
        public TextMesh scoreLabel;

        [Tooltip("Hit effect. The scene builder creates one with Unity's particle material; if empty, one is made at runtime")]
        public ParticleSystem burst;

        [Tooltip("Slower hits (m/s) don't count -- a cube resting against the board isn't a throw")]
        public float minHitSpeed = 1.2f;

        public int Hits { get; private set; }

        AudioSource _audio;
        AudioClip _chime;
        float _lastHitTime;

        void Awake()
        {
            _audio = PlaygroundAudio.SourceOn(gameObject);
            _chime = PlaygroundAudio.Chime("TargetChime", 0.12f, 659.25f, 987.77f); // E5 -> B5
            if (burst == null) burst = CreateBurst(transform);
            UpdateLabel();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody == null || collision.rigidbody.GetComponent<Throwable>() == null) return;
            if (collision.relativeVelocity.magnitude < minHitSpeed) return;
            if (Time.time - _lastHitTime < 0.3f) return; // one bounce = one hit
            _lastHitTime = Time.time;

            Hits++;
            UpdateLabel();
            burst.transform.position = collision.GetContact(0).point;
            burst.Play();
            _audio.pitch = 1f + Mathf.Min(Hits, 10) * 0.03f; // each hit a little higher
            _audio.PlayOneShot(_chime);
        }

        public void ResetScore()
        {
            Hits = 0;
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (scoreLabel != null) scoreLabel.text = $"TARGET\nHits: {Hits}";
        }

        /// <summary>A one-shot burst of 40 gold/white sparks. Public so the scene builder can pre-create it.</summary>
        public static ParticleSystem CreateBurst(Transform parent)
        {
            var go = new GameObject("HitBurst");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = 2.5f;
            main.startSize = 0.05f;
            main.gravityModifier = 0.5f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.11f), Color.white);
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            return ps;
        }
    }
}
