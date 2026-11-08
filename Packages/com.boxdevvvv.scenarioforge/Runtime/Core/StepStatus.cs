namespace ScenarioForge
{
    /// <summary>
    /// The value a step returns from <see cref="ScenarioStep.OnTick"/> to tell the runner how it is doing.
    /// </summary>
    public enum StepStatus
    {
        /// <summary>The step is still in progress and will be ticked again.</summary>
        Running,

        /// <summary>The step finished successfully. The runner moves to the next step.</summary>
        Succeeded,

        /// <summary>The step failed. The runner applies the step's <see cref="FailurePolicy"/>.</summary>
        Failed
    }
}
