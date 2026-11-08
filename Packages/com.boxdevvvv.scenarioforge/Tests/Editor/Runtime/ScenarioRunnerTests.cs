using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioRunnerTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        static int Count(ScenarioContext context, string key) => context.Blackboard.TryGet(key, out int value) ? value : 0;

        static string Outcome(ScenarioContext context, string id) => context.Blackboard.TryGet(ProbeStep.OutcomeKey(id), out string value) ? value : null;

        ScenarioRunner CreateRunner(params ScenarioStep[] steps) => new(scenarios.Create(steps), new ScenarioContext());

        // Rule 1 ----------------------------------------------------------------------------------------

        [Test]
        public void NewRunner_IsNotStarted_WithNoActiveStep()
        {
            using var runner = CreateRunner(TestScenarios.Probe());

            Assert.That(runner.State, Is.EqualTo(RunnerState.NotStarted));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(-1));
            Assert.That(runner.CurrentStep, Is.Null);
        }

        [Test]
        public void Start_FromNotStarted_EntersFirstStepAndRaisesEvents()
        {
            ProbeStep first = TestScenarios.Probe("First");
            first.Instruction = "Do the first thing";
            using var runner = CreateRunner(first, TestScenarios.Probe());
            var states = new List<RunnerState>();
            StepInfo? started = null;
            string instruction = null;
            runner.StateChanged += states.Add;
            runner.StepStarted += info => started = info;
            runner.InstructionChanged += text => instruction = text;

            runner.Start();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(states, Is.EqualTo(new[] { RunnerState.Running }));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(first.Id)), Is.EqualTo(1));
            Assert.That(started.HasValue, Is.True);
            Assert.That(started.Value.StepId, Is.EqualTo(first.Id));
            Assert.That(started.Value.Index, Is.EqualTo(0));
            Assert.That(started.Value.Attempt, Is.EqualTo(1));
            Assert.That(instruction, Is.EqualTo("Do the first thing"));
        }

        [Test]
        public void Start_WithNoSteps_CompletesImmediately()
        {
            using var runner = CreateRunner();

            runner.Start();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            Assert.That(runner.Results, Is.Empty);
        }

        [Test]
        public void Start_WhenAlreadyRunning_IsIgnored()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step);
            runner.Start();

            runner.Start();

            Assert.That(Count(runner.Context, ProbeStep.EnterKey(step.Id)), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_WithNullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ScenarioRunner(null, new ScenarioContext()));
            Assert.Throws<ArgumentNullException>(() => new ScenarioRunner(scenarios.Create(), null));
        }

        // Rule 2 and 4 -----------------------------------------------------------------------------------

        [Test]
        public void Tick_AddsDeltaTimeAndCallsOnTick()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step);
            runner.Start();

            runner.Tick(0.25f);
            runner.Tick(0.5f);

            Assert.That(runner.CurrentStepElapsed, Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(runner.Context.TotalElapsed, Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(Count(runner.Context, ProbeStep.TickKey(step.Id)), Is.EqualTo(2));
        }

        [Test]
        public void Tick_BeforeStart_DoesNothing()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step);

            runner.Tick(1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.NotStarted));
            Assert.That(runner.CurrentStepElapsed, Is.Zero);
        }

        [Test]
        public void Tick_WhenStepSucceeds_EntersNextStepInSameTick_ButTicksItOnTheNextOne()
        {
            ProbeStep first = TestScenarios.Probe(ticksToFinish: 1);
            ProbeStep second = TestScenarios.Probe();
            using var runner = CreateRunner(first, second);
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(Count(runner.Context, ProbeStep.ExitKey(first.Id)), Is.EqualTo(1));
            Assert.That(Outcome(runner.Context, first.Id), Is.EqualTo(nameof(StepOutcome.Succeeded)));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(second.Id)), Is.EqualTo(1));
            Assert.That(Count(runner.Context, ProbeStep.TickKey(second.Id)), Is.Zero);
            Assert.That(runner.CurrentStepElapsed, Is.Zero);

            runner.Tick(0.1f);

            Assert.That(Count(runner.Context, ProbeStep.TickKey(second.Id)), Is.EqualTo(1));
        }

        [Test]
        public void Tick_WhenLastStepSucceeds_Completes()
        {
            ProbeStep only = TestScenarios.Probe(ticksToFinish: 2);
            using var runner = CreateRunner(only);
            runner.Start();

            runner.Tick(0.1f);
            runner.Tick(0.2f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            Assert.That(runner.CurrentStep, Is.Null);
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(-1));
            Assert.That(runner.Results.Count, Is.EqualTo(1));
            StepResult result = runner.Results[0];
            Assert.That(result.StepId, Is.EqualTo(only.Id));
            Assert.That(result.Outcome, Is.EqualTo(StepOutcome.Succeeded));
            Assert.That(result.Duration, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(result.Attempts, Is.EqualTo(1));
            Assert.That(result.FailureReason, Is.Null);
        }

        // Rule 3 -----------------------------------------------------------------------------------------

        [Test]
        public void Failure_WithFailScenario_FailsTheScenario()
        {
            ProbeStep failing = TestScenarios.Probe(ticksToFinish: 1);
            failing.finishWith = StepStatus.Failed;
            ProbeStep never = TestScenarios.Probe();
            using var runner = CreateRunner(failing, never);
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(Outcome(runner.Context, failing.Id), Is.EqualTo(nameof(StepOutcome.Failed)));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(never.Id)), Is.Zero);
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Failed));
            Assert.That(runner.Results[0].FailureReason, Is.EqualTo("Probe failure"));
        }

        [Test]
        public void Failure_WithSkipStep_RecordsFailureAndContinues()
        {
            ProbeStep failing = TestScenarios.Probe(ticksToFinish: 1);
            failing.finishWith = StepStatus.Failed;
            failing.OnFailure = FailurePolicy.SkipStep;
            ProbeStep next = TestScenarios.Probe(ticksToFinish: 1);
            using var runner = CreateRunner(failing, next);
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Failed));

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
        }

        [Test]
        public void Failure_WithRetry_ExitsAndReentersTheStep()
        {
            ProbeStep flaky = TestScenarios.Probe(ticksToFinish: 1);
            flaky.failingAttempts = 1;
            flaky.OnFailure = FailurePolicy.Retry;
            flaky.MaxRetries = 2;
            using var runner = CreateRunner(flaky);
            var attempts = new List<int>();
            runner.StepStarted += info => attempts.Add(info.Attempt);
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(Count(runner.Context, ProbeStep.ExitKey(flaky.Id)), Is.EqualTo(1));
            Assert.That(Outcome(runner.Context, flaky.Id), Is.EqualTo(nameof(StepOutcome.Failed)));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(flaky.Id)), Is.EqualTo(2));
            Assert.That(runner.CurrentStepElapsed, Is.Zero, "A retry starts a new attempt.");
            Assert.That(runner.Results, Is.Empty, "Retries are not final results.");

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            Assert.That(runner.Results[0].Attempts, Is.EqualTo(2));
            Assert.That(attempts, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Failure_WithRetry_WhenRetriesRunOut_FailsTheScenario()
        {
            ProbeStep broken = TestScenarios.Probe(ticksToFinish: 1);
            broken.finishWith = StepStatus.Failed;
            broken.OnFailure = FailurePolicy.Retry;
            broken.MaxRetries = 2;
            using var runner = CreateRunner(broken, TestScenarios.Probe());
            runner.Start();

            runner.Tick(0.1f);
            runner.Tick(0.1f);
            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(broken.Id)), Is.EqualTo(3));
            Assert.That(runner.Results[0].Attempts, Is.EqualTo(3));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Failed));
        }

        [Test]
        public void Timeout_WhenExceeded_TimesOutAndAppliesFailScenario()
        {
            ProbeStep slow = TestScenarios.Probe();
            slow.TimeoutSeconds = 1f;
            using var runner = CreateRunner(slow);
            runner.Start();

            runner.Tick(0.6f);
            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));

            runner.Tick(0.6f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.TimedOut));
            Assert.That(runner.Results[0].FailureReason, Does.Contain("timed out"));
            Assert.That(Outcome(runner.Context, slow.Id), Is.EqualTo(nameof(StepOutcome.TimedOut)));
        }

        [Test]
        public void Timeout_WithSkipStep_RecordsTimedOutAndContinues()
        {
            ProbeStep slow = TestScenarios.Probe();
            slow.TimeoutSeconds = 0.5f;
            slow.OnFailure = FailurePolicy.SkipStep;
            using var runner = CreateRunner(slow, TestScenarios.Probe());
            runner.Start();

            runner.Tick(1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.TimedOut));
        }

        [Test]
        public void Timeout_WithRetry_RetriesTheStep()
        {
            ProbeStep slow = TestScenarios.Probe();
            slow.TimeoutSeconds = 0.5f;
            slow.OnFailure = FailurePolicy.Retry;
            slow.MaxRetries = 1;
            using var runner = CreateRunner(slow);
            runner.Start();

            runner.Tick(1f);
            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(slow.Id)), Is.EqualTo(2));

            runner.Tick(1f);
            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.TimedOut));
        }

        [Test]
        public void Timeout_OfZero_MeansNoLimit()
        {
            using var runner = CreateRunner(TestScenarios.Probe());
            runner.Start();

            runner.Tick(10000f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
        }

        [Test]
        public void Success_OnTheSameTickAsTimeout_WinsOverTheTimeout()
        {
            ProbeStep step = TestScenarios.Probe(ticksToFinish: 1);
            step.TimeoutSeconds = 0.5f;
            using var runner = CreateRunner(step);
            runner.Start();

            runner.Tick(1f);

            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Succeeded));
        }

        // Rule 5 -----------------------------------------------------------------------------------------

        [Test]
        public void Pause_FromRunning_StopsTimeFromAdvancing()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step);
            runner.Start();
            runner.Tick(0.5f);

            runner.Pause();
            runner.Tick(10f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Paused));
            Assert.That(runner.CurrentStepElapsed, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(runner.Context.TotalElapsed, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(Count(runner.Context, ProbeStep.TickKey(step.Id)), Is.EqualTo(1));
        }

        [Test]
        public void Resume_FromPaused_ContinuesRunning()
        {
            using var runner = CreateRunner(TestScenarios.Probe());
            runner.Start();
            runner.Pause();

            runner.Resume();
            runner.Tick(0.5f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.CurrentStepElapsed, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void PauseAndResume_OutsideTheirStates_DoNothing()
        {
            using var runner = CreateRunner(TestScenarios.Probe());

            runner.Pause();
            Assert.That(runner.State, Is.EqualTo(RunnerState.NotStarted));

            runner.Start();
            runner.Resume();
            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
        }

        // Rule 6 -----------------------------------------------------------------------------------------

        [Test]
        public void Abort_WhileRunning_ExitsActiveStepWithAborted()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step, TestScenarios.Probe());
            runner.Start();

            runner.Abort();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Aborted));
            Assert.That(Outcome(runner.Context, step.Id), Is.EqualTo(nameof(StepOutcome.Aborted)));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Aborted));
            Assert.That(runner.CurrentStep, Is.Null);
        }

        [Test]
        public void Abort_WhilePaused_Aborts()
        {
            using var runner = CreateRunner(TestScenarios.Probe());
            runner.Start();
            runner.Pause();

            runner.Abort();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Aborted));
        }

        [Test]
        public void Abort_AfterCompletion_DoesNothing()
        {
            using var runner = CreateRunner(TestScenarios.Probe(ticksToFinish: 1));
            runner.Start();
            runner.Tick(0.1f);
            var states = new List<RunnerState>();
            runner.StateChanged += states.Add;

            runner.Abort();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            Assert.That(states, Is.Empty);
        }

        // Rule 7 -----------------------------------------------------------------------------------------

        [Test]
        public void ExceptionInOnTick_IsLoggedAndTreatedAsFailure()
        {
            ProbeStep throwing = TestScenarios.Probe();
            throwing.throwOnTick = true;
            using var runner = CreateRunner(throwing);
            runner.Start();
            LogAssert.Expect(LogType.Exception, new Regex("Probe tick failure"));

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Failed));
            Assert.That(runner.Results[0].FailureReason, Is.EqualTo("Probe tick failure"));
        }

        [Test]
        public void ExceptionInOnEnter_IsLoggedAndTreatedAsFailure()
        {
            ProbeStep throwing = TestScenarios.Probe();
            throwing.throwOnEnter = true;
            throwing.OnFailure = FailurePolicy.SkipStep;
            ProbeStep next = TestScenarios.Probe();
            using var runner = CreateRunner(throwing, next);
            LogAssert.Expect(LogType.Exception, new Regex("Probe enter failure"));

            runner.Start();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(runner.Results[0].FailureReason, Is.EqualTo("Probe enter failure"));
        }

        [Test]
        public void MissingStepType_FailsTheScenarioWithAClearReason()
        {
            using var runner = CreateRunner(TestScenarios.Probe(ticksToFinish: 1), null);
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(runner.Results[1].FailureReason, Does.Contain("missing type"));
        }

        // Rule 8 -----------------------------------------------------------------------------------------

        [Test]
        public void StepTransition_RaisesEventsInGuaranteedOrder()
        {
            ProbeStep first = TestScenarios.Probe(ticksToFinish: 1);
            ProbeStep second = TestScenarios.Probe();
            second.Instruction = "Second";
            using var runner = CreateRunner(first, second);
            runner.Start();
            var events = new List<string>();
            runner.StepEnded += result => events.Add("ended:" + result.Index);
            runner.StepStarted += info => events.Add("started:" + info.Index);
            runner.InstructionChanged += text => events.Add("instruction:" + text);

            runner.Tick(0.1f);

            Assert.That(events, Is.EqualTo(new[] { "ended:0", "started:1", "instruction:Second" }));
        }

        [Test]
        public void UpdateInstruction_FromActiveStep_RaisesInstructionChanged()
        {
            using var runner = CreateRunner(new InstructionUpdatingStep());
            runner.Start();
            string instruction = null;
            runner.InstructionChanged += text => instruction = text;

            runner.Tick(0.1f);

            Assert.That(instruction, Is.EqualTo("updated"));
        }

        [Test]
        public void UpdateInstruction_WithoutActiveStep_IsIgnored()
        {
            using var runner = CreateRunner(TestScenarios.Probe());
            int calls = 0;
            runner.InstructionChanged += _ => calls++;

            runner.Context.UpdateInstruction("ignored");

            Assert.That(calls, Is.Zero);
        }

        // Debugging controls -----------------------------------------------------------------------------

        [Test]
        public void SkipCurrentStep_EndsStepAsSkippedAndAdvances()
        {
            ProbeStep first = TestScenarios.Probe();
            using var runner = CreateRunner(first, TestScenarios.Probe());
            runner.Start();

            runner.SkipCurrentStep();

            Assert.That(runner.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(runner.Results[0].Outcome, Is.EqualTo(StepOutcome.Skipped));
            Assert.That(Outcome(runner.Context, first.Id), Is.EqualTo(nameof(StepOutcome.Skipped)));
        }

        [Test]
        public void SkipCurrentStep_OnLastStep_Completes()
        {
            using var runner = CreateRunner(TestScenarios.Probe());
            runner.Start();

            runner.SkipCurrentStep();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
        }

        [Test]
        public void Restart_StartsAgainWithANewContext()
        {
            ProbeStep first = TestScenarios.Probe(ticksToFinish: 1);
            using var runner = CreateRunner(first, TestScenarios.Probe());
            runner.Start();
            runner.Tick(0.1f);
            ScenarioContext oldContext = runner.Context;

            runner.Restart();

            Assert.That(oldContext.Blackboard.Has(ProbeStep.EnterKey(first.Id)), Is.True);
            Assert.That(runner.Context, Is.Not.SameAs(oldContext));
            Assert.That(runner.Context.Targets, Is.SameAs(oldContext.Targets));
            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(runner.Results, Is.Empty);
            Assert.That(Count(runner.Context, ProbeStep.EnterKey(first.Id)), Is.EqualTo(1));
            Assert.That(Outcome(oldContext, first.Id), Is.EqualTo(nameof(StepOutcome.Succeeded)));
        }

        [Test]
        public void Restart_AfterFailure_RunsAgain()
        {
            ProbeStep failing = TestScenarios.Probe(ticksToFinish: 1);
            failing.finishWith = StepStatus.Failed;
            using var runner = CreateRunner(failing);
            runner.Start();
            runner.Tick(0.1f);

            runner.Restart();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Running));
        }

        [Test]
        public void Dispose_WhileRunning_AbortsAndPreventsRestart()
        {
            ProbeStep step = TestScenarios.Probe();
            var runner = CreateRunner(step);
            runner.Start();

            runner.Dispose();

            Assert.That(runner.State, Is.EqualTo(RunnerState.Aborted));
            Assert.Throws<ObjectDisposedException>(runner.Restart);
        }

        // Shared asset (spec 6.4) ------------------------------------------------------------------------

        [Test]
        public void RunningAScenario_DoesNotModifyTheOriginalAsset()
        {
            var mutating = new MutatingStep { Title = "Mutates its fields" };
            ScenarioDefinition definition = scenarios.Create(mutating, TestScenarios.Probe(ticksToFinish: 1));
            string before = EditorJsonUtility.ToJson(definition);

            using (var runner = new ScenarioRunner(definition, new ScenarioContext()))
            {
                runner.Start();
                for (int i = 0; i < 10 && runner.State == RunnerState.Running; i++)
                    runner.Tick(0.1f);

                Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            }

            Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
            Assert.That(mutating.counter, Is.Zero);
            Assert.That(mutating.note, Is.EqualTo("original"));
        }

        [Test]
        public void CurrentStep_IsARuntimeCopy()
        {
            ProbeStep step = TestScenarios.Probe();
            using var runner = CreateRunner(step);

            runner.Start();

            Assert.That(runner.CurrentStep, Is.Not.SameAs(step));
            Assert.That(runner.CurrentStep.Id, Is.EqualTo(step.Id));
        }

        // Rule 9 -----------------------------------------------------------------------------------------

        // GC.GetAllocatedBytesForCurrentThread() always returns 0 on Unity's Mono runtime, so it cannot prove
        // anything here. The Test Framework's AllocatingGCMemory constraint reads the profiler's GC.Alloc
        // counter instead; the control test below shows that it does detect allocations.

        [Test]
        public void Tick_InSteadyState_DoesNotAllocate()
        {
            var idle = new IdleStep();
            idle.TimeoutSeconds = 1000000f;
            using var runner = CreateRunner(idle);
            runner.StepStarted += _ => { };
            runner.InstructionChanged += _ => { };
            runner.Start();
            for (int i = 0; i < 100; i++)
                runner.Tick(0.016f);

            Assert.That(() =>
            {
                for (int i = 0; i < 1000; i++)
                    runner.Tick(0.016f);
            }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }

        [Test]
        public void AllocationConstraint_DetectsAllocations()
        {
            object sink = null;

            Assert.That(() => { sink = new byte[64]; }, UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory());
            Assert.That(sink, Is.Not.Null);
        }
    }
}
