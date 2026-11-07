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

        /// <summary>Creates a new issue.</summary>
        public ValidationIssue(ValidationSeverity severity, string message, string stepId)
        {
            Severity = severity;
            Message = message;
            StepId = stepId;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{Severity}] {Message}";
    }
}
