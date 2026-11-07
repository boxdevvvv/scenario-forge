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
        public void Add(ValidationSeverity severity, string message, string stepId = null)
        {
            issues.Add(new ValidationIssue(severity, message ?? string.Empty, stepId));
            if (severity == ValidationSeverity.Error)
                HasErrors = true;
        }

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
    }
}
