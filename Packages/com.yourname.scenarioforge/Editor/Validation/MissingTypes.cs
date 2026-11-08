using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>Helpers around managed references whose class no longer exists.</summary>
    static class MissingTypes
    {
        /// <summary>Whether <paramref name="asset"/> has managed references with missing types.</summary>
        public static bool HasAny(Object asset) => asset != null && SerializationUtility.HasManagedReferencesWithMissingTypes(asset);

        /// <summary>The full name of the missing class referenced by <paramref name="referenceId"/>, or <c>null</c>.</summary>
        public static string GetTypeName(Object asset, long referenceId)
        {
            if (!HasAny(asset))
                return null;

            foreach (ManagedReferenceMissingType missing in SerializationUtility.GetManagedReferencesWithMissingTypes(asset))
            {
                if (missing.referenceId != referenceId)
                    continue;

                string name = string.IsNullOrEmpty(missing.namespaceName) ? missing.className : missing.namespaceName + "." + missing.className;
                return string.IsNullOrEmpty(missing.assemblyName) ? name : $"{name} ({missing.assemblyName})";
            }

            return null;
        }
    }
}
