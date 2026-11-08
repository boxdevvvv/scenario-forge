namespace ScenarioForge
{
    /// <summary>How serious a <see cref="ValidationIssue"/> is.</summary>
    public enum ValidationSeverity
    {
        /// <summary>A hint. The scenario works as authored.</summary>
        Info,

        /// <summary>Probably a mistake, but the scenario can still run.</summary>
        Warning,

        /// <summary>The scenario will not behave correctly. CI validation fails on errors.</summary>
        Error
    }
}
