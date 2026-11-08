using System.Collections.Generic;

namespace ScenarioForge
{
    /// <summary>The list of problems found while validating a scenario.</summary>
    public sealed class ValidationReport
    {
        readonly List<ValidationIssue> issues = new();

        /// <summary>Every issue, in the order it was added.</summary>
        public IReadOnlyList<ValidationIssue> Issues => issues;

        /// <summary>Whether any issue is an <see cref="ValidationSeverity.Error"/>.</summary>
        public bool HasErrors { get; private set; }

        /// <summary>Adds an issue.</summary>
        /// <param name="severity">How serious it is.</param>
        /// <param name="message">What is wrong and how to fix it.</param>
        /// <param name="stepId">The step with the problem, or <c>null</c> for the whole scenario.</param>
        public void Add(ValidationSeverity severity, string message, string stepId = null) => Add(severity, message, stepId, -1);

        /// <summary>Counts the issues of one severity.</summary>
        public int Count(ValidationSeverity severity)
        {
            int count = 0;
            foreach (ValidationIssue issue in issues)
            {
                if (issue.Severity == severity)
                    count++;
            }

            return count;
        }

        /// <summary>The most serious severity reported for <paramref name="stepIndex"/>, or <c>null</c> when it has no issues.</summary>
        public ValidationSeverity? GetWorstSeverity(int stepIndex)
        {
            ValidationSeverity? worst = null;
            foreach (ValidationIssue issue in issues)
            {
                if (issue.StepIndex == stepIndex && (!worst.HasValue || issue.Severity > worst.Value))
                    worst = issue.Severity;
            }

            return worst;
        }

        internal void Add(ValidationSeverity severity, string message, string stepId, int stepIndex)
        {
            issues.Add(new ValidationIssue(severity, message ?? string.Empty, stepId, stepIndex));
            if (severity == ValidationSeverity.Error)
                HasErrors = true;
        }

        /// <summary>Gives every issue added since <paramref name="firstIssue"/> the step index, when it has none.</summary>
        internal void AssignStepIndex(int firstIssue, int stepIndex, string stepId)
        {
            for (int i = firstIssue; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];
                if (issue.StepIndex < 0)
                    issues[i] = new ValidationIssue(issue.Severity, issue.Message, issue.StepId ?? stepId, stepIndex);
            }
        }

        /// <summary>Appends every issue of <paramref name="other"/>.</summary>
        internal void AddRange(ValidationReport other)
        {
            foreach (ValidationIssue issue in other.issues)
                Add(issue.Severity, issue.Message, issue.StepId, issue.StepIndex);
        }
    }
}
