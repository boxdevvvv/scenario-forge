using System;

namespace ScenarioForge
{
    /// <summary>
    /// Places a <see cref="ScenarioStep"/> type in the Scenario Forge "Add Step" menu.
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable]
    /// [ScenarioStepMenu("Custom/Press Lever", Tooltip = "Waits until the lever reaches the target angle.")]
    /// public sealed class PressLeverStep : ScenarioStep { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ScenarioStepMenuAttribute : Attribute
    {
        /// <summary>The category used for types without the attribute.</summary>
        public const string DefaultCategory = "Other";

        /// <summary>Creates the attribute.</summary>
        /// <param name="path">Menu path in the form <c>"Category/Name"</c>. Nested categories use more slashes.</param>
        public ScenarioStepMenuAttribute(string path)
        {
            Path = path ?? string.Empty;
        }

        /// <summary>Menu path in the form <c>"Category/Name"</c>.</summary>
        public string Path { get; }

        /// <summary>Short description shown in the menu.</summary>
        public string Tooltip { get; set; }

        /// <summary>Everything before the last slash, or <see cref="DefaultCategory"/> when there is none.</summary>
        public string Category
        {
            get
            {
                int slash = Path.LastIndexOf('/');
                return slash > 0 ? Path.Substring(0, slash) : DefaultCategory;
            }
        }

        /// <summary>The text after the last slash.</summary>
        public string DisplayName
        {
            get
            {
                int slash = Path.LastIndexOf('/');
                return slash >= 0 ? Path.Substring(slash + 1) : Path;
            }
        }
    }
}
