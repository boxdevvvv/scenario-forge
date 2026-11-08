using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>Waits until a blackboard value satisfies a comparison.</summary>
    [Serializable]
    [ScenarioStepMenu("Blackboard/Wait For Condition", Tooltip = "Waits until a blackboard value satisfies a comparison.")]
    public sealed class BlackboardConditionStep : ScenarioStep
    {
        [SerializeField] string key;
        [SerializeField] CompareOperator op = CompareOperator.Equal;
        [SerializeField] BlackboardValue value = BlackboardValue.FromBool(true);
        [SerializeField] bool failIfMissing;

        /// <summary>The blackboard key to read.</summary>
        public string Key
        {
            get => key;
            internal set => key = value;
        }

        /// <summary>The comparison: stored value (left) against <see cref="Value"/> (right).</summary>
        public CompareOperator Operator
        {
            get => op;
            internal set => op = value;
        }

        /// <summary>The value to compare against.</summary>
        public BlackboardValue Value
        {
            get => value;
            internal set => this.value = value;
        }

        /// <summary>Whether the step fails when the key is missing or holds another type, instead of waiting.</summary>
        public bool FailIfMissing
        {
            get => failIfMissing;
            internal set => failIfMissing = value;
        }

        /// <inheritdoc />
        public override bool CanFail => failIfMissing;

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Fail("Key is empty. Enter the blackboard key to read.");

            if (!context.Blackboard.TryGetValue(key.Trim(), out BlackboardValue stored))
                return failIfMissing ? Fail($"Blackboard key '{key}' is missing.") : StepStatus.Running;

            if (!stored.TryCompare(op, value, out bool result))
            {
                if (!BlackboardValue.IsOperatorSupported(value.Type, op))
                    return Fail($"Operator {op} cannot compare {value.Type} values. Use Equal or NotEqual.");

                return failIfMissing ? Fail($"Blackboard key '{key}' holds a {stored.Type}, but the condition expects a {value.Type}.") : StepStatus.Running;
            }

            return result ? StepStatus.Succeeded : StepStatus.Running;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(key))
                report.Add(ValidationSeverity.Error, "Key is empty. Enter the blackboard key to read.", Id);

            if (!BlackboardValue.IsOperatorSupported(value.Type, op))
                report.Add(ValidationSeverity.Error, $"Operator {op} cannot compare {value.Type} values. Use Equal or NotEqual, or change the value type.", Id);
        }
    }
}
