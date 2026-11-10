using ScenarioForge.Demo;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Dev
{
    /// <summary>
    /// Development only (not part of the package): plays the Fire Drill demo by itself in Play Mode, like a
    /// player would, so the debugger shot (docs/portfolio/recording-guide.md) can be recorded without keyboard
    /// input. It makes one deliberate mistake (aiming before pulling the pin) so the event log shows one.
    /// </summary>
    sealed class PortfolioAutopilot
    {
        const float WalkSpeed = 3.5f;
        const float Pause = 0.8f;

        static PortfolioAutopilot current;

        readonly ScenarioRunnerBehaviour runner;
        readonly Transform player;
        readonly DemoPickup extinguisher;
        readonly DemoFire fire;
        double lastTime = EditorApplication.timeSinceStartup;
        float wait = 1.2f;
        int phase;

        PortfolioAutopilot()
        {
            runner = Object.FindAnyObjectByType<ScenarioRunnerBehaviour>();
            player = Object.FindAnyObjectByType<DemoPlayer>()?.transform;
            extinguisher = Object.FindAnyObjectByType<DemoPickup>();
            fire = Object.FindAnyObjectByType<DemoFire>();
        }

        /// <summary>Starts the autopilot. Play Mode and the Fire Drill demo scene must be running.</summary>
        internal static string Start()
        {
            Stop();
            if (!EditorApplication.isPlaying)
                return "Enter Play Mode first.";

            var pilot = new PortfolioAutopilot();
            if (pilot.runner == null || pilot.player == null || pilot.extinguisher == null || pilot.fire == null)
                return "The Fire Drill demo scene is not running.";

            current = pilot;
            EditorApplication.update += pilot.Update;
            return "Autopilot started.";
        }

        /// <summary>Stops the autopilot.</summary>
        internal static void Stop()
        {
            if (current != null)
                EditorApplication.update -= current.Update;
            current = null;
        }

        void Update()
        {
            if (!EditorApplication.isPlaying || runner == null)
            {
                Stop();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - lastTime);
            lastTime = now;
            if (EditorApplication.isPaused)
                return;

            if (wait > 0f)
            {
                wait -= dt;
                return;
            }

            ScenarioContext context = runner.Context;
            int step = runner.Runner != null ? runner.Runner.CurrentStepIndex : -1;
            switch (phase)
            {
                case 0: // Briefing: confirm.
                    context.Signals.Raise("confirm");
                    Next(1.5f);
                    break;
                case 1: // Walk to the extinguisher, then take it.
                    if (WalkTo(extinguisher.transform.position, 0.9f, dt) && extinguisher.IsInteractable)
                    {
                        if (extinguisher.TryPickUp())
                            Next(Pause);
                    }
                    break;
                case 2: // Walk to the fire until the checklist step starts.
                    WalkTo(fire.transform.position, 0.7f, dt);
                    if (step >= 5)
                        Next(Pause);
                    break;
                case 3: // Mistake on purpose: aim before pulling the pin.
                    context.Signals.Raise("aim");
                    Next(1.2f);
                    break;
                case 4:
                    context.Signals.Raise("pull_pin");
                    Next(Pause);
                    break;
                case 5:
                    context.Signals.Raise("aim");
                    Next(Pause);
                    break;
                case 6:
                    context.Signals.Raise("squeeze");
                    Next(Pause);
                    break;
                case 7: // Spray until the fire is out.
                    if (fire.IsReady && !fire.Spray(dt) && !fire.IsBurning)
                        Next(0f);
                    break;
                default:
                    Stop();
                    break;
            }
        }

        void Next(float pause)
        {
            phase++;
            wait = pause;
        }

        bool WalkTo(Vector3 target, float stopDistance, float dt)
        {
            Vector3 offset = target - player.position;
            offset.y = 0f;
            float remaining = offset.magnitude - stopDistance;
            if (remaining <= 0.01f)
                return true;

            float distance = Mathf.Min(WalkSpeed * dt, remaining);
            player.SetPositionAndRotation(player.position + offset.normalized * distance, Quaternion.LookRotation(offset.normalized));
            return distance >= remaining;
        }
    }
}
