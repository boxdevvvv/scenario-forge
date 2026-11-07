using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Steps;
using UnityEngine;

namespace ScenarioForge.Tests
{
    static class StepTestUtility
    {
        public static ValidationReport Validate(ScenarioStep step)
        {
            if (string.IsNullOrEmpty(step.Id))
                step.AssignNewId();

            var report = new ValidationReport();
            step.Validate(report);
            return report;
        }

        public static bool Has(this ValidationReport report, ValidationSeverity severity, string fragment)
        {
            return report.Issues.Any(issue => issue.Severity == severity && issue.Message.Contains(fragment));
        }
    }

    public sealed class WaitStepTests
    {
        [Test]
        public void CompletesAfterDuration_NotBefore()
        {
            var step = new WaitStep { Duration = 1f };
            var context = new ScenarioContext();
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.6f), Is.EqualTo(StepStatus.Running));
            Assert.That(step.OnTick(context, 0.6f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void OnEnter_ResetsElapsedTime()
        {
            var step = new WaitStep { Duration = 1f };
            var context = new ScenarioContext();
            step.OnEnter(context);
            step.OnTick(context, 0.9f);

            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.5f), Is.EqualTo(StepStatus.Running));
        }

        [Test]
        public void CannotFailOnItsOwn() => Assert.That(new WaitStep().CanFail, Is.False);

