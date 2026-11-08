namespace ScenarioForge
{
    /// <summary>One problem found by validation.</summary>
    public readonly struct ValidationIssue
    {
        /// <summary>How serious the problem is.</summary>
        public readonly ValidationSeverity Severity;

        /// <summary>What is wrong and how to fix it.</summary>
        public readonly string Message;

        /// <summary>The id of the step with the problem, or <c>null</c> for scenario-level problems.</summary>
        public readonly string StepId;

        /// <summary>
        /// Zero-based index of the step with the problem, or <c>-1</c> when unknown. Set by
        /// <see cref="ScenarioValidator"/>; useful for steps whose type is missing and therefore have no id.
        /// </summary>
        public readonly int StepIndex;

        /// <summary>Creates a new issue.</summary>
        public ValidationIssue(ValidationSeverity severity, string message, string stepId)
            : this(severity, message, stepId, -1)
        {
        }

        /// <summary>Creates a new issue tied to a step index.</summary>
        public ValidationIssue(ValidationSeverity severity, string message, string stepId, int stepIndex)
        {
            Severity = severity;
            Message = message;
            StepId = stepId;
            StepIndex = stepIndex;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{Severity}] {Message}";
    }
}
