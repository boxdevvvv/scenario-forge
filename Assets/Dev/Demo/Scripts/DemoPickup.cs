using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// Lets the player pick the object up with E and raises a scenario signal through a
    /// <see cref="ScenarioSignalEmitter"/>. The pickup knows nothing about the scenario's steps.
    /// </summary>
    [RequireComponent(typeof(ScenarioSignalEmitter))]
    public sealed class DemoPickup : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Transform holdPoint;
        [SerializeField, Min(0)] float pickupRadius = 1.6f;

        ScenarioSignalEmitter emitter;
        Vector3 startPosition;
        Quaternion startRotation;
        Transform startParent;

        /// <summary>Whether the player is carrying the object.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Whether the player is close enough to pick the object up.</summary>
        public bool IsInReach => player != null && !IsHeld &&
            (player.position - transform.position).sqrMagnitude <= pickupRadius * pickupRadius;

        void Awake()
        {
            emitter = GetComponent<ScenarioSignalEmitter>();
            startParent = transform.parent;
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                TryPickUp();
        }

        /// <summary>Picks the object up if the player is in reach. Returns whether it was picked up.</summary>
        public bool TryPickUp()
        {
            if (!IsInReach)
                return false;

            IsHeld = true;
            transform.SetParent(holdPoint != null ? holdPoint : player, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            emitter.Raise();
            return true;
        }

        /// <summary>Drops the object back where the scene started.</summary>
        public void ResetDemo()
        {
            IsHeld = false;
            transform.SetParent(startParent, false);
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
