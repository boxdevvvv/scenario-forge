using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace ScenarioForge.Editor
{
    /// <summary>Finds the step types that can be added to a scenario.</summary>
    static class StepTypeCatalog
    {
        /// <summary>
        /// Every public, non-abstract, <c>[Serializable]</c> <see cref="ScenarioStep"/> type with a public
        /// parameterless constructor, ordered by category and name.
        /// </summary>
        public static IReadOnlyList<StepTypeInfo> GetStepTypes()
        {
            return TypeCache.GetTypesDerivedFrom<ScenarioStep>()
                .Where(IsCreatable)
                .Select(Describe)
                .OrderBy(info => info.Category, StringComparer.Ordinal)
                .ThenBy(info => info.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Whether <paramref name="type"/> meets the requirements to appear in the Add Step menu.</summary>
        public static bool IsCreatable(Type type)
        {
            return type != null
                && typeof(ScenarioStep).IsAssignableFrom(type)
                && !type.IsAbstract
                && !type.IsGenericTypeDefinition
                && type.IsVisible
                && type.IsSerializable
                && type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>Menu information for <paramref name="type"/>.</summary>
        public static StepTypeInfo Describe(Type type) => new(type);
    }
}
