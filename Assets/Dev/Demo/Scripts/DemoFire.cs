using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// A fire that starts unlit. A <see cref="ScenarioSignalListener"/> calls <see cref="Ignite"/> when the
    /// scenario sends <c>fire_started</c>. The player puts it out by holding Space nearby while carrying the
    /// extinguisher, once the scenario has written <c>extinguisher_ready = true</c> to the blackboard.
    /// Spraying too early is reported as a mistake. When the fire is out it writes <c>fire_out = true</c>.
    /// </summary>
    public sealed class DemoFire : MonoBehaviour
    {
        /// <summary>Blackboard key the scenario sets when the extinguisher is prepared.</summary>
        public const string ReadyKey = "extinguisher_ready";

        /// <summary>Blackboard key this fire sets when it is out.</summary>
        public const string FireOutKey = "fire_out";

        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] Transform player;
        [SerializeField] DemoPickup extinguisher;
        [SerializeField] Transform flame;
        [SerializeField] Light glow;
        [SerializeField, Min(0)] float range = 3f;
        [SerializeField, Min(0.1f)] float secondsToPutOut = 3f;

        Vector3 flameScale;

        /// <summary>Remaining fire, from 1 (full) to 0 (out or not lit yet).</summary>
        public float Intensity { get; private set; }

        /// <summary>Whether the fire was lit and is still burning.</summary>
        public bool IsBurning => Intensity > 0f;

        /// <summary>Whether the player is spraying the fire this frame.</summary>
        public bool IsSpraying { get; private set; }

        /// <summary>Whether the scenario has marked the extinguisher as ready.</summary>
        public bool IsReady =>
            runner != null && runner.Context != null &&
            runner.Context.Blackboard.TryGet(ReadyKey, out bool ready) && ready;

        bool InRange => player != null && (player.position - transform.position).sqrMagnitude <= range * range;

        void Awake()
        {
            if (flame != null)
                flameScale = flame.localScale;
            UpdateFlame();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && IsBurning && InRange && extinguisher != null && extinguisher.IsHeld && !IsReady)
                runner.Context?.ReportMistake("Sprayed before preparing the extinguisher (pin, aim, squeeze).");

            IsSpraying = keyboard != null && keyboard.spaceKey.isPressed && Spray(Time.deltaTime);
            UpdateFlame();
        }

        /// <summary>Lights the fire. Called by a signal listener when the scenario sends <c>fire_started</c>.</summary>
        public void Ignite()
        {
            Intensity = 1f;
            UpdateFlame();
        }

        /// <summary>
        /// Sprays the fire for <paramref name="deltaTime"/> seconds. Returns <c>false</c> when the fire is not
        /// burning, the player is out of range or not carrying the extinguisher, or the scenario has not marked it ready.
        /// </summary>
        public bool Spray(float deltaTime)
        {
            if (!IsBurning || !InRange || extinguisher == null || !extinguisher.IsHeld || !IsReady)
                return false;

            Intensity = Mathf.Max(0f, Intensity - deltaTime / secondsToPutOut);
            if (Intensity <= 0f)
                runner.Context.Blackboard.Set(FireOutKey, true);
            return true;
        }

        void UpdateFlame()
        {
            if (glow != null)
            {
                glow.enabled = IsBurning;
                glow.intensity = 6f * Intensity * (1f + Mathf.Sin(Time.time * 23f) * 0.15f);
            }

            if (flame == null)
                return;

            flame.gameObject.SetActive(IsBurning);
            float flicker = 1f + Mathf.Sin(Time.time * 18f) * 0.06f;
            flame.localScale = flameScale * (Mathf.Lerp(0.25f, 1f, Intensity) * flicker);
        }

        /// <summary>Puts the fire out without scoring it, back to the unlit start state.</summary>
        public void ResetDemo()
        {
            Intensity = 0f;
            IsSpraying = false;
            UpdateFlame();
        }
    }
}
