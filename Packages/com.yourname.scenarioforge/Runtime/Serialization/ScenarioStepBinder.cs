using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ScenarioForge.Serialization
{
    /// <summary>
    /// Resolves <c>$type</c> names, accepting only non-abstract <see cref="ScenarioStep"/> types from assemblies
    /// that are already loaded. Any other type is rejected before anything is created, so a JSON file cannot
    /// instantiate arbitrary types.
    /// </summary>
    sealed class ScenarioStepBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            if (!TryResolve(assemblyName, typeName, out Type type, out string error))
                throw new JsonSerializationException(error);
            return type;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly.GetName().Name;
            typeName = serializedType.FullName;
        }

        /// <summary>Checks a <c>$type</c> name without throwing.</summary>
        internal static bool TryResolve(string assemblyName, string typeName, out Type type, out string error)
        {
            type = Find(assemblyName, typeName);
            string display = string.IsNullOrEmpty(assemblyName) ? typeName : $"{typeName}, {assemblyName}";
            if (type == null)
            {
                error = $"Unknown step type \"{display}\". Check the spelling, or add the package or script that defines it.";
                return false;
            }

            if (!typeof(ScenarioStep).IsAssignableFrom(type) || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                type = null;
                error = $"Type \"{display}\" is not allowed. Only non-abstract ScenarioStep types can be imported.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Splits <c>"Namespace.Type, Assembly"</c> at the first top-level comma.</summary>
        internal static void Split(string fullName, out string typeName, out string assemblyName)
        {
            int depth = 0;
            for (int i = 0; i < fullName.Length; i++)
            {
                char c = fullName[i];
                if (c == '[') depth++;
                else if (c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    typeName = fullName.Substring(0, i).Trim();
                    assemblyName = fullName.Substring(i + 1).Trim();
                    int versionStart = assemblyName.IndexOf(',');
                    if (versionStart >= 0)
                        assemblyName = assemblyName.Substring(0, versionStart).Trim();
                    return;
                }
            }

            typeName = fullName.Trim();
            assemblyName = null;
        }

        static Type Find(string assemblyName, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.IsNullOrEmpty(assemblyName) && assembly.GetName().Name != assemblyName)
                    continue;

                Type type = assembly.GetType(typeName, false);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
