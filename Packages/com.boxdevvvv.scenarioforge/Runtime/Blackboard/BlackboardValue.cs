using System;
using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// A serializable, typed value for the <see cref="Blackboard"/>: one of <c>bool</c>, <c>int</c>,
    /// <c>float</c> or <c>string</c>, chosen by <see cref="Type"/>.
    /// </summary>
    [Serializable]
    public struct BlackboardValue : IEquatable<BlackboardValue>
    {
        [SerializeField] BlackboardValueType type;
        [SerializeField] bool boolValue;
        [SerializeField] int intValue;
        [SerializeField] float floatValue;
        [SerializeField] string stringValue;

        /// <summary>Which of the typed fields holds the value.</summary>
        public BlackboardValueType Type => type;

        /// <summary>The value when <see cref="Type"/> is <see cref="BlackboardValueType.Bool"/>.</summary>
        public bool BoolValue => boolValue;

        /// <summary>The value when <see cref="Type"/> is <see cref="BlackboardValueType.Int"/>.</summary>
        public int IntValue => intValue;

        /// <summary>The value when <see cref="Type"/> is <see cref="BlackboardValueType.Float"/>.</summary>
        public float FloatValue => floatValue;

        /// <summary>The value when <see cref="Type"/> is <see cref="BlackboardValueType.String"/>. Never <c>null</c>.</summary>
        public string StringValue => stringValue ?? string.Empty;

        /// <summary>Creates a <c>bool</c> value.</summary>
        public static BlackboardValue FromBool(bool value) => new() { type = BlackboardValueType.Bool, boolValue = value };

        /// <summary>Creates an <c>int</c> value.</summary>
        public static BlackboardValue FromInt(int value) => new() { type = BlackboardValueType.Int, intValue = value };

        /// <summary>Creates a <c>float</c> value.</summary>
        public static BlackboardValue FromFloat(float value) => new() { type = BlackboardValueType.Float, floatValue = value };

        /// <summary>Creates a <c>string</c> value. <c>null</c> becomes an empty string.</summary>
        public static BlackboardValue FromString(string value) => new() { type = BlackboardValueType.String, stringValue = value ?? string.Empty };

        /// <summary>Whether <paramref name="op"/> can be used with values of <paramref name="valueType"/>.</summary>
        /// <remarks>Equality works with every type; ordering operators only with <c>int</c> and <c>float</c>.</remarks>
        public static bool IsOperatorSupported(BlackboardValueType valueType, CompareOperator op)
        {
            if (op == CompareOperator.Equal || op == CompareOperator.NotEqual)
                return true;

            return valueType == BlackboardValueType.Int || valueType == BlackboardValueType.Float;
        }

        /// <summary>
        /// Compares this value (left) with <paramref name="other"/> (right).
        /// </summary>
        /// <param name="op">The comparison to apply.</param>
        /// <param name="other">The right-hand value.</param>
        /// <param name="result">The comparison result when the method returns <c>true</c>.</param>
        /// <returns><c>false</c> when the types differ or the operator does not support the type.</returns>
        public bool TryCompare(CompareOperator op, BlackboardValue other, out bool result)
        {
            result = false;
            if (type != other.type || !IsOperatorSupported(type, op))
                return false;

            switch (type)
            {
                case BlackboardValueType.Bool:
                    result = op == CompareOperator.Equal ? boolValue == other.boolValue : boolValue != other.boolValue;
                    return true;

                case BlackboardValueType.String:
                    bool same = string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
                    result = op == CompareOperator.Equal ? same : !same;
                    return true;

                case BlackboardValueType.Int:
                    result = Evaluate(op, intValue.CompareTo(other.intValue), intValue == other.intValue);
                    return true;

                case BlackboardValueType.Float:
                    result = Evaluate(op, floatValue.CompareTo(other.floatValue), Mathf.Approximately(floatValue, other.floatValue));
                    return true;

                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public bool Equals(BlackboardValue other)
        {
            if (type != other.type)
                return false;

            return type switch
            {
                BlackboardValueType.Bool => boolValue == other.boolValue,
                BlackboardValueType.Int => intValue == other.intValue,
                BlackboardValueType.Float => floatValue.Equals(other.floatValue),
                _ => string.Equals(StringValue, other.StringValue, StringComparison.Ordinal)
            };
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is BlackboardValue other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return type switch
            {
                BlackboardValueType.Bool => HashCode.Combine(type, boolValue),
                BlackboardValueType.Int => HashCode.Combine(type, intValue),
                BlackboardValueType.Float => HashCode.Combine(type, floatValue),
                _ => HashCode.Combine(type, StringValue)
            };
        }

        /// <summary>The value formatted as text, for logs and debugging UI.</summary>
        public override string ToString()
        {
            return type switch
            {
                BlackboardValueType.Bool => boolValue ? "true" : "false",
                BlackboardValueType.Int => intValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                BlackboardValueType.Float => floatValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                _ => StringValue
            };
        }

        static bool Evaluate(CompareOperator op, int comparison, bool equal)
        {
            return op switch
            {
                CompareOperator.Equal => equal,
                CompareOperator.NotEqual => !equal,
                CompareOperator.Greater => !equal && comparison > 0,
                CompareOperator.GreaterOrEqual => equal || comparison > 0,
                CompareOperator.Less => !equal && comparison < 0,
                CompareOperator.LessOrEqual => equal || comparison < 0,
                _ => false
            };
        }
    }
}
