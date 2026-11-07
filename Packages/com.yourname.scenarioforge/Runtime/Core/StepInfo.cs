namespace ScenarioForge
{
    /// <summary>
    /// Describes a step that has just started. Raised by <see cref="ScenarioRunner.StepStarted"/>.
    /// </summary>
    public readonly struct StepInfo
    {
        /// <summary>The step's unique id within the scenario.</summary>
        public readonly string StepId;

        /// <summary>Zero-based index of the step in the scenario.</summary>
        public readonly int Index;

        /// <summary>The step's display title.</summary>
        public readonly string Title;

        /// <summary>The instruction text shown to the player.</summary>
        public readonly string Instruction;

        /// <summary>One-based attempt number. Greater than 1 when the step is retried.</summary>
        public readonly int Attempt;

        /// <summary>Creates a new <see cref="StepInfo"/>.</summary>
        public StepInfo(string stepId, int index, string title, string instruction, int attempt)
        {
            StepId = stepId;
            Index = index;
            Title = title;
            Instruction = instruction;
            Attempt = attempt;
        }
    }
}
