using System;

namespace ScenarioForge
{
    /// <summary>
    /// Everything a step needs while it runs: the blackboard, the signal bus, scene targets and
    /// optional game services. A new context is created for every run.
    /// </summary>
    public sealed class ScenarioContext
    {
        static readonly ITargetResolver EmptyTargets = new EmptyTargetResolver();

        /// <summary>Creates a context for one run.</summary>
        /// <param name="targets">Resolves target ids to scene transforms. <c>null</c> resolves nothing.</param>
        /// <param name="services">Optional game services for custom steps.</param>
        public ScenarioContext(ITargetResolver targets = null, IScenarioServices services = null)
        {
            Targets = targets ?? EmptyTargets;
            Services = services;
            Blackboard = new Blackboard();
            Signals = new SignalBus();
        }

        /// <summary>Typed key/value store shared by the steps of this run.</summary>
        public Blackboard Blackboard { get; }

        /// <summary>Named events raised by the game and observed by steps.</summary>
        public SignalBus Signals { get; }

        /// <summary>Resolves <see cref="ScenarioTarget"/> ids to transforms.</summary>
        public ITargetResolver Targets { get; }

        /// <summary>Optional game services. <c>null</c> when the game does not provide any.</summary>
        public IScenarioServices Services { get; }

        /// <summary>Seconds the scenario has been running, excluding pauses.</summary>
        public float TotalElapsed { get; internal set; }

        /// <summary>Raised when the active step asks to change the instruction shown to the player.</summary>
        internal event Action<string> InstructionUpdateRequested;

        /// <summary>
        /// Replaces the instruction text shown to the player, for example to show progress
        /// (<c>"Check the mirrors (2/4)"</c>). The runner forwards it as <see cref="ScenarioRunner.InstructionChanged"/>.
        /// </summary>
        /// <param name="text">The new instruction text.</param>
        public void UpdateInstruction(string text) => InstructionUpdateRequested?.Invoke(text ?? string.Empty);

        sealed class EmptyTargetResolver : ITargetResolver
        {
            public bool TryGetTransform(string targetId, out UnityEngine.Transform transform)
            {
                transform = null;
                return false;
            }
        }
    }
}
