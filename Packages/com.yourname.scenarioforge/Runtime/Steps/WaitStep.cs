using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>Waits a fixed number of seconds.</summary>
    [Serializable]
    [ScenarioStepMenu("Flow/Wait", Tooltip = "Waits a fixed number of seconds.")]
    public sealed class WaitStep : ScenarioStep
    {
        [SerializeField, Min(0)] float duration = 1f;

        [NonSerialized] float elapsed;

        /// <summary>Seconds to wait.</summary>
        public float Duration
        {
            get => duration;
            internal set => duration = value;
        }

        /// <inheritdoc />
        public override bool CanFail => false;

        /// <inheritdoc />
        protected internal override void OnEnter(ScenarioContext context) => elapsed = 0f;

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            elapsed += deltaTime;
            return elapsed >= duration ? StepStatus.Succeeded : StepStatus.Running;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (duration <= 0f)
                report.Add(ValidationSeverity.Warning, "Duration is 0, so the step finishes on its first tick. Set a duration greater than 0.", Id);
        }
    }
}
