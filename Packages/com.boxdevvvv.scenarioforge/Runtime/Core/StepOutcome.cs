namespace ScenarioForge
{
    /// <summary>
    /// How a step ended. Passed to <see cref="ScenarioStep.OnExit"/> and recorded in <see cref="StepResult"/>.
    /// </summary>
    public enum StepOutcome
    {
        /// <summary>The step completed successfully.</summary>
        Succeeded,

        /// <summary>The step reported a failure, or threw an exception.</summary>
        Failed,

        /// <summary>The step was skipped with <see cref="ScenarioRunner.SkipCurrentStep"/>.</summary>
        Skipped,

        /// <summary>The step exceeded its <see cref="ScenarioStep.TimeoutSeconds"/>.</summary>
        TimedOut,

        /// <summary>The scenario was aborted while the step was active.</summary>
        Aborted
    }
}
