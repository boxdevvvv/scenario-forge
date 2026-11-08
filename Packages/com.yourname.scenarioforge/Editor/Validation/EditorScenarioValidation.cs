using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// Runs <see cref="ScenarioValidator"/> plus the checks that need the editor: managed references with
    /// missing types, and target ids that do not exist in the open scenes.
    /// </summary>
    static class EditorScenarioValidation
    {
        /// <summary>Validates <paramref name="definition"/>.</summary>
        /// <param name="definition">The scenario to check.</param>
        /// <param name="checkSceneTargets">Whether to warn about target ids missing from the open scenes.</param>
        public static ValidationReport Validate(ScenarioDefinition definition, bool checkSceneTargets)
        {
            ValidationReport runtimeReport = ScenarioValidator.Validate(definition);
            if (definition == null)
                return runtimeReport;

            var missingTypes = new Dictionary<int, string>();
            foreach (var missing in MissingTypes.FindInSteps(definition))
                missingTypes[missing.Key] = missing.Value.TypeName;
            var report = new ValidationReport();
            foreach (ValidationIssue issue in runtimeReport.Issues)
            {
                if (issue.StepIndex >= 0 && missingTypes.ContainsKey(issue.StepIndex) && definition.Steps[issue.StepIndex] == null)
                    continue;

                report.Add(issue.Severity, issue.Message, issue.StepId, issue.StepIndex);
            }

            foreach (KeyValuePair<int, string> missing in missingTypes)
            {
                report.Add(
                    ValidationSeverity.Error,
                    $"Step {missing.Key + 1}: Missing type: {missing.Value}. Restore the class, add [MovedFrom] to the renamed class, or remove the step.",
                    null,
                    missing.Key);
            }

            if (checkSceneTargets)
                ValidateSceneTargets(definition, report);

            return report;
        }

        /// <summary>Text for summaries such as the Inspector: <c>"1 error, 2 warnings"</c>.</summary>
        public static string Summarize(ValidationReport report)
        {
            int errors = report.Count(ValidationSeverity.Error);
            int warnings = report.Count(ValidationSeverity.Warning);
            int infos = report.Count(ValidationSeverity.Info);
            if (errors + warnings + infos == 0)
                return "No problems found";

            var parts = new List<string>();
            if (errors > 0)
                parts.Add(Plural(errors, "error"));
            if (warnings > 0)
                parts.Add(Plural(warnings, "warning"));
            if (infos > 0)
                parts.Add(Plural(infos, "info"));
            return string.Join(", ", parts);
        }

        /// <summary>The message prefixed with the step it belongs to, as shown in the window and the CI log.</summary>
        public static string FormatIssue(ScenarioDefinition definition, ValidationIssue issue)
        {
            if (issue.StepIndex < 0 || issue.Message.StartsWith("Step ", StringComparison.Ordinal))
                return issue.Message;

            ScenarioStep step = definition != null && issue.StepIndex < definition.Steps.Count ? definition.Steps[issue.StepIndex] : null;
            return $"{ScenarioValidator.Describe(step, issue.StepIndex)}: {issue.Message}";
        }

        static void ValidateSceneTargets(ScenarioDefinition definition, ValidationReport report)
        {
            ScenarioTarget[] targets = Object.FindObjectsByType<ScenarioTarget>(FindObjectsInactive.Include);
            if (UnityEngine.SceneManagement.SceneManager.sceneCount == 0)
                return;

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (ScenarioTarget target in targets)
            {
                if (!string.IsNullOrWhiteSpace(target.TargetId))
                    known.Add(target.TargetId.Trim());
            }

            for (int index = 0; index < definition.Steps.Count; index++)
            {
                if (definition.Steps[index] is not ITargetReferencingStep referencing)
                    continue;

                foreach (string id in referencing.TargetIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || known.Contains(id.Trim()))
                        continue;

                    report.Add(
                        ValidationSeverity.Warning,
                        $"Target '{id.Trim()}' is not in the open scenes. Add a ScenarioTarget with that id, or ignore this if the scenario runs in another scene.",
                        definition.Steps[index].Id,
                        index);
                }
            }
        }

        static string Plural(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";
    }
}
