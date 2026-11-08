using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>
    /// Shows an instruction and waits for a confirmation signal, a fixed time, or whichever comes first.
    /// </summary>
    [Serializable]
    [ScenarioStepMenu("Flow/Instruction", Tooltip = "Shows an instruction and waits for confirmation or a delay.")]
    public sealed class InstructionStep : ScenarioStep
    {
        [SerializeField] bool requireConfirmation = true;
        [SerializeField] string confirmSignal = "confirm";
        [SerializeField, Min(0)] float autoAdvanceSeconds;

        [NonSerialized] float elapsed;
        [NonSerialized] int confirmCountAtEnter;

        /// <summary>Whether the step waits for <see cref="ConfirmSignal"/>.</summary>
        public bool RequireConfirmation
        {
            get => requireConfirmation;
            internal set => requireConfirmation = value;
        }

        /// <summary>The signal that confirms the instruction.</summary>
        public string ConfirmSignal
        {
            get => confirmSignal;
            internal set => confirmSignal = value;
        }

        /// <summary>Seconds after which the step completes on its own. <c>0</c> disables it.</summary>
        public float AutoAdvanceSeconds
        {
            get => autoAdvanceSeconds;
            internal set => autoAdvanceSeconds = value;
        }

        /// <inheritdoc />
        public override bool CanFail => false;

        bool HasConfirmation => requireConfirmation && !string.IsNullOrWhiteSpace(confirmSignal);

        bool HasAutoAdvance => autoAdvanceSeconds > 0f;

        /// <inheritdoc />
        protected internal override void OnEnter(ScenarioContext context)
        {
            elapsed = 0f;
            confirmCountAtEnter = HasConfirmation ? context.Signals.GetCount(confirmSignal) : 0;
        }

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (!HasConfirmation && !HasAutoAdvance)
                return Fail("Instruction step has neither confirmation nor auto-advance, so it can never finish. Enable one of them.");

            elapsed += deltaTime;
            if (HasConfirmation && context.Signals.GetCount(confirmSignal) > confirmCountAtEnter)
                return StepStatus.Succeeded;

            if (HasAutoAdvance && elapsed >= autoAdvanceSeconds)
                return StepStatus.Succeeded;

            return StepStatus.Running;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(Instruction))
                report.Add(ValidationSeverity.Warning, "Instruction is empty, so the player sees no text. Write the instruction to show.", Id);

            if (!requireConfirmation && autoAdvanceSeconds <= 0f)
                report.Add(ValidationSeverity.Error, "The step can never finish: enable Require Confirmation or set Auto Advance Seconds above 0.", Id);
            else if (requireConfirmation && string.IsNullOrWhiteSpace(confirmSignal))
                report.Add(ValidationSeverity.Error, "Require Confirmation is on but Confirm Signal is empty. Enter the signal that confirms the instruction.", Id);
        }
    }
}
