using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// Lets the player pick the object up with E, but only while the scenario allows it.
    /// A <see cref="ScenarioStepEvents"/> binding calls <see cref="SetInteractable"/> when the pickup step
    /// starts and ends. Trying too early is reported as a mistake. On pickup it raises a signal through a
    /// <see cref="ScenarioSignalEmitter"/>. The pickup knows nothing about the order of the steps.
    /// </summary>
    [RequireComponent(typeof(ScenarioSignalEmitter))]
    public sealed class DemoPickup : MonoBehaviour
    {
        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] Transform player;
        [SerializeField] Transform holdPoint;
        [SerializeField, Min(0)] float pickupRadius = 1.6f;

        ScenarioSignalEmitter emitter;
        Vector3 startPosition;
        Quaternion startRotation;
        Transform startParent;

        /// <summary>Whether the player is carrying the object.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Whether the scenario currently allows picking the object up.</summary>
        public bool IsInteractable { get; private set; }

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

        /// <summary>Allows or forbids picking the object up. Called from scenario step events.</summary>
        public void SetInteractable(bool value) => IsInteractable = value;

        /// <summary>
        /// Picks the object up if the player is in reach and the scenario allows it. Trying while it is not
        /// allowed reports a mistake. Returns whether the object was picked up.
        /// </summary>
        public bool TryPickUp()
        {
            if (!IsInReach)
                return false;

            if (!IsInteractable)
            {
                if (runner != null && runner.Context != null)
                    runner.Context.ReportMistake("Tried to take the extinguisher before being told to.");
                return false;
            }

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
            IsInteractable = false;
            transform.SetParent(startParent, false);
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
