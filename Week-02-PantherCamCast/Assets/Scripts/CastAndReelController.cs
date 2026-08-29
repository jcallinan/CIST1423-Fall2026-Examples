using System;
using UnityEngine;

namespace PantherCamCast
{
    /// <summary>
    /// The core fishing state machine: Idle -> Casting -> Waiting -> Biting -> Reeling -> Caught -> Idle.
    /// Deliberately input-agnostic: FishingRod (or a VR grab/trigger script) calls Cast()/Reel(),
    /// this class only tracks state and timing. See 04_STATUS-style docs in Rhodium for the pattern
    /// this mirrors: gameplay logic stays free of input/XR concerns.
    /// </summary>
    public enum FishingState { Idle, Casting, Waiting, Biting, Reeling, Caught }

    public class CastAndReelController : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds the cast animation/arc takes before the bobber lands.")]
        public float castDuration = 0.75f;

        [Tooltip("Min/max seconds to wait for a bite once the bobber is in the water.")]
        public Vector2 biteWindowRange = new Vector2(2f, 8f);

        [Tooltip("Seconds the player has to start reeling once a bite starts, before the fish gets away.")]
        public float biteReactionWindow = 1.25f;

        [Header("Reel tension")]
        [Tooltip("Reel input below this per second is treated as 'too slow' and tension drains.")]
        public float reelDrainRate = 0.15f;
        [Tooltip("Reel input above this per second snaps the line.")]
        public float maxReelRate = 3.0f;
        [Range(0f, 1f)] public float progress;   // 0 = just hooked, 1 = landed
        [Range(0f, 1f)] public float tension;    // 1 = about to snap

        public FishingState State { get; private set; } = FishingState.Idle;

        public event Action<FishingState> OnStateChanged;
        public event Action OnCaught;
        public event Action OnLineSnapped;
        public event Action OnFishGotAway;

        float _stateTimer;
        float _lastReelTime;

        void SetState(FishingState next)
        {
            State = next;
            _stateTimer = 0f;
            OnStateChanged?.Invoke(next);
        }

        /// <summary>Call from input (keyboard/mouse, gamepad, or a VR rod-grab trigger).</summary>
        public void Cast()
        {
            if (State != FishingState.Idle) return;
            SetState(FishingState.Casting);
        }

        /// <summary>Call every frame reel input is held, with how far the reel turned this frame (0..1 typical).</summary>
        public void Reel(float amount)
        {
            if (State != FishingState.Biting)
            {
                // A reel input during Biting converts it into Reeling; otherwise Reel() only matters while Reeling.
                if (State != FishingState.Reeling) return;
            }
            else
            {
                SetState(FishingState.Reeling);
            }

            float rate = amount / Mathf.Max(Time.deltaTime, 0.0001f);
            if (rate > maxReelRate)
            {
                OnLineSnapped?.Invoke();
                SetState(FishingState.Idle);
                return;
            }

            progress += amount * 0.5f;
            tension = Mathf.Clamp01(tension + amount * 0.3f - reelDrainRate * Time.deltaTime);

            if (progress >= 1f)
            {
                SetState(FishingState.Caught);
                OnCaught?.Invoke();
            }
        }

        void Update()
        {
            _stateTimer += Time.deltaTime;

            switch (State)
            {
                case FishingState.Casting:
                    if (_stateTimer >= castDuration)
                    {
                        SetState(FishingState.Waiting);
                        _biteDelay = UnityEngine.Random.Range(biteWindowRange.x, biteWindowRange.y);
                    }
                    break;

                case FishingState.Waiting:
                    if (_stateTimer >= _biteDelay)
                    {
                        SetState(FishingState.Biting);
                    }
                    break;

                case FishingState.Biting:
                    if (_stateTimer >= biteReactionWindow)
                    {
                        OnFishGotAway?.Invoke();
                        SetState(FishingState.Idle);
                    }
                    break;

                case FishingState.Reeling:
                    // Tension drains passively too, so students have to keep reeling rhythmically.
                    tension = Mathf.Clamp01(tension - reelDrainRate * Time.deltaTime);
                    break;

                case FishingState.Caught:
                    if (_stateTimer >= 1.5f) SetState(FishingState.Idle);
                    break;
            }
        }

        float _biteDelay;
    }
}
