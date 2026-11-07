namespace ScenarioForge
{
    /// <summary>
    /// The lifecycle state of a <see cref="ScenarioRunner"/>.
    /// </summary>
    public enum RunnerState
    {
        /// <summary>Created but <see cref="ScenarioRunner.Start"/> has not been called yet.</summary>
        NotStarted,

        /// <summary>Executing steps. <see cref="ScenarioRunner.Tick"/> advances the active step.</summary>
        Running,

        /// <summary>Paused with <see cref="ScenarioRunner.Pause"/>. Ticks do not advance time.</summary>
        Paused,

        /// <summary>Every step finished. Terminal state.</summary>
        Completed,

        /// <summary>A step failed with <see cref="FailurePolicy.FailScenario"/> or ran out of retries. Terminal state.</summary>
        Failed,

        /// <summary>Stopped with <see cref="ScenarioRunner.Abort"/>. Terminal state.</summary>
        Aborted
    }
}
