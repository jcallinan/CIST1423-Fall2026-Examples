using UnityEngine;

namespace PantherCamCast
{
    /// <summary>
    /// Bobs gently on the water surface while Waiting, dips sharply on a Biting cue.
    /// Purely visual/feedback -- listens to CastAndReelController, drives nothing back.
    /// </summary>
    [RequireComponent(typeof(CastAndReelController))]
    public class Bobber : MonoBehaviour
    {
        public Transform bobberVisual;
        public float idleBobHeight = 0.05f;
        public float idleBobSpeed = 1.5f;
        public float biteDipDepth = 0.35f;
        public float biteDipSpeed = 8f;

        CastAndReelController _controller;
        Vector3 _basePos;
        bool _biting;

        void Awake()
        {
            _controller = GetComponent<CastAndReelController>();
            if (bobberVisual != null) _basePos = bobberVisual.localPosition;
            _controller.OnStateChanged += HandleStateChanged;
        }

        void OnDestroy()
        {
            if (_controller != null) _controller.OnStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(FishingState state)
        {
            _biting = state == FishingState.Biting || state == FishingState.Reeling;
        }

        void Update()
        {
            if (bobberVisual == null) return;

            float targetOffset = _biting
                ? -biteDipDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * biteDipSpeed))
                : idleBobHeight * Mathf.Sin(Time.time * idleBobSpeed);

            var pos = bobberVisual.localPosition;
            pos.y = _basePos.y + targetOffset;
            bobberVisual.localPosition = pos;
        }
    }
}
