using System;
using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// Base class of every step in a scenario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derive from this class, mark the type <c>[Serializable]</c>, give it a public parameterless
    /// constructor and (optionally) a <see cref="ScenarioStepMenuAttribute"/>. It then shows up in the
    /// Scenario Forge window automatically.
    /// </para>
    /// <para>
    /// Steps are authored inside a <see cref="ScenarioDefinition"/> asset, but the runner always executes a
    /// copy. Keep execution state in <c>[NonSerialized]</c> fields and reset it in <see cref="OnEnter"/>.
    /// </para>
    /// <para>
    /// Two step types must not declare fields with the same name and different types: UI Toolkit's
    /// <c>PropertyField</c> has known issues with managed references in that case.
    /// </para>
    /// </remarks>
    [Serializable]
    public abstract class ScenarioStep
    {
        [SerializeField, HideInInspector] string id;
        [SerializeField] string title;
        [SerializeField, TextArea] string instruction;
        [SerializeField, Min(0)] float timeoutSeconds;
        [SerializeField] FailurePolicy onFailure = FailurePolicy.FailScenario;
        [SerializeField, Min(0)] int maxRetries;

        [NonSerialized] string failureReason;

        /// <summary>Unique id of the step within its scenario (a GUID in "N" format).</summary>
        public string Id
        {
            get => id;
            internal set => id = value;
        }

        /// <summary>Display name of the step in the window and the event log.</summary>
        public string Title
        {
            get => title;
            internal set => title = value;
        }

        /// <summary>Text the game UI shows to the player while the step is active.</summary>
        public string Instruction
        {
            get => instruction;
            internal set => instruction = value;
        }

        /// <summary>Seconds before the step times out. <c>0</c> means no limit.</summary>
        public float TimeoutSeconds
        {
            get => timeoutSeconds;
            internal set => timeoutSeconds = value;
        }

        /// <summary>What the runner does when the step fails or times out.</summary>
        public FailurePolicy OnFailure
        {
            get => onFailure;
            internal set => onFailure = value;
        }

        /// <summary>Extra attempts allowed when <see cref="OnFailure"/> is <see cref="FailurePolicy.Retry"/>.</summary>
        public int MaxRetries
        {
            get => maxRetries;
            internal set => maxRetries = value;
        }

        /// <summary>
        /// Whether this step can fail on its own (without a timeout). Validation reports a timeout on a
        /// step that returns <c>false</c> here as informational. Defaults to <c>true</c>.
        /// </summary>
        public virtual bool CanFail => true;

        /// <summary>The reason given to the last call to <see cref="Fail"/> during the current attempt.</summary>
        internal string FailureReason => failureReason;

        /// <summary>Called when the step becomes active, and again on every retry.</summary>
        /// <param name="context">The context of the current run.</param>
        protected internal virtual void OnEnter(ScenarioContext context)
        {
        }

        /// <summary>Called once per runner tick while the step is active.</summary>
        /// <param name="context">The context of the current run.</param>
        /// <param name="deltaTime">Seconds since the previous tick.</param>
        /// <returns>Whether the step is still running, succeeded or failed.</returns>
        protected internal abstract StepStatus OnTick(ScenarioContext context, float deltaTime);

        /// <summary>Called when the step stops being active, for any reason.</summary>
        /// <param name="context">The context of the current run.</param>
        /// <param name="outcome">Why the step ended.</param>
        protected internal virtual void OnExit(ScenarioContext context, StepOutcome outcome)
        {
        }

        /// <summary>
        /// Adds this step's own problems to <paramref name="report"/>. Never throws.
        /// </summary>
        /// <param name="report">The report to add issues to. Pass <see cref="Id"/> as the step id.</param>
        public virtual void Validate(ValidationReport report)
        {
        }

        /// <summary>
        /// Records why the step failed and returns <see cref="StepStatus.Failed"/>.
        /// Use it as <c>return Fail("Lever target 'lever_01' was not found.");</c>.
        /// </summary>
        /// <param name="reason">A message that says what happened and how to fix it.</param>
        /// <returns>Always <see cref="StepStatus.Failed"/>.</returns>
        protected StepStatus Fail(string reason)
        {
            failureReason = reason;
            return StepStatus.Failed;
        }

        /// <summary>Gives the step a new unique id.</summary>
        internal void AssignNewId() => id = Guid.NewGuid().ToString("N");

        /// <summary>Clears per-attempt state owned by the base class. Called by the runner before <see cref="OnEnter"/>.</summary>
        internal void ResetAttemptState() => failureReason = null;
    }
}
