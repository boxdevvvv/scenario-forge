namespace ScenarioForge
{
    /// <summary>Comparison used by <see cref="BlackboardValue.TryCompare"/>.</summary>
    public enum CompareOperator
    {
        /// <summary>Values are equal. Floats use <c>Mathf.Approximately</c>.</summary>
        Equal,

        /// <summary>Values are not equal.</summary>
        NotEqual,

        /// <summary>Left is greater than right. Numbers only.</summary>
        Greater,

        /// <summary>Left is greater than or equal to right. Numbers only.</summary>
        GreaterOrEqual,

        /// <summary>Left is less than right. Numbers only.</summary>
        Less,

        /// <summary>Left is less than or equal to right. Numbers only.</summary>
        LessOrEqual
    }
}
