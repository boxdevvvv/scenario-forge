using System;
using System.Collections.Generic;

namespace ScenarioForge
{
    /// <summary>
    /// A typed key/value store shared by the steps of one run.
    /// Supported value types: <c>bool</c>, <c>int</c>, <c>float</c> and <c>string</c>.
    /// </summary>
    public sealed class Blackboard
    {
        readonly Dictionary<string, BlackboardValue> values = new(StringComparer.Ordinal);

        /// <summary>Raised with the key whenever a value is added, changed or removed.</summary>
        public event Action<string> ValueChanged;

        /// <summary>The keys currently stored.</summary>
        public IReadOnlyCollection<string> Keys => values.Keys;

        /// <summary>Number of stored values.</summary>
        public int Count => values.Count;

        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>, replacing any previous value.</summary>
        /// <typeparam name="T"><c>bool</c>, <c>int</c>, <c>float</c> or <c>string</c>.</typeparam>
        /// <exception cref="ArgumentException">When the key is empty or <typeparamref name="T"/> is not supported.</exception>
        public void Set<T>(string key, T value) => SetValue(key, ToValue(value));

        /// <summary>Stores an already typed <see cref="BlackboardValue"/>.</summary>
        /// <exception cref="ArgumentException">When the key is empty.</exception>
        public void SetValue(string key, BlackboardValue value)
        {
            ValidateKey(key);
            if (values.TryGetValue(key, out BlackboardValue existing) && existing.Equals(value))
                return;

            values[key] = value;
            ValueChanged?.Invoke(key);
        }

        /// <summary>
        /// Reads the value stored under <paramref name="key"/>. Returns <c>false</c> (and does not throw)
        /// when the key is missing or the stored value has a different type than <typeparamref name="T"/>.
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            value = default;
            if (key == null || !values.TryGetValue(key, out BlackboardValue stored))
                return false;

            switch (stored.Type)
            {
                case BlackboardValueType.Bool when typeof(T) == typeof(bool):
                    value = (T)(object)stored.BoolValue;
                    return true;
                case BlackboardValueType.Int when typeof(T) == typeof(int):
                    value = (T)(object)stored.IntValue;
                    return true;
                case BlackboardValueType.Float when typeof(T) == typeof(float):
                    value = (T)(object)stored.FloatValue;
                    return true;
                case BlackboardValueType.String when typeof(T) == typeof(string):
                    value = (T)(object)stored.StringValue;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Reads the typed value stored under <paramref name="key"/> without allocating.</summary>
        public bool TryGetValue(string key, out BlackboardValue value)
        {
            if (key == null)
            {
                value = default;
                return false;
            }

            return values.TryGetValue(key, out value);
        }

        /// <summary>Whether a value is stored under <paramref name="key"/>.</summary>
        public bool Has(string key) => key != null && values.ContainsKey(key);

        /// <summary>Removes the value stored under <paramref name="key"/>, if any.</summary>
        public void Remove(string key)
        {
            if (key != null && values.Remove(key))
                ValueChanged?.Invoke(key);
        }

        /// <summary>Removes every value. Raises <see cref="ValueChanged"/> once per removed key.</summary>
        public void Clear()
        {
            if (values.Count == 0)
                return;

            var removed = new List<string>(values.Keys);
            values.Clear();
            foreach (string key in removed)
                ValueChanged?.Invoke(key);
        }

        static BlackboardValue ToValue<T>(T value)
        {
            if (typeof(T) == typeof(bool))
                return BlackboardValue.FromBool((bool)(object)value);
            if (typeof(T) == typeof(int))
                return BlackboardValue.FromInt((int)(object)value);
            if (typeof(T) == typeof(float))
                return BlackboardValue.FromFloat((float)(object)value);
            if (typeof(T) == typeof(string))
                return BlackboardValue.FromString((string)(object)value);
            if (typeof(T) == typeof(BlackboardValue))
                return (BlackboardValue)(object)value;

            throw new ArgumentException(
                $"Blackboard does not support values of type '{typeof(T).Name}'. Use bool, int, float or string.",
                nameof(value));
        }

        static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Blackboard keys cannot be empty. Give the value a name such as \"door_open\".", nameof(key));
        }
    }
}
