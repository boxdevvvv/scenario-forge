namespace ScenarioForge
{
    /// <summary>
    /// What the runner does when a step fails or times out.
    /// </summary>
    public enum FailurePolicy
    {
        /// <summary>End the step and move the whole scenario to <see cref="RunnerState.Failed"/>.</summary>
        FailScenario,

        /// <summary>Record the step as failed and continue with the next step.</summary>
        SkipStep,

        /// <summary>
        /// Exit and re-enter the step, up to <see cref="ScenarioStep.MaxRetries"/> extra attempts.
        /// When the retries run out it behaves like <see cref="FailScenario"/>.
        /// </summary>
        Retry
    }
}
