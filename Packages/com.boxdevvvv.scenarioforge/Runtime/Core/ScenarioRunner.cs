using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScenarioForge
{
    /// <summary>
    /// Executes the steps of a <see cref="ScenarioDefinition"/> in order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runner is plain C#: call <see cref="Tick"/> from your own loop, or use
    /// <see cref="ScenarioRunnerBehaviour"/> to drive it from a scene. It always works on a copy of the
    /// definition (made with <c>Object.Instantiate</c> in <see cref="Start"/>), so running a scenario never
    /// modifies the asset.
    /// </para>
    /// <para>
    /// Event order at a step transition is guaranteed: <see cref="StepEnded"/> for the old step,
    /// then <see cref="StepStarted"/> for the new one, then <see cref="InstructionChanged"/>.
    /// </para>
    /// </remarks>
    public sealed class ScenarioRunner : IDisposable
    {
        readonly ScenarioDefinition definition;
        readonly List<StepResult> results = new();
        readonly List<ScenarioMistake> mistakes = new();
        readonly Action<string> onSignalRaised;
        readonly Action<string> onMistakeReportRequested;

        ScenarioContext context;
        ScenarioDefinition runtimeCopy;
        IReadOnlyList<ScenarioStep> steps;
        ScenarioStep currentStep;
        string currentInstruction;
        int attempt;
        int stepMistakes;
        bool isEnteringStep;
        bool isDisposed;

        /// <summary>Creates a runner for <paramref name="definition"/>. Nothing runs until <see cref="Start"/>.</summary>
        /// <param name="definition">The scenario to run. It is copied in <see cref="Start"/> and never modified.</param>
        /// <param name="context">The context for the first run. <see cref="Restart"/> creates a new one.</param>
        /// <exception cref="ArgumentNullException">When either argument is <c>null</c>.</exception>
        public ScenarioRunner(ScenarioDefinition definition, ScenarioContext context)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition), "Assign a ScenarioDefinition asset before creating a ScenarioRunner.");

            this.definition = definition;
            this.context = context ?? throw new ArgumentNullException(nameof(context), "Pass a ScenarioContext, for example new ScenarioContext().");
            CurrentStepIndex = -1;
            onSignalRaised = OnSignalRaised;
            onMistakeReportRequested = OnMistakeReportRequested;
        }

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<RunnerState> StateChanged;

        /// <summary>Raised when a step becomes active, including when it is retried.</summary>
        public event Action<StepInfo> StepStarted;

        /// <summary>Raised when a step ends for good (not between retries).</summary>
        public event Action<StepResult> StepEnded;

        /// <summary>Raised with the instruction text of the active step whenever it changes.</summary>
        public event Action<string> InstructionChanged;

        /// <summary>
        /// Raised for every signal of the current run, whoever raised it (the game or a step such as
        /// <see cref="Steps.SendSignalStep"/>). Keeps working across <see cref="Restart"/>.
        /// </summary>
        public event Action<string> SignalRaised;

        /// <summary>Raised when a mistake is reported with <see cref="ScenarioContext.ReportMistake"/>.</summary>
        public event Action<ScenarioMistake> MistakeReported;

        /// <summary>The asset this runner was created for. Never modified by the runner.</summary>
        public ScenarioDefinition Definition => definition;

        /// <summary>The context of the current run. Replaced by <see cref="Restart"/>.</summary>
        public ScenarioContext Context => context;

        /// <summary>The current lifecycle state.</summary>
        public RunnerState State { get; private set; }

        /// <summary>Zero-based index of the active step, or <c>-1</c> when no step is active.</summary>
        public int CurrentStepIndex { get; private set; }

        /// <summary>The active step (a runtime copy), or <c>null</c> when no step is active.</summary>
        public ScenarioStep CurrentStep => currentStep;

        /// <summary>Seconds spent in the active step's current attempt.</summary>
        public float CurrentStepElapsed { get; private set; }

        /// <summary>The final result of every step that has ended, in order.</summary>
        public IReadOnlyList<StepResult> Results => results;

        /// <summary>Mistakes reported during the current run, in the order they happened.</summary>
        public IReadOnlyList<ScenarioMistake> Mistakes => mistakes;

        bool IsActive => State == RunnerState.Running || State == RunnerState.Paused;

        /// <summary>
        /// Starts the scenario. Only works from <see cref="RunnerState.NotStarted"/>.
        /// A scenario without steps goes straight to <see cref="RunnerState.Completed"/>.
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();
            if (State != RunnerState.NotStarted)
                return;

            if (definition == null)
                throw new InvalidOperationException("The ScenarioDefinition was destroyed before the runner started. Keep the asset loaded while it runs.");

            runtimeCopy = Object.Instantiate(definition);
            runtimeCopy.name = definition.name + " (Runtime Copy)";
            runtimeCopy.hideFlags = HideFlags.HideAndDontSave;
            steps = runtimeCopy.Steps;

            context.InstructionUpdateRequested += OnInstructionUpdateRequested;
            context.MistakeReportRequested += onMistakeReportRequested;
            context.Signals.Raised += onSignalRaised;
            SetState(RunnerState.Running);

            if (steps.Count == 0)
            {
                Finish(RunnerState.Completed);
                return;
            }

            BeginStep(0);
        }

        /// <summary>
        /// Advances the active step by <paramref name="deltaTime"/> seconds. Does nothing unless the
        /// state is <see cref="RunnerState.Running"/>.
        /// </summary>
        /// <param name="deltaTime">Seconds since the previous tick.</param>
        public void Tick(float deltaTime)
        {
            if (State != RunnerState.Running || currentStep == null)
                return;

            ScenarioStep step = currentStep;
            CurrentStepElapsed += deltaTime;
            context.TotalElapsed += deltaTime;

            StepStatus status;
            string exceptionMessage = null;
            try
            {
                status = step.OnTick(context, deltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, definition);
                status = StepStatus.Failed;
                exceptionMessage = exception.Message;
            }

            if (State != RunnerState.Running || step != currentStep)
                return;

            switch (status)
            {
                case StepStatus.Succeeded:
                    EndStep(StepOutcome.Succeeded, null);
                    if (State == RunnerState.Running)
                        Advance();
                    break;

                case StepStatus.Failed:
                    HandleFailure(StepOutcome.Failed, exceptionMessage ?? step.FailureReason ?? $"Step '{DescribeStep(step, CurrentStepIndex)}' reported a failure.");
                    break;

                default:
                    if (step.TimeoutSeconds > 0f && CurrentStepElapsed >= step.TimeoutSeconds)
                        HandleFailure(StepOutcome.TimedOut, $"Step '{DescribeStep(step, CurrentStepIndex)}' timed out after {step.TimeoutSeconds:0.##} s.");
                    break;
            }
        }

        /// <summary>Pauses the scenario. Only works from <see cref="RunnerState.Running"/>.</summary>
        public void Pause()
        {
            if (State == RunnerState.Running)
                SetState(RunnerState.Paused);
        }

        /// <summary>Resumes a paused scenario. Only works from <see cref="RunnerState.Paused"/>.</summary>
        public void Resume()
        {
            if (State == RunnerState.Paused)
                SetState(RunnerState.Running);
        }

        /// <summary>
        /// Stops the scenario: calls <see cref="ScenarioStep.OnExit"/> with <see cref="StepOutcome.Aborted"/>
        /// on the active step and moves to <see cref="RunnerState.Aborted"/>. Does nothing if the scenario
        /// already ended.
        /// </summary>
        public void Abort()
        {
            if (State == RunnerState.NotStarted)
            {
                SetState(RunnerState.Aborted);
                return;
            }

            if (!IsActive)
                return;

            if (currentStep != null)
                EndStep(StepOutcome.Aborted, null);

            if (IsActive)
                Finish(RunnerState.Aborted);
        }

        /// <summary>
        /// Debugging aid: ends the active step with <see cref="StepOutcome.Skipped"/> and moves to the next one.
        /// Works while running or paused.
        /// </summary>
        public void SkipCurrentStep()
        {
            if (!IsActive || CurrentStepIndex < 0)
                return;

            EndStep(StepOutcome.Skipped, null);
            if (IsActive)
                Advance();
        }

        /// <summary>
        /// Aborts the current run (if any) and starts again from the first step with a new
        /// <see cref="ScenarioContext"/> that keeps the same targets and services.
        /// </summary>
        public void Restart()
        {
            ThrowIfDisposed();
            if (IsActive)
                Abort();

            DetachContext();
            DestroyRuntimeCopy();
            results.Clear();
            mistakes.Clear();
            context = new ScenarioContext(context.Targets, context.Services);
            CurrentStepIndex = -1;
            CurrentStepElapsed = 0f;
            SetState(RunnerState.NotStarted);
            Start();
        }

        /// <summary>Aborts the scenario if it is running and releases the runtime copy.</summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            if (IsActive)
                Abort();

            DetachContext();
            DestroyRuntimeCopy();
            isDisposed = true;
        }

        void BeginStep(int index)
        {
            CurrentStepIndex = index;
            attempt = 1;
            stepMistakes = 0;
            EnterCurrentStep();
        }

        void EnterCurrentStep()
        {
            ScenarioStep step = steps[CurrentStepIndex];
            currentStep = step;
            CurrentStepElapsed = 0f;

            if (step == null)
            {
                StepStarted?.Invoke(new StepInfo(null, CurrentStepIndex, null, string.Empty, attempt));
                if (IsActive)
                    HandleFailure(StepOutcome.Failed, $"Step {CurrentStepIndex + 1} has a missing type (its class was deleted or renamed). Fix it in the Scenario Forge window.");
                return;
            }

            step.ResetAttemptState();
            currentInstruction = step.Instruction ?? string.Empty;

            string exceptionMessage = null;
            isEnteringStep = true;
            try
            {
                step.OnEnter(context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, definition);
                exceptionMessage = exception.Message;
            }
            finally
            {
                isEnteringStep = false;
            }

            StepStarted?.Invoke(new StepInfo(step.Id, CurrentStepIndex, step.Title, currentInstruction, attempt));
            if (!IsActive || step != currentStep)
                return;

            InstructionChanged?.Invoke(currentInstruction);
            if (!IsActive || step != currentStep)
                return;

            if (exceptionMessage != null)
                HandleFailure(StepOutcome.Failed, exceptionMessage);
        }

        void HandleFailure(StepOutcome outcome, string reason)
        {
            ScenarioStep step = currentStep;
            FailurePolicy policy = step != null ? step.OnFailure : FailurePolicy.FailScenario;

            if (policy == FailurePolicy.Retry && attempt <= step.MaxRetries)
            {
                SafeExit(step, StepOutcome.Failed);
                if (!IsActive)
                    return;

                attempt++;
                EnterCurrentStep();
                return;
            }

            EndStep(outcome, reason);
            if (!IsActive)
                return;

            if (policy == FailurePolicy.SkipStep)
                Advance();
            else
                Finish(RunnerState.Failed);
        }

        void EndStep(StepOutcome outcome, string reason)
        {
            ScenarioStep step = currentStep;
            int index = CurrentStepIndex;
            currentStep = null;

            if (step != null)
                SafeExit(step, outcome);

            var result = new StepResult(step?.Id, index, outcome, CurrentStepElapsed, attempt, reason, stepMistakes);
            results.Add(result);
            StepEnded?.Invoke(result);
        }

        void Advance()
        {
            int next = CurrentStepIndex + 1;
            if (next >= steps.Count)
            {
                Finish(RunnerState.Completed);
                return;
            }

            BeginStep(next);
        }

        void Finish(RunnerState finalState)
        {
            currentStep = null;
            CurrentStepIndex = -1;
            DetachContext();
            DestroyRuntimeCopy();
            SetState(finalState);
        }

        void SafeExit(ScenarioStep step, StepOutcome outcome)
        {
            try
            {
                step.OnExit(context, outcome);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, definition);
            }
        }

        void OnInstructionUpdateRequested(string text)
        {
            if (isEnteringStep)
            {
                currentInstruction = text;
                return;
            }

            if (!IsActive || currentStep == null)
                return;

            currentInstruction = text;
            InstructionChanged?.Invoke(text);
        }

        void SetState(RunnerState newState)
        {
            if (State == newState)
                return;

            State = newState;
            StateChanged?.Invoke(newState);
        }

        void DetachContext()
        {
            context.InstructionUpdateRequested -= OnInstructionUpdateRequested;
            context.MistakeReportRequested -= onMistakeReportRequested;
            context.Signals.Raised -= onSignalRaised;
        }

        void OnSignalRaised(string signal) => SignalRaised?.Invoke(signal);

        void OnMistakeReportRequested(string message)
        {
            if (!IsActive)
                return;

            bool hasStep = CurrentStepIndex >= 0;
            string stepId = hasStep ? steps[CurrentStepIndex]?.Id : null;
            var mistake = new ScenarioMistake(stepId, hasStep ? CurrentStepIndex : -1, message, context.TotalElapsed);
            if (hasStep)
                stepMistakes++;
            mistakes.Add(mistake);
            MistakeReported?.Invoke(mistake);
        }

        void DestroyRuntimeCopy()
        {
            if (runtimeCopy == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(runtimeCopy);
            else
                Object.DestroyImmediate(runtimeCopy);

            runtimeCopy = null;
        }

        void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ScenarioRunner), "This ScenarioRunner was disposed. Create a new one to run the scenario again.");
        }

        static string DescribeStep(ScenarioStep step, int index)
        {
            return string.IsNullOrEmpty(step.Title) ? $"#{index + 1} ({step.GetType().Name})" : step.Title;
        }
    }
}
