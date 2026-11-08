namespace ScenarioForge
{
    /// <summary>
    /// The final result of one step. Raised by <see cref="ScenarioRunner.StepEnded"/>
    /// and collected in <see cref="ScenarioRunner.Results"/>.
    /// </summary>
    public readonly struct StepResult
    {
        /// <summary>The step's unique id within the scenario.</summary>
        public readonly string StepId;

        /// <summary>Zero-based index of the step in the scenario.</summary>
        public readonly int Index;

        /// <summary>How the step ended.</summary>
        public readonly StepOutcome Outcome;

        /// <summary>Seconds spent in the step's last attempt.</summary>
        public readonly float Duration;

        /// <summary>How many times the step was entered (1 unless it was retried).</summary>
        public readonly int Attempts;

        /// <summary>Why the step failed or timed out; <c>null</c> when it did not fail.</summary>
        public readonly string FailureReason;

        /// <summary>Mistakes reported while the step was active, across all its attempts.</summary>
        public readonly int Mistakes;

        /// <summary>Creates a new <see cref="StepResult"/>.</summary>
        public StepResult(string stepId, int index, StepOutcome outcome, float duration, int attempts, string failureReason, int mistakes = 0)
        {
            Mistakes = mistakes;
            StepId = stepId;
            Index = index;
            Outcome = outcome;
            Duration = duration;
            Attempts = attempts;
            FailureReason = failureReason;
        }
    }
}