        [Test]
        public void Validate_ZeroDuration_IsAWarning()
        {
            Assert.That(StepTestUtility.Validate(new WaitStep { Duration = 0f }).Has(ValidationSeverity.Warning, "Duration"), Is.True);
            Assert.That(StepTestUtility.Validate(new WaitStep { Duration = 2f }).Issues, Is.Empty);
        }
    }

    public sealed class InstructionStepTests
    {
        [Test]
        public void WithConfirmation_CompletesOnlyAfterSignalRaisedAfterEnter()
        {
            var step = new InstructionStep { RequireConfirmation = true, ConfirmSignal = "confirm" };
            var context = new ScenarioContext();
            context.Signals.Raise("confirm");
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running), "A signal raised before the step started must not count.");

            context.Signals.Raise("confirm");

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void WithAutoAdvance_CompletesAfterDelay()
        {
            var step = new InstructionStep { RequireConfirmation = false, AutoAdvanceSeconds = 2f };
            var context = new ScenarioContext();
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 1.5f), Is.EqualTo(StepStatus.Running));
            Assert.That(step.OnTick(context, 1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void WithBoth_CompletesOnWhicheverHappensFirst()
        {
            var step = new InstructionStep { RequireConfirmation = true, ConfirmSignal = "ok", AutoAdvanceSeconds = 10f };
            var context = new ScenarioContext();
            step.OnEnter(context);
            context.Signals.Raise("ok");

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void Validate_EmptyInstruction_IsAWarning()
        {
            var step = new InstructionStep { Instruction = "" };

            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Warning, "Instruction is empty"), Is.True);
        }

        [Test]
        public void Validate_NeitherConfirmationNorAutoAdvance_IsAnError()
        {
            var step = new InstructionStep { Instruction = "Read this", RequireConfirmation = false, AutoAdvanceSeconds = 0f };

            ValidationReport report = StepTestUtility.Validate(step);

            Assert.That(report.Has(ValidationSeverity.Error, "can never finish"), Is.True);
            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void Validate_ConfirmationWithoutSignal_IsAnError()
        {
            var step = new InstructionStep { Instruction = "Read this", RequireConfirmation = true, ConfirmSignal = " " };

            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Error, "Confirm Signal is empty"), Is.True);
        }

        [Test]
        public void Validate_ValidStep_HasNoIssues()
        {
            var step = new InstructionStep { Instruction = "Read this" };

            Assert.That(StepTestUtility.Validate(step).Issues, Is.Empty);
        }
    }

    public sealed class WaitForSignalStepTests
    {
        [Test]
        public void CompletesAfterRequiredCount_CountingOnlySignalsAfterEnter()
        {
            var step = new WaitForSignalStep { Signal = "lever_pulled", RequiredCount = 2 };
            var context = new ScenarioContext();
            context.Signals.Raise("lever_pulled");
            step.OnEnter(context);

            context.Signals.Raise("lever_pulled");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            context.Signals.Raise("other");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            context.Signals.Raise("lever_pulled");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void Validate_EmptySignal_IsAnError()
        {
            Assert.That(StepTestUtility.Validate(new WaitForSignalStep { Signal = "" }).Has(ValidationSeverity.Error, "Signal is empty"), Is.True);
        }

        [Test]
        public void Validate_RequiredCountBelowOne_IsAnError()
        {
            var step = new WaitForSignalStep { Signal = "go", RequiredCount = 0 };

            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Error, "Required Count"), Is.True);
        }

        [Test]
        public void Validate_ValidStep_HasNoIssues()
        {
            Assert.That(StepTestUtility.Validate(new WaitForSignalStep { Signal = "go" }).Issues, Is.Empty);
        }
    }

    public sealed class ChecklistStepTests
    {
        static ChecklistStep Create(bool ordered, params string[] signals)
        {
            var step = new ChecklistStep { Ordered = ordered, Instruction = "Check the mirrors" };
            foreach (string signal in signals)
                step.MutableItems.Add(new ChecklistItem("Item " + signal, signal));
            return step;
        }

        [Test]
        public void Unordered_CompletesWhenEverySignalIsRaised()
        {
            ChecklistStep step = Create(false, "a", "b", "c");
            var context = new ScenarioContext();
            step.OnEnter(context);

            context.Signals.Raise("c");
            context.Signals.Raise("a");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            context.Signals.Raise("b");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void Ordered_IgnoresSignalsRaisedOutOfOrder()
        {
            ChecklistStep step = Create(true, "a", "b");
            var context = new ScenarioContext();
            step.OnEnter(context);

            context.Signals.Raise("b");
            context.Signals.Raise("a");
            Assert.That(step.CheckedCount, Is.EqualTo(1));
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            context.Signals.Raise("b");
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void SignalsRaisedBeforeEnterOrAfterExit_DoNotCount()
        {
            ChecklistStep step = Create(false, "a");
            var context = new ScenarioContext();
            context.Signals.Raise("a");
            step.OnEnter(context);
            Assert.That(step.CheckedCount, Is.Zero);

            step.OnExit(context, StepOutcome.Aborted);
            context.Signals.Raise("a");

            Assert.That(step.CheckedCount, Is.Zero);
        }

        [Test]
        public void ProgressIsReportedThroughTheRunnerInstruction()
        {
            ChecklistStep step = Create(false, "a", "b", "c", "d");
            step.AssignNewId();
            using var scenarios = new TestScenarios();
            using var runner = new ScenarioRunner(scenarios.Create(step), new ScenarioContext());
            var instructions = new List<string>();
            runner.InstructionChanged += instructions.Add;

            runner.Start();
            runner.Context.Signals.Raise("a");
            runner.Context.Signals.Raise("b");

            Assert.That(instructions, Is.EqualTo(new[]
            {
                "Check the mirrors (0/4)",
                "Check the mirrors (1/4)",
                "Check the mirrors (2/4)"
            }));
        }

        [Test]
        public void Validate_EmptyList_IsAnError()
        {
            Assert.That(StepTestUtility.Validate(Create(false)).Has(ValidationSeverity.Error, "no items"), Is.True);
        }

        [Test]
        public void Validate_DuplicateSignals_IsAWarning()
        {
            ValidationReport report = StepTestUtility.Validate(Create(false, "mirror_ok", "mirror_ok"));

            Assert.That(report.Has(ValidationSeverity.Warning, "Duplicate signal \"mirror_ok\""), Is.True);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_ItemWithoutSignal_IsAnError()
        {
            Assert.That(StepTestUtility.Validate(Create(false, "a", "")).Has(ValidationSeverity.Error, "Item 2 has no signal"), Is.True);
        }
    }

    public sealed class ReachTargetStepTests
    {
        readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in objects)
                Object.DestroyImmediate(go);
            objects.Clear();
        }

        ScenarioTarget CreateTarget(string id, Vector3 position)
        {
            var go = new GameObject(id);
            objects.Add(go);
            go.transform.position = position;
            var target = go.AddComponent<ScenarioTarget>();
            target.TargetId = id;
            return target;
        }

        [Test]
        public void CompletesWhenActorIsWithinRadius_NotBefore()
        {
            ScenarioTarget cart = CreateTarget("cart", Vector3.zero);
            ScenarioTarget dock = CreateTarget("dock", new Vector3(5f, 0f, 0f));
            var context = new ScenarioContext(new SceneTargetResolver(new[] { cart, dock }));
            var step = new ReachTargetStep { ActorTargetId = "cart", DestinationTargetId = "dock", Radius = 1f };
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            cart.transform.position = new Vector3(4.2f, 0f, 0f);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void MissingTarget_FailsWithAClearMessage()
        {
            ScenarioTarget cart = CreateTarget("cart", Vector3.zero);
            var context = new ScenarioContext(new SceneTargetResolver(new[] { cart }));
            var step = new ReachTargetStep { ActorTargetId = "cart", DestinationTargetId = "dock" };

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Failed));
            Assert.That(step.FailureReason, Does.Contain("Destination target 'dock' was not found"));
        }

        [Test]
        public void MissingTarget_ThroughRunner_FailsTheScenarioWithoutThrowing()
        {
            var step = new ReachTargetStep { ActorTargetId = "cart", DestinationTargetId = "dock" };
            step.AssignNewId();
            using var scenarios = new TestScenarios();
            using var runner = new ScenarioRunner(scenarios.Create(step), new ScenarioContext());
            runner.Start();

            runner.Tick(0.1f);

            Assert.That(runner.State, Is.EqualTo(RunnerState.Failed));
            Assert.That(runner.Results[0].FailureReason, Does.Contain("Actor target 'cart' was not found"));
        }

        [Test]
        public void Validate_EmptyIds_AreErrors()
        {
            ValidationReport report = StepTestUtility.Validate(new ReachTargetStep());

            Assert.That(report.Has(ValidationSeverity.Error, "Actor target is empty"), Is.True);
            Assert.That(report.Has(ValidationSeverity.Error, "Destination target is empty"), Is.True);
        }

        [Test]
        public void Validate_NonPositiveRadius_IsAnError()
        {
            var step = new ReachTargetStep { ActorTargetId = "a", DestinationTargetId = "b", Radius = 0f };

            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Error, "Radius"), Is.True);
        }
    }

    public sealed class SetBlackboardValueStepTests
    {
        [Test]
        public void WritesTheValueAndCompletesOnFirstTick()
        {
            var step = new SetBlackboardValueStep { Key = "parts_assembled", Value = BlackboardValue.FromInt(3) };
            var context = new ScenarioContext();
            step.OnEnter(context);

            Assert.That(context.Blackboard.Has("parts_assembled"), Is.False);
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
            Assert.That(context.Blackboard.TryGet("parts_assembled", out int value) && value == 3, Is.True);
        }

        [Test]
        public void Validate_EmptyKey_IsAnError()
        {
            Assert.That(StepTestUtility.Validate(new SetBlackboardValueStep()).Has(ValidationSeverity.Error, "Key is empty"), Is.True);
            Assert.That(StepTestUtility.Validate(new SetBlackboardValueStep { Key = "k" }).Issues, Is.Empty);
        }
    }

    public sealed class BlackboardConditionStepTests
    {
        [Test]
        public void CompletesWhenConditionHolds_NotBefore()
        {
            var step = new BlackboardConditionStep { Key = "speed", Operator = CompareOperator.GreaterOrEqual, Value = BlackboardValue.FromFloat(2f) };
            var context = new ScenarioContext();
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running), "Missing key keeps waiting when failIfMissing is off.");

            context.Blackboard.Set("speed", 1f);
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Running));

            context.Blackboard.Set("speed", 2f);
            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
        }

        [Test]
        public void FailIfMissing_FailsWhenKeyIsMissing()
        {
            var step = new BlackboardConditionStep { Key = "ready", FailIfMissing = true };
            var context = new ScenarioContext();

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Failed));
            Assert.That(step.FailureReason, Does.Contain("'ready' is missing"));
            Assert.That(step.CanFail, Is.True);
        }

        [Test]
        public void FailIfMissing_FailsWhenTypeDiffers()
        {
            var step = new BlackboardConditionStep { Key = "ready", Value = BlackboardValue.FromBool(true), FailIfMissing = true };
            var context = new ScenarioContext();
            context.Blackboard.Set("ready", 1);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Failed));
            Assert.That(step.FailureReason, Does.Contain("holds a Int"));
        }

        [Test]
        public void WithoutFailIfMissing_CannotFailOnItsOwn() => Assert.That(new BlackboardConditionStep().CanFail, Is.False);

        [Test]
        public void Validate_EmptyKey_IsAnError()
        {
            Assert.That(StepTestUtility.Validate(new BlackboardConditionStep()).Has(ValidationSeverity.Error, "Key is empty"), Is.True);
        }

        [Test]
        public void Validate_IncompatibleOperator_IsAnError()
        {
            var step = new BlackboardConditionStep { Key = "name", Operator = CompareOperator.Greater, Value = BlackboardValue.FromString("a") };

            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Error, "cannot compare String"), Is.True);
        }

        [Test]
        public void Validate_ValidStep_HasNoIssues()
        {
            var step = new BlackboardConditionStep { Key = "count", Operator = CompareOperator.Less, Value = BlackboardValue.FromInt(3) };

            Assert.That(StepTestUtility.Validate(step).Issues, Is.Empty);
        }
    }
}
