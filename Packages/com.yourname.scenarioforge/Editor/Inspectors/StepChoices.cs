using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>Builds the step list shown when a scene component picks a step of the runner's scenario.</summary>
    static class StepChoices
    {
        internal readonly struct Choice
        {
            public readonly string Id;
            public readonly string Label;

            public Choice(string id, string label)
            {
                Id = id;
                Label = label;
            }
        }

        internal const string NoneLabel = "(none)";

        /// <summary>One choice per step that has an id, labelled "N. Title".</summary>
        internal static List<Choice> Build(ScenarioDefinition definition)
        {
            var choices = new List<Choice>();
            if (definition == null)
                return choices;

            IReadOnlyList<ScenarioStep> steps = definition.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                ScenarioStep step = steps[i];
                if (step == null || string.IsNullOrEmpty(step.Id))
                    continue;

                string title = string.IsNullOrWhiteSpace(step.Title) ? StepTypeCatalog.Describe(step.GetType()).DisplayName : step.Title;
                choices.Add(new Choice(step.Id, $"{i + 1}. {title}"));
            }

            return choices;
        }

        /// <summary>The label for <paramref name="stepId"/>, or a "missing step" label when the scenario has no such step.</summary>
        internal static string LabelFor(List<Choice> choices, string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return NoneLabel;

            foreach (Choice choice in choices)
            {
                if (choice.Id == stepId)
                    return choice.Label;
            }

            return $"(missing step {(stepId.Length > 8 ? stepId.Substring(0, 8) : stepId)})";
        }

        /// <summary>
        /// The scenario of the runner a scene component listens to: its assigned runner, or the first
        /// runner in the open scenes, matching what the component does at runtime.
        /// </summary>
        internal static ScenarioDefinition FindDefinition(Object component)
        {
            ScenarioRunnerBehaviour runner = null;
            if (component != null)
            {
                var serialized = new SerializedObject(component);
                runner = serialized.FindProperty("runner")?.objectReferenceValue as ScenarioRunnerBehaviour;
            }

            if (runner == null)
                runner = Object.FindAnyObjectByType<ScenarioRunnerBehaviour>();

            return runner != null ? runner.Definition : null;
        }
    }
}
