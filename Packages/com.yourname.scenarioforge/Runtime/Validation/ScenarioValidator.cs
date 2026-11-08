using System;
using System.Collections.Generic;

namespace ScenarioForge
{
    /// <summary>
    /// Checks a <see cref="ScenarioDefinition"/> for authoring mistakes. Works in the editor and at runtime;
    /// the editor adds checks that need editor APIs (missing types, scene targets).
    /// </summary>
    public static class ScenarioValidator
    {
        /// <summary>Validates <paramref name="definition"/> and returns every problem found. Never throws.</summary>
        public static ValidationReport Validate(ScenarioDefinition definition)
        {
            var report = new ValidationReport();
            if (definition == null)
            {
                report.Add(ValidationSeverity.Error, "No scenario to validate. Assign a ScenarioDefinition.");
                return report;
            }

            IReadOnlyList<ScenarioStep> steps = definition.Steps;
            if (steps.Count == 0)
            {
                report.Add(ValidationSeverity.Warning, "The scenario has no steps. Add steps in the Scenario Forge window.");
                return report;
            }

            var seenIds = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < steps.Count; index++)
                ValidateStep(report, steps[index], index, seenIds);

            return report;
        }

        /// <summary>A short description of a step for messages: <c>Step 3 "Drive to dock"</c>.</summary>
        public static string Describe(ScenarioStep step, int index)
        {
            return step == null || string.IsNullOrWhiteSpace(step.Title)
                ? $"Step {index + 1}"
                : $"Step {index + 1} \"{step.Title}\"";
        }

        static void ValidateStep(ValidationReport report, ScenarioStep step, int index, Dictionary<string, int> seenIds)
        {
            if (step == null)
            {
                report.Add(ValidationSeverity.Error, $"Step {index + 1} has a missing type: its class was deleted or renamed. Restore the class, add [MovedFrom] to the renamed class, or remove the step.", null, index);
                return;
            }

            string label = Describe(step, index);

            if (string.IsNullOrWhiteSpace(step.Id))
                report.Add(ValidationSeverity.Error, $"{label} has no id. Remove it and add it again, or duplicate it.", null, index);
            else if (seenIds.TryGetValue(step.Id, out int first))
                report.Add(ValidationSeverity.Error, $"{label} has the same id as step {first + 1}. Duplicate the step instead of copying its data, or remove one of them.", step.Id, index);
            else
                seenIds[step.Id] = index;

            if (string.IsNullOrWhiteSpace(step.Title))
                report.Add(ValidationSeverity.Warning, $"{label} has no title. Give it a title so it is easy to find in the window and the log.", step.Id, index);

            if (step.OnFailure == FailurePolicy.Retry && step.MaxRetries == 0)
                report.Add(ValidationSeverity.Warning, $"{label} uses Retry with Max Retries 0, so it never retries. Set Max Retries above 0 or choose another policy.", step.Id, index);

            if (step.TimeoutSeconds > 0f && !step.CanFail)
                report.Add(ValidationSeverity.Info, $"{label} has a timeout, but this step type never fails on its own: the timeout is its only way to fail.", step.Id, index);

            int firstIssue = report.Issues.Count;
            try
            {
                step.Validate(report);
            }
            catch (Exception exception)
            {
                report.Add(ValidationSeverity.Error, $"{label}: its Validate method threw {exception.GetType().Name}: {exception.Message}", step.Id, index);
            }

            report.AssignStepIndex(firstIssue, index, step.Id);
        }
    }
}
