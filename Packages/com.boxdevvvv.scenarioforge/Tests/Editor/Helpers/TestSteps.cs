using System;
using UnityEngine;

namespace ScenarioForge.Tests
{
    /// <summary>
    /// Configurable step for runner tests. It records lifecycle calls on the context's blackboard, because the
    /// runner works on a copy of the step and the test cannot observe the original instance.
    /// </summary>
    [Serializable]
    sealed class ProbeStep : ScenarioStep
    {
        /// <summary>Ticks before the step finishes. Negative means it never finishes on its own.</summary>
        [SerializeField] internal int ticksToFinish = -1;
        [SerializeField] internal StepStatus finishWith = StepStatus.Succeeded;
        [SerializeField] internal int failingAttempts;
        [SerializeField] internal bool throwOnTick;
        [SerializeField] internal bool throwOnEnter;
        [SerializeField] internal bool canFail = true;

        [NonSerialized] internal int Ticks;
        [NonSerialized] internal int Enters;

        public override bool CanFail => canFail;

        public static string EnterKey(string id) => id + ".enter";
        public static string ExitKey(string id) => id + ".exit";
        public static string OutcomeKey(string id) => id + ".outcome";
        public static string TickKey(string id) => id + ".ticks";

        protected internal override void OnEnter(ScenarioContext context)
        {
            Ticks = 0;
            Enters++;
            Increment(context, EnterKey(Id));
            if (throwOnEnter)
                throw new InvalidOperationException("Probe enter failure");
        }

        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            Ticks++;
            Increment(context, TickKey(Id));
            if (throwOnTick)
                throw new InvalidOperationException("Probe tick failure");

            if (Enters <= failingAttempts)
                return Fail($"Probe failing attempt {Enters}");

            if (ticksToFinish >= 0 && Ticks >= ticksToFinish)
                return finishWith == StepStatus.Failed ? Fail("Probe failure") : finishWith;

            return StepStatus.Running;
        }

        protected internal override void OnExit(ScenarioContext context, StepOutcome outcome)
        {
            Increment(context, ExitKey(Id));
            context.Blackboard.Set(OutcomeKey(Id), outcome.ToString());
        }

        static void Increment(ScenarioContext context, string key)
        {
            context.Blackboard.TryGet(key, out int count);
            context.Blackboard.Set(key, count + 1);
        }
    }

    /// <summary>A step that (incorrectly) writes to one of its serialized fields while it runs.</summary>
    [Serializable]
    sealed class MutatingStep : ScenarioStep
    {
        [SerializeField] internal int counter;
        [SerializeField] internal string note = "original";

        protected internal override void OnEnter(ScenarioContext context) => note = "mutated";

        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            counter++;
            return counter >= 3 ? StepStatus.Succeeded : StepStatus.Running;
        }
    }

    /// <summary>A step whose tick does nothing and allocates nothing.</summary>
    [Serializable]
    sealed class IdleStep : ScenarioStep
    {
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Running;
    }

    /// <summary>Asks the runner to change the instruction on every tick.</summary>
    [Serializable]
    sealed class InstructionUpdatingStep : ScenarioStep
    {
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            context.UpdateInstruction("updated");
            return StepStatus.Running;
        }
    }
}
