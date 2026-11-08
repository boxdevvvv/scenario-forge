using System.Collections.Generic;
using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// A scenario read from JSON by <see cref="ScenarioJson.TryImport"/>. Turn it into a runnable asset
    /// with <see cref="CreateDefinition"/>, or merge it into an existing asset from the editor.
    /// </summary>
    public sealed class ScenarioData
    {
        readonly List<ScenarioStep> steps;

        internal ScenarioData(string title, string description, List<ScenarioStep> steps)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            this.steps = steps ?? new List<ScenarioStep>();
        }

        /// <summary>The scenario title.</summary>
        public string Title { get; }

        /// <summary>The scenario description.</summary>
        public string Description { get; }

        /// <summary>The imported steps, in order. Never contains <c>null</c>.</summary>
        public IReadOnlyList<ScenarioStep> Steps => steps;

        /// <summary>
        /// Creates an in-memory <see cref="ScenarioDefinition"/> with this content, for example to run a
        /// scenario downloaded at runtime. The definition takes these step objects, so create one definition
        /// per import. Destroy it when you no longer need it.
        /// </summary>
        public ScenarioDefinition CreateDefinition()
        {
            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.name = string.IsNullOrWhiteSpace(Title) ? "Imported Scenario" : Title;
            definition.SetContent(Title, Description, steps);
            return definition;
        }
    }
}
