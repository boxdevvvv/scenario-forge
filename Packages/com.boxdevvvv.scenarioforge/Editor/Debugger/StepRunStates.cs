using System.Collections.Generic;

namespace ScenarioForge.Editor
{
    /// <summary>Run state of one step, as shown in the Play Mode debugger.</summary>
    enum StepRunState
    {
        Pending,
        Active,
        Succeeded,
        Failed,
        Skipped,
        TimedOut,
        Aborted
    }

    /// <summary>Works out each step's <see cref="StepRunState"/> from a runner.</summary>
    static class StepRunStates
    {
        /// <summary>
        /// Fills <paramref name="states"/> (resized to <paramref name="stepCount"/>) from the runner's results and
        /// active step. A step's latest result wins, so a step entered again after a restart shows its new state.
        /// </summary>
        internal static void Compute(ScenarioRunner runner, int stepCount, List<StepRunState> states)
        {
            states.Clear();
            for (int i = 0; i < stepCount; i++)
                states.Add(StepRunState.Pending);

            if (runner == null)
                return;

            foreach (StepResult result in runner.Results)
            {
                if (result.Index >= 0 && result.Index < stepCount)
                    states[result.Index] = FromOutcome(result.Outcome);
            }

            bool active = runner.State == RunnerState.Running || runner.State == RunnerState.Paused;
            if (active && runner.CurrentStepIndex >= 0 && runner.CurrentStepIndex < stepCount)
                states[runner.CurrentStepIndex] = StepRunState.Active;
        }

        internal static StepRunState FromOutcome(StepOutcome outcome) => outcome switch
        {
            StepOutcome.Succeeded => StepRunState.Succeeded,
            StepOutcome.Failed => StepRunState.Failed,
            StepOutcome.Skipped => StepRunState.Skipped,
            StepOutcome.TimedOut => StepRunState.TimedOut,
            _ => StepRunState.Aborted
        };

        /// <summary>The symbol shown at the end of the row.</summary>
        internal static string Symbol(StepRunState state) => state switch
        {
            StepRunState.Succeeded => "✓",
            StepRunState.Failed => "✕",
            StepRunState.Skipped => "↷",
            StepRunState.TimedOut => "⏱",
            StepRunState.Aborted => "■",
            _ => string.Empty
        };

        /// <summary>The USS class added to the row.</summary>
        internal static string UssClass(StepRunState state) => "sf-row--run-" + state switch
        {
            StepRunState.Pending => "pending",
            StepRunState.Active => "active",
            StepRunState.Succeeded => "succeeded",
            StepRunState.Failed => "failed",
            StepRunState.Skipped => "skipped",
            StepRunState.TimedOut => "timedout",
            _ => "aborted"
        };

        internal static readonly string[] AllUssClasses =
        {
            "sf-row--run-pending", "sf-row--run-active", "sf-row--run-succeeded", "sf-row--run-failed",
            "sf-row--run-skipped", "sf-row--run-timedout", "sf-row--run-aborted"
        };
    }
}
