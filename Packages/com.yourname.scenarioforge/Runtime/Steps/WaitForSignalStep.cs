using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>Waits until a signal is raised a number of times after the step starts.</summary>
    [Serializable]
    [ScenarioStepMenu("Signals/Wait For Signal", Tooltip = "Waits until a signal is raised a number of times.")]
    public sealed class WaitForSignalStep : ScenarioStep
    {
        [SerializeField] string signal;
        [SerializeField, Min(1)] int requiredCount = 1;

        [NonSerialized] int countAtEnter;

        /// <summary>The signal to wait for.</summary>
        public string Signal
        {
            get => signal;
            internal set => signal = value;
        }

        /// <summary>How many times the signal must be raised after the step starts.</summary>
        public int RequiredCount
        {
            get => requiredCount;
            internal set => requiredCount = value;
        }

        /// <inheritdoc />
        public override bool CanFail => false;

        /// <inheritdoc />
        protected internal override void OnEnter(ScenarioContext context)
        {
            countAtEnter = context.Signals.GetCount(signal);
        }

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (string.IsNullOrWhiteSpace(signal))
                return Fail("Signal is empty, so the step can never finish. Enter the signal to wait for.");

            return context.Signals.GetCount(signal) - countAtEnter >= Math.Max(1, requiredCount)
                ? StepStatus.Succeeded
                : StepStatus.Running;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(signal))
                report.Add(ValidationSeverity.Error, "Signal is empty. Enter the signal to wait for.", Id);

            if (requiredCount < 1)
                report.Add(ValidationSeverity.Error, "Required Count must be at least 1.", Id);
        }
    }
}
