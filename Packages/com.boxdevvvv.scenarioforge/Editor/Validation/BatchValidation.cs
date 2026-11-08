using System.Text;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// Validates every <see cref="ScenarioDefinition"/> in the project. Use it from CI with
    /// <c>-executeMethod ScenarioForge.Editor.BatchValidation.ValidateAllScenarios</c>.
    /// </summary>
    public static class BatchValidation
    {
        const string MenuPath = "Tools/Scenario Forge/Validate All Scenarios";

        /// <summary>
        /// Validates every scenario, prints each problem and exits the editor with code 1 when any scenario has
        /// errors, or 0 otherwise.
        /// </summary>
        public static void ValidateAllScenarios()
        {
            int errors = Run(out _);
            EditorApplication.Exit(errors > 0 ? 1 : 0);
        }

        [MenuItem(MenuPath, priority = 100)]
        static void ValidateFromMenu()
        {
            int errors = Run(out string summary);
            EditorUtility.DisplayDialog("Scenario Forge validation", summary + (errors > 0 ? "\n\nSee the Console for details." : string.Empty), "OK");
        }

        /// <summary>Validates every scenario asset and logs each issue.</summary>
        /// <param name="summary">One line describing the result.</param>
        /// <returns>The total number of errors.</returns>
        internal static int Run(out string summary)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(ScenarioDefinition));
            int errors = 0, warnings = 0, scenariosWithErrors = 0;
            var log = new StringBuilder();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
                if (definition == null)
                    continue;

                ValidationReport report = EditorScenarioValidation.Validate(definition, checkSceneTargets: false);
                int scenarioErrors = report.Count(ValidationSeverity.Error);
                errors += scenarioErrors;
                warnings += report.Count(ValidationSeverity.Warning);
                if (scenarioErrors > 0)
                    scenariosWithErrors++;

                foreach (ValidationIssue issue in report.Issues)
                {
                    string line = $"[Scenario Forge] {issue.Severity}: {path}: {EditorScenarioValidation.FormatIssue(definition, issue)}";
                    switch (issue.Severity)
                    {
                        case ValidationSeverity.Error:
                            Debug.LogError(line, definition);
                            break;
                        case ValidationSeverity.Warning:
                            Debug.LogWarning(line, definition);
                            break;
                        default:
                            Debug.Log(line, definition);
                            break;
                    }

                    log.AppendLine(line);
                }
            }

            summary = $"Validated {guids.Length} scenario(s): {errors} error(s) in {scenariosWithErrors} scenario(s), {warnings} warning(s).";
            Debug.Log("[Scenario Forge] " + summary);
            return errors;
        }
    }
}
