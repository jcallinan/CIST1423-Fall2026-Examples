using UnityEngine;

namespace SteamVRPlayground
{
    /// <summary>
    /// Tiny procedural sound library, so the playground needs no audio files -- the same
    /// AudioClip.Create technique GolfVR uses for its putt, fanfare and horn sounds.
    /// </summary>
    public static class PlaygroundAudio
    {
        const int SampleRate = 44100;

        /// <summary>Short sine "ding" with a fast decay. Notes are frequencies in Hz, played in order.</summary>
        public static AudioClip Chime(string name, float noteSeconds, params float[] notes)
        {
            int perNote = (int)(SampleRate * noteSeconds);
            var samples = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    float envelope = Mathf.Exp(-t * 9f);
                    samples[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * envelope * 0.5f;
                }
            }
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Adds a 3D AudioSource if the object doesn't have one.</summary>
        public static AudioSource SourceOn(GameObject go)
        {
            var source = go.GetComponent<AudioSource>();
            if (source == null)
            {
                source = go.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.playOnAwake = false;
            }
            return source;
        }
    }
}
