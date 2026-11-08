using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>Helpers around managed references whose class no longer exists.</summary>
    /// <remarks>
    /// Once a type is missing, Unity reports the list element as a null reference (id -2), so the element no
    /// longer says which stored reference it pointed to. The order is still in the asset file, under the
    /// steps field, so it is read from there.
    /// </remarks>
    static class MissingTypes
    {
        static readonly Regex RidLine = new(@"^\s*-\s*rid:\s*(-?\d+)\s*$");

        /// <summary>Whether <paramref name="asset"/> has managed references with missing types.</summary>
        public static bool HasAny(Object asset) => asset != null && SerializationUtility.HasManagedReferencesWithMissingTypes(asset);

        /// <summary>
        /// For every element of the steps list that points to a missing type: its index, stored reference id
        /// and type name.
        /// </summary>
        public static IReadOnlyDictionary<int, (long ReferenceId, string TypeName)> FindInSteps(ScenarioDefinition definition)
        {
            var result = new Dictionary<int, (long, string)>();
            if (!HasAny(definition))
                return result;

            var missingById = new Dictionary<long, string>();
            var missingInOrder = new List<long>();
            foreach (ManagedReferenceMissingType missing in SerializationUtility.GetManagedReferencesWithMissingTypes(definition))
            {
                missingById[missing.referenceId] = Describe(missing);
                missingInOrder.Add(missing.referenceId);
            }

            List<long> stepIds = ReadStepReferenceIds(definition);
            IReadOnlyList<ScenarioStep> steps = definition.Steps;
            int fallback = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    continue;

                long referenceId;
                if (stepIds != null && i < stepIds.Count && missingById.ContainsKey(stepIds[i]))
                    referenceId = stepIds[i];
                else if (stepIds == null && fallback < missingInOrder.Count)
                    referenceId = missingInOrder[fallback++];
                else
                    continue;

                result[i] = (referenceId, missingById[referenceId]);
            }

            return result;
        }

        /// <summary>The name of the missing type at step <paramref name="index"/>, or <c>null</c>.</summary>
        public static string GetTypeNameAt(ScenarioDefinition definition, int index)
        {
            return FindInSteps(definition).TryGetValue(index, out var missing) ? missing.TypeName : null;
        }

        static string Describe(ManagedReferenceMissingType missing)
        {
            string name = string.IsNullOrEmpty(missing.namespaceName) ? missing.className : missing.namespaceName + "." + missing.className;
            return string.IsNullOrEmpty(missing.assemblyName) ? name : $"{name} ({missing.assemblyName})";
        }

        /// <summary>The reference ids listed under the steps field of the asset file, in order.</summary>
        static List<long> ReadStepReferenceIds(ScenarioDefinition definition)
        {
            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            var ids = new List<long>();
            bool inSteps = false;
            foreach (string line in File.ReadLines(path))
            {
                if (!inSteps)
                {
                    inSteps = line.TrimEnd() == "  " + ScenarioDefinition.StepsPropertyName + ":";
                    continue;
                }

                Match match = RidLine.Match(line);
                if (!match.Success)
                    break;

                ids.Add(long.Parse(match.Groups[1].Value));
            }

            return inSteps ? ids : null;
        }
    }
}
