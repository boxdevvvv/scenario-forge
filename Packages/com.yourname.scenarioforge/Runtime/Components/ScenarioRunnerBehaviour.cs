using System;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioForge
{
    /// <summary>
    /// Runs a <see cref="ScenarioDefinition"/> in a scene: builds the context and the runner, ticks it every
    /// frame and exposes UnityEvents for designers.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scenario Forge/Scenario Runner")]
    public sealed class ScenarioRunnerBehaviour : MonoBehaviour
    {
        [SerializeField] ScenarioDefinition definition;
        [SerializeField] bool autoStart = true;
        [SerializeField] UnityEvent onScenarioStarted = new();
        [SerializeField] UnityEvent onScenarioCompleted = new();
        [SerializeField] UnityEvent onScenarioFailed = new();
        [SerializeField] UnityEvent<string> onInstructionChanged = new();

        RunnerState previousState;

        /// <summary>The scenario to run. Change it before calling <see cref="StartScenario"/>.</summary>
        public ScenarioDefinition Definition
        {
            get => definition;
            set => definition = value;
        }

        /// <summary>Whether the scenario starts automatically in <c>Start</c>.</summary>
        public bool AutoStart
        {
            get => autoStart;
            set => autoStart = value;
        }

        /// <summary>Optional game services passed to the context. Set it before <see cref="StartScenario"/>.</summary>
        public IScenarioServices Services { get; set; }

        /// <summary>The runner of the current run, or <c>null</c> before the scenario starts.</summary>
        public ScenarioRunner Runner { get; private set; }

        /// <summary>The context of the current run, or <c>null</c> before the scenario starts.</summary>
        public ScenarioContext Context => Runner?.Context;

        /// <summary>Invoked when the scenario starts running.</summary>
        public UnityEvent OnScenarioStarted => onScenarioStarted;

        /// <summary>Invoked when every step has completed.</summary>
        public UnityEvent OnScenarioCompleted => onScenarioCompleted;

        /// <summary>Invoked when the scenario fails.</summary>
        public UnityEvent OnScenarioFailed => onScenarioFailed;

        /// <summary>Invoked with the instruction text whenever it changes.</summary>
        public UnityEvent<string> OnInstructionChanged => onInstructionChanged;

        /// <summary>Forwards <see cref="ScenarioRunner.StepStarted"/> for every run of this behaviour.</summary>
        public event Action<StepInfo> StepStarted;

        /// <summary>Forwards <see cref="ScenarioRunner.StepEnded"/> for every run of this behaviour.</summary>
        public event Action<StepResult> StepEnded;

        /// <summary>Forwards <see cref="ScenarioRunner.SignalRaised"/> for every run of this behaviour.</summary>
        public event Action<string> SignalRaised;

        /// <summary>Forwards <see cref="ScenarioRunner.MistakeReported"/> for every run of this behaviour.</summary>
        public event Action<ScenarioMistake> MistakeReported;

        /// <summary>
        /// Creates a new context and runner and starts the scenario. Disposes any previous run first.
        /// </summary>
        public void StartScenario()
        {
            if (definition == null)
            {
                Debug.LogError($"ScenarioRunnerBehaviour on '{name}' has no Scenario Definition. Assign one in the Inspector.", this);
                return;
            }

            DisposeRunner();

            var context = new ScenarioContext(SceneTargetResolver.FromLoadedScenes(), Services);
            previousState = RunnerState.NotStarted;
            Runner = new ScenarioRunner(definition, context);
            Runner.StateChanged += HandleStateChanged;
            Runner.InstructionChanged += HandleInstructionChanged;
            Runner.StepStarted += HandleStepStarted;
            Runner.StepEnded += HandleStepEnded;
            Runner.SignalRaised += HandleSignalRaised;
            Runner.MistakeReported += HandleMistakeReported;
            Runner.Start();
        }

        void Start()
        {
            if (autoStart)
                StartScenario();
        }

        void Update()
        {
            Runner?.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            DisposeRunner();
        }

        void DisposeRunner()
        {
            if (Runner == null)
                return;

            Runner.StateChanged -= HandleStateChanged;
            Runner.InstructionChanged -= HandleInstructionChanged;
            Runner.StepStarted -= HandleStepStarted;
            Runner.StepEnded -= HandleStepEnded;
            Runner.SignalRaised -= HandleSignalRaised;
            Runner.MistakeReported -= HandleMistakeReported;
            Runner.Dispose();
            Runner = null;
        }

        void HandleStepStarted(StepInfo info) => StepStarted?.Invoke(info);

        void HandleStepEnded(StepResult result) => StepEnded?.Invoke(result);

        void HandleSignalRaised(string signal) => SignalRaised?.Invoke(signal);

        void HandleMistakeReported(ScenarioMistake mistake) => MistakeReported?.Invoke(mistake);

        void HandleStateChanged(RunnerState state)
        {
            RunnerState from = previousState;
            previousState = state;
            switch (state)
            {
                case RunnerState.Running when from == RunnerState.NotStarted:
                    onScenarioStarted.Invoke();
                    break;
                case RunnerState.Completed:
                    onScenarioCompleted.Invoke();
                    break;
                case RunnerState.Failed:
                    onScenarioFailed.Invoke();
                    break;
            }
        }

        void HandleInstructionChanged(string instruction) => onInstructionChanged.Invoke(instruction);
    }
}
