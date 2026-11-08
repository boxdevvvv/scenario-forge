using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// Makes the scene react to the scenario: invokes UnityEvents when chosen steps start or end.
    /// Use it to enable an interaction only during its step, show a highlight, light a fire, and so on.
    /// </summary>
    [AddComponentMenu("Scenario Forge/Scenario Step Events")]
    public sealed class ScenarioStepEvents : MonoBehaviour
    {
        [Tooltip("The runner to listen to. Leave empty to use the first runner in the scene.")]
        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] List<StepEventBinding> bindings = new();

        ScenarioRunnerBehaviour subscribed;

        /// <summary>The runner to listen to. When <c>null</c>, the first runner in the scene is used.</summary>
        public ScenarioRunnerBehaviour Runner
        {
            get => runner;
            set
            {
                runner = value;
                if (isActiveAndEnabled)
                    Subscribe();
            }
        }

        /// <summary>The step bindings. Changes take effect immediately.</summary>
        public List<StepEventBinding> Bindings => bindings;

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            Unsubscribe();
            if (runner == null)
                runner = FindAnyObjectByType<ScenarioRunnerBehaviour>();

            if (runner == null)
            {
                Debug.LogWarning($"ScenarioStepEvents on '{name}' found no ScenarioRunnerBehaviour. Add one to the scene or assign Runner.", this);
                return;
            }

            subscribed = runner;
            subscribed.StepStarted += HandleStepStarted;
            subscribed.StepEnded += HandleStepEnded;
        }

        void Unsubscribe()
        {
            if (subscribed == null)
                return;

            subscribed.StepStarted -= HandleStepStarted;
            subscribed.StepEnded -= HandleStepEnded;
            subscribed = null;
        }

        void HandleStepStarted(StepInfo info)
        {
            foreach (StepEventBinding binding in bindings)
            {
                if (Matches(binding, info.StepId))
                    binding.OnStarted.Invoke();
            }
        }

        void HandleStepEnded(StepResult result)
        {
            foreach (StepEventBinding binding in bindings)
            {
                if (Matches(binding, result.StepId))
                    binding.OnEnded.Invoke();
            }
        }

        static bool Matches(StepEventBinding binding, string stepId) =>
            binding != null && !string.IsNullOrEmpty(stepId) && string.Equals(binding.StepId, stepId, StringComparison.Ordinal);
    }
}
