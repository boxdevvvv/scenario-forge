using System;
using UnityEditor;

namespace ScenarioForge.Editor
{
    /// <summary>Display information about a step type, read from its <see cref="ScenarioStepMenuAttribute"/>.</summary>
    sealed class StepTypeInfo
    {
        public StepTypeInfo(Type type)
        {
            Type = type;
            var attribute = (ScenarioStepMenuAttribute)Attribute.GetCustomAttribute(type, typeof(ScenarioStepMenuAttribute), false);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Path))
            {
                Category = attribute.Category;
                DisplayName = attribute.DisplayName;
                Tooltip = attribute.Tooltip ?? string.Empty;
            }
            else
            {
                Category = ScenarioStepMenuAttribute.DefaultCategory;
                DisplayName = ObjectNames.NicifyVariableName(type.Name);
                Tooltip = string.Empty;
            }
        }

        public Type Type { get; }

        public string Category { get; }

        public string DisplayName { get; }

        public string Tooltip { get; }

        /// <summary>The first segment of the category, used for the row icon.</summary>
        public string RootCategory
        {
            get
            {
                int slash = Category.IndexOf('/');
                return slash > 0 ? Category.Substring(0, slash) : Category;
            }
        }
    }
}
