using System.Collections.Generic;
using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// A complete procedure: an ordered list of steps plus a title and description.
    /// </summary>
    /// <remarks>
    /// This asset only holds data. Run it with a <see cref="ScenarioRunner"/> (or a
    /// <see cref="ScenarioRunnerBehaviour"/> in a scene). In the editor, modify it through
    /// <c>SerializedObject</c> so that Undo and saving work.
    /// </remarks>
    [CreateAssetMenu(menuName = "Scenario Forge/Scenario", fileName = "NewScenario")]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        /// <summary>Serialized name of the steps list, for editor code that uses <c>SerializedObject</c>.</summary>
        public const string StepsPropertyName = nameof(steps);

        [SerializeField] string title;
        [SerializeField, TextArea] string description;
        [SerializeReference] List<ScenarioStep> steps = new();

        /// <summary>Display title of the scenario.</summary>
        public string Title => title;

        /// <summary>Longer description of the scenario.</summary>
        public string Description => description;

        /// <summary>The steps, in execution order. Elements can be <c>null</c> when a step type is missing.</summary>
        public IReadOnlyList<ScenarioStep> Steps => steps;

        /// <summary>Replaces the content of the definition. Used by tests and by JSON import.</summary>
        internal void SetContent(string newTitle, string newDescription, IEnumerable<ScenarioStep> newSteps)
        {
            title = newTitle;
            description = newDescription;
            steps = newSteps != null ? new List<ScenarioStep>(newSteps) : new List<ScenarioStep>();
        }
    }
}
