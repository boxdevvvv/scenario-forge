namespace ScenarioForge
{
    /// <summary>
    /// A player mistake reported with <see cref="ScenarioContext.ReportMistake"/>.
    /// Collected in <see cref="ScenarioRunner.Mistakes"/>.
    /// </summary>
    public readonly struct ScenarioMistake
    {
        /// <summary>Id of the step that was active, or <c>null</c> when no step was active.</summary>
        public readonly string StepId;

        /// <summary>Zero-based index of the step that was active, or -1 when no step was active.</summary>
        public readonly int StepIndex;

        /// <summary>What the player did wrong.</summary>
        public readonly string Message;

        /// <summary>Scenario time, in seconds, when the mistake was reported.</summary>
        public readonly float Time;

        /// <summary>Creates a new <see cref="ScenarioMistake"/>.</summary>
        public ScenarioMistake(string stepId, int stepIndex, string message, float time)
        {
            StepId = stepId;
            StepIndex = stepIndex;
            Message = message;
            Time = time;
        }
    }
}
