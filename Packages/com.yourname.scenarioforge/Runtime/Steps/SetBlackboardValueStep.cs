using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>Writes a value to the blackboard and completes immediately.</summary>
    [Serializable]
    [ScenarioStepMenu("Blackboard/Set Value", Tooltip = "Writes a value to the blackboard and completes immediately.")]
    public sealed class SetBlackboardValueStep : ScenarioStep
    {
        [SerializeField] string key;
        [SerializeField] BlackboardValue value = BlackboardValue.FromBool(true);

        /// <summary>The blackboard key to write.</summary>
        public string Key
        {
            get => key;
            internal set => key = value;
        }

        /// <summary>The value to write.</summary>
        public BlackboardValue Value
        {
            get => value;
            internal set => this.value = value;
        }

        /// <inheritdoc />
        public override bool CanFail => false;

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Fail("Key is empty. Enter the blackboard key to write.");

            context.Blackboard.SetValue(key.Trim(), value);
            return StepStatus.Succeeded;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(key))
                report.Add(ValidationSeverity.Error, "Key is empty. Enter the blackboard key to write.", Id);
        }
    }
}
