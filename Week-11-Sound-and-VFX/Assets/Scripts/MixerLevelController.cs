using UnityEngine;
using UnityEngine.Audio;

namespace Chapter7SoundVFX
{
    public class MixerLevelController : MonoBehaviour
    {
        [SerializeField] AudioMixer mixer;
        [SerializeField] string exposedVolumeParameter = "FeedbackLevelDb";

        public void SetFeedbackLevel(float decibels)
        {
            if (mixer == null) return;
            mixer.SetFloat(exposedVolumeParameter, Mathf.Clamp(decibels, -30f, 0f));
        }
    }
}