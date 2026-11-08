using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace ScenarioForge.Json
{
    /// <summary>
    /// Serializes the same fields Unity serializes: public fields and private fields with
    /// <see cref="SerializeField"/>, from the whole class hierarchy (base class first), skipping
    /// <see cref="NonSerializedAttribute"/>, readonly, static and <see cref="UnityEngine.Object"/> fields.
    /// Properties are never serialized.
    /// </summary>
    sealed class UnitySerializationContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var hierarchy = new Stack<Type>();
            for (Type type = objectType; type != null && type != typeof(object); type = type.BaseType)
                hierarchy.Push(type);

            var members = new List<MemberInfo>();
            foreach (Type type in hierarchy)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (IsUnitySerialized(field))
                        members.Add(field);
                }
            }

            return members;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            property.Readable = true;
            property.Writable = true;
            property.Ignored = false;
            return property;
        }

        static bool IsUnitySerialized(FieldInfo field)
        {
            if (field.IsNotSerialized || field.IsInitOnly || field.IsLiteral)
                return false;

            if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                return false;

            return field.IsPublic || field.IsDefined(typeof(SerializeField), false);
        }
    }
}
