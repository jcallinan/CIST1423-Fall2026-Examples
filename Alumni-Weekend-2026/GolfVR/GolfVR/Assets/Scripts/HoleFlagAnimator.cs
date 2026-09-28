using UnityEngine;

namespace GolfVR
{
    /// <summary>
    /// Attach to a hole's flag. The flag's Animator (waving/idle loop) only plays
    /// while the player is actively on this flag's nearest hole; it stays still
    /// otherwise, instead of every flag on the course animating all the time.
    /// </summary>
    public class HoleFlagAnimator : MonoBehaviour
    {
        private Animator _animator;
        private GolfHole _nearestHole;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _nearestHole = FindNearestHole();
            SetAnimating(false);
        }

        private void Update()
        {
            if (_nearestHole == null || MiniGolfGameManager.Instance == null) return;

            bool isCurrentHole = MiniGolfGameManager.Instance.CurrentHoleNumber == _nearestHole.holeNumber;
            SetAnimating(isCurrentHole);
        }

        private void SetAnimating(bool animating)
        {
            if (_animator != null && _animator.enabled != animating)
            {
                _animator.enabled = animating;
            }
        }

        private GolfHole FindNearestHole()
        {
            GolfHole[] holes = FindObjectsOfType<GolfHole>();
            GolfHole nearest = null;
            float bestDist = float.MaxValue;

            foreach (GolfHole hole in holes)
            {
                float dist = Vector3.Distance(transform.position, hole.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = hole;
                }
            }

            return nearest;
        }
    }
}
