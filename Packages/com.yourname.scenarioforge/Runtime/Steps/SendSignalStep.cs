using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>
    /// Raises a signal and completes immediately. Use it to make the scene react at this point of the
    /// scenario, for example with a <see cref="ScenarioSignalListener"/> that opens a door or lights a fire.
    /// </summary>
    [Serializable]
    [ScenarioStepMenu("Signals/Send Signal", Tooltip = "Raises a signal so the scene can react, then completes immediately.")]
    public sealed class SendSignalStep : ScenarioStep
    {
        [SerializeField] string signal;

        /// <summary>The signal to raise.</summary>
        public string Signal
        {
            get => signal;
            internal set => signal = value;
        }

        /// <inheritdoc />
        public override bool CanFail => false;

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (string.IsNullOrWhiteSpace(signal))
                return Fail("Signal is empty. Enter the signal to send.");

            context.Signals.Raise(signal);
            return StepStatus.Succeeded;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(signal))
                report.Add(ValidationSeverity.Error, "Signal is empty. Enter the signal to send.", Id);
        }
    }
}
