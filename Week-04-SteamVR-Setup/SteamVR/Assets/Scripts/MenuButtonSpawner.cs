using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SteamVRPlayground
{
    /// <summary>
    /// Reading a SteamVR Input *action* directly, instead of going through the Interaction
    /// System. Press the Menu button (Vive Wand: button above the trackpad) on either hand to
    /// drop a fresh throwable cube half a meter in front of the player.
    ///
    /// Same technique as GolfVR's QuickResetController, which rebinds SnapTurnLeft/Right to
    /// "reset ball" and "snap putter to me".
    /// </summary>
    public class MenuButtonSpawner : MonoBehaviour
    {
        // Shows as a dropdown in the Inspector; the field initializer is the pattern
        // Valve's own samples use (BuggyController, JoeJeffController).
        public SteamVR_Action_Boolean spawnAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "Menu");

        public float forwardDistance = 0.5f;
        public float spawnHeight = 1.2f;
        public float cubeSize = 0.1f;

        void Update()
        {
            if (spawnAction == null || Player.instance == null) return;
            if (!spawnAction.GetStateDown(SteamVR_Input_Sources.Any)) return;

            Vector3 pos = Player.instance.feetPositionGuess
                        + Player.instance.bodyDirectionGuess.normalized * forwardDistance
                        + Vector3.up * spawnHeight;
            Spawn(pos);
        }

        void Spawn(Vector3 position)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SpawnedCube";
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * cubeSize;
            cube.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
            // Throwable's [RequireComponent]s add the Interactable and Rigidbody for us.
            cube.AddComponent<Throwable>();
            cube.AddComponent<GrabReporter>();
            cube.AddComponent<ResettablePose>().destroyOnReset = true; // the reset kiosk clears these
        }
    }
}
