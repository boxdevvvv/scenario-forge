using System;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// Watches one <see cref="ScenarioRunnerBehaviour"/> for the Play Mode debugger and writes its events
    /// (runs, state changes, steps, signals and mistakes) to a <see cref="DebugEventLog"/>.
    /// </summary>
    sealed class RunnerDebugSession : IDisposable
    {
        readonly DebugEventLog log;
        ScenarioRunner observedRunner;
        bool isDisposed;

        public RunnerDebugSession(ScenarioRunnerBehaviour behaviour, DebugEventLog log)
        {
            Behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            Behaviour.StepStarted += OnStepStarted;
            Behaviour.StepEnded += OnStepEnded;
            Behaviour.SignalRaised += OnSignalRaised;
            Behaviour.MistakeReported += OnMistakeReported;
            CheckRunner();
        }

        public ScenarioRunnerBehaviour Behaviour { get; }

        /// <summary>The runner of the current run, or <c>null</c> before it starts or once the behaviour is destroyed.</summary>
        public ScenarioRunner Runner => Behaviour != null ? Behaviour.Runner : null;

        /// <summary>Picks up a new run (for example after <see cref="ScenarioRunnerBehaviour.StartScenario"/>). Call it regularly.</summary>
        public void Poll() => CheckRunner();

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            Observe(null);
            if (Behaviour == null)
                return;

            Behaviour.StepStarted -= OnStepStarted;
            Behaviour.StepEnded -= OnStepEnded;
            Behaviour.SignalRaised -= OnSignalRaised;
            Behaviour.MistakeReported -= OnMistakeReported;
        }

        void CheckRunner()
        {
            if (isDisposed)
                return;

            ScenarioRunner current = Runner;
            if (current == observedRunner)
                return;

            Observe(current);
            if (current == null)
                return;

            string scenarioName = current.Definition != null ? current.Definition.name : "(no scenario)";
            log.Add(Now, DebugEventKind.Run, $"Watching a run of \"{scenarioName}\" on '{Behaviour.name}'.");
            if (current.State != RunnerState.NotStarted)
                log.Add(Now, DebugEventKind.State, $"State: {current.State}.");
        }

        void Observe(ScenarioRunner runner)
        {
            if (observedRunner != null)
                observedRunner.StateChanged -= OnStateChanged;
            observedRunner = runner;
            if (observedRunner != null)
                observedRunner.StateChanged += OnStateChanged;
        }

        float Now => observedRunner?.Context != null ? observedRunner.Context.TotalElapsed : 0f;

        string Describe(int index)
        {
            ScenarioDefinition definition = observedRunner?.Definition;
            ScenarioStep step = definition != null && index >= 0 && index < definition.Steps.Count ? definition.Steps[index] : null;
            string title = step == null ? "(missing type)" : string.IsNullOrWhiteSpace(step.Title) ? StepTypeCatalog.Describe(step.GetType()).DisplayName : step.Title;
            return $"Step {index + 1} \"{title}\"";
        }

        void OnStateChanged(RunnerState state) => log.Add(Now, DebugEventKind.State, $"State: {state}.");

        void OnStepStarted(StepInfo info)
        {
            CheckRunner();
            log.Add(Now, DebugEventKind.StepStarted, info.Attempt > 1 ? $"{Describe(info.Index)} started (attempt {info.Attempt})." : $"{Describe(info.Index)} started.");
        }

        void OnStepEnded(StepResult result)
        {
            CheckRunner();
            string message = $"{Describe(result.Index)} {result.Outcome} after {result.Duration:0.0}s.";
            if (!string.IsNullOrEmpty(result.FailureReason))
                message += " " + result.FailureReason;
            log.Add(Now, DebugEventKind.StepEnded, message);
        }

        void OnSignalRaised(string signal)
        {
            CheckRunner();
            log.Add(Now, DebugEventKind.Signal, $"Signal \"{signal}\" raised.");
        }

        void OnMistakeReported(ScenarioMistake mistake)
        {
            CheckRunner();
            log.Add(Now, DebugEventKind.Mistake, mistake.StepIndex >= 0 ? $"{Describe(mistake.StepIndex)}: {mistake.Message}" : mistake.Message);
        }
    }
}
