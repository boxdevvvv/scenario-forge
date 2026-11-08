using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// A fire the player puts out by holding Space nearby while carrying the extinguisher.
    /// It only works once the scenario has written <c>extinguisher_ready = true</c> to the
    /// blackboard, and it writes <c>fire_out = true</c> when the fire is out.
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
        [SerializeField, Min(0)] float range = 3f;
        [SerializeField, Min(0.1f)] float secondsToPutOut = 3f;

        Vector3 flameScale;

        /// <summary>Remaining fire, from 1 (full) to 0 (out).</summary>
        public float Intensity { get; private set; } = 1f;

        /// <summary>Whether the player is spraying the fire this frame.</summary>
        public bool IsSpraying { get; private set; }

        /// <summary>Whether the scenario has marked the extinguisher as ready.</summary>
        public bool IsReady =>
            runner != null && runner.Context != null &&
            runner.Context.Blackboard.TryGet(ReadyKey, out bool ready) && ready;

        void Awake()
        {
            if (flame != null)
                flameScale = flame.localScale;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            IsSpraying = keyboard != null && keyboard.spaceKey.isPressed && Spray(Time.deltaTime);
            UpdateFlame();
        }

        /// <summary>
        /// Sprays the fire for <paramref name="deltaTime"/> seconds. Returns <c>false</c> when the player is
        /// out of range, is not carrying the extinguisher, the scenario has not marked it ready, or the fire is out.
        /// </summary>
        public bool Spray(float deltaTime)
        {
            bool inRange = player != null && (player.position - transform.position).sqrMagnitude <= range * range;
            if (Intensity <= 0f || !inRange || extinguisher == null || !extinguisher.IsHeld || !IsReady)
                return false;

            Intensity = Mathf.Max(0f, Intensity - deltaTime / secondsToPutOut);
            if (Intensity <= 0f)
                runner.Context.Blackboard.Set(FireOutKey, true);
            return true;
        }

        void UpdateFlame()
        {
            if (flame == null)
                return;

            flame.gameObject.SetActive(Intensity > 0f);
            float flicker = 1f + Mathf.Sin(Time.time * 18f) * 0.06f;
            flame.localScale = flameScale * (Mathf.Lerp(0.25f, 1f, Intensity) * flicker);
        }

        /// <summary>Relights the fire.</summary>
        public void ResetDemo()
        {
            Intensity = 1f;
            IsSpraying = false;
        }
    }
}
