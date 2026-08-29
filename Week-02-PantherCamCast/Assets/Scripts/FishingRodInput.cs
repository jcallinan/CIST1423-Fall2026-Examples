using UnityEngine;

namespace PantherCamCast
{
    /// <summary>
    /// Desktop/keyboard stand-in for the eventual VR rod-grab + trigger input.
    /// Space = cast, hold Left Mouse (or R) while Biting/Reeling to reel.
    /// Swap this component out for an XRI grab+trigger script later -- CastAndReelController
    /// never needs to change, same "engine-agnostic gameplay" split Rhodium uses for desktop vs VR.
    /// </summary>
    [RequireComponent(typeof(CastAndReelController))]
    public class FishingRodInput : MonoBehaviour
    {
        public float reelAmountPerSecond = 1f;
        CastAndReelController _controller;

        void Awake() => _controller = GetComponent<CastAndReelController>();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) _controller.Cast();

            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.R))
            {
                _controller.Reel(reelAmountPerSecond * Time.deltaTime);
            }
        }
    }
}
