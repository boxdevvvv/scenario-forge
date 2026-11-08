using System.Collections.Generic;
using NUnit.Framework;
using ScenarioForge.Steps;

namespace ScenarioForge.Tests
{
    public sealed class MistakeTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        [Test]
        public void ReportMistake_RecordsTheActiveStepAndTime()
        {
            var first = new IdleStep();
            ScenarioDefinition definition = scenarios.Create(first);
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            var reported = new List<ScenarioMistake>();
            runner.MistakeReported += reported.Add;
            runner.Start();
            runner.Tick(0.5f);

            runner.Context.ReportMistake("  Pressed the wrong button  ");

            Assert.That(reported, Has.Count.EqualTo(1));
            Assert.That(runner.Mistakes, Has.Count.EqualTo(1));
            ScenarioMistake mistake = runner.Mistakes[0];
            Assert.That(mistake.StepIndex, Is.EqualTo(0));
            Assert.That(mistake.StepId, Is.EqualTo(first.Id));
            Assert.That(mistake.Message, Is.EqualTo("Pressed the wrong button"));
            Assert.That(mistake.Time, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void StepResult_CountsMistakesOfThatStepOnly()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep(), new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            runner.Start();

            runner.Context.ReportMistake("one");
            runner.Context.ReportMistake("two");
            runner.SkipCurrentStep();
            runner.Context.ReportMistake("three");
            runner.SkipCurrentStep();

            Assert.That(runner.Results[0].Mistakes, Is.EqualTo(2));
            Assert.That(runner.Results[1].Mistakes, Is.EqualTo(1));
            Assert.That(runner.Mistakes, Has.Count.EqualTo(3));
        }

        [Test]
        public void StepResult_CountsMistakesAcrossRetries()
        {
            var step = TestScenarios.Probe("retry", 1);
            step.finishWith = StepStatus.Failed;
            step.OnFailure = FailurePolicy.Retry;
            step.MaxRetries = 1;
            ScenarioDefinition definition = scenarios.Create(step);
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            runner.Start();

            runner.Context.ReportMistake("first attempt");
            runner.Tick(0.1f);
            runner.Context.ReportMistake("second attempt");
            runner.Tick(0.1f);

            Assert.That(runner.Results, Has.Count.EqualTo(1));
            Assert.That(runner.Results[0].Attempts, Is.EqualTo(2));
            Assert.That(runner.Results[0].Mistakes, Is.EqualTo(2));
        }

        [Test]
        public void ReportMistake_IsIgnoredWhenTheScenarioIsNotRunning()
        {
            ScenarioDefinition definition = scenarios.Create(TestScenarios.Probe("done", 1));
            var context = new ScenarioContext();
            using var runner = new ScenarioRunner(definition, context);

            context.ReportMistake("before start");
            runner.Start();
            runner.Tick(0.1f);
            Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
            context.ReportMistake("after the end");

            Assert.That(runner.Mistakes, Is.Empty);
        }

        [Test]
        public void ReportMistake_WhilePaused_IsRecorded()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            runner.Start();
            runner.Pause();

            runner.Context.ReportMistake("while paused");

            Assert.That(runner.Mistakes, Has.Count.EqualTo(1));
        }

        [Test]
        public void Restart_ClearsMistakes_AndTracksTheNewContext()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            runner.Start();
            ScenarioContext oldContext = runner.Context;
            runner.Context.ReportMistake("old run");

            runner.Restart();
            oldContext.ReportMistake("stale context");
            Assert.That(runner.Mistakes, Is.Empty);

            runner.Context.ReportMistake("new run");
            Assert.That(runner.Mistakes, Has.Count.EqualTo(1));
            Assert.That(runner.Mistakes[0].Message, Is.EqualTo("new run"));
        }

        [Test]
        public void ReportMistake_WithEmptyMessage_UsesAPlaceholder()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            runner.Start();

            runner.Context.ReportMistake("   ");

            Assert.That(runner.Mistakes[0].Message, Is.EqualTo("Mistake"));
        }
    }

    public sealed class RunnerSignalForwardingTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        [Test]
        public void SignalRaised_ForwardsSignalsFromTheGameAndFromSteps()
        {
            ScenarioDefinition definition = scenarios.Create(new SendSignalStep { Signal = "fire_started" }, new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            var signals = new List<string>();
            runner.SignalRaised += signals.Add;
            runner.Start();

            runner.Tick(0.1f);
            runner.Context.Signals.Raise(" door_opened ");

            Assert.That(signals, Is.EqualTo(new[] { "fire_started", "door_opened" }));
        }

        [Test]
        public void SignalRaised_KeepsWorkingAfterRestart()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            var signals = new List<string>();
            runner.SignalRaised += signals.Add;
            runner.Start();
            ScenarioContext oldContext = runner.Context;

            runner.Restart();
            oldContext.Signals.Raise("stale");
            runner.Context.Signals.Raise("fresh");

            Assert.That(signals, Is.EqualTo(new[] { "fresh" }));
        }
    }

    public sealed class SendSignalStepTests
    {
        [Test]
        public void RaisesTheSignalOnce_AndSucceedsOnTheFirstTick()
        {
            var step = new SendSignalStep { Signal = " alarm " };
            var context = new ScenarioContext();
            step.OnEnter(context);
            Assert.That(context.Signals.GetCount("alarm"), Is.EqualTo(0), "Nothing is raised on enter.");

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Succeeded));
            Assert.That(context.Signals.GetCount("alarm"), Is.EqualTo(1));
        }

        [Test]
        public void EmptySignal_FailsAtRuntime_AndIsAValidationError()
        {
            var step = new SendSignalStep { Signal = "" };
            var context = new ScenarioContext();
            step.OnEnter(context);

            Assert.That(step.OnTick(context, 0.1f), Is.EqualTo(StepStatus.Failed));
            Assert.That(StepTestUtility.Validate(step).Has(ValidationSeverity.Error, "Signal is empty"), Is.True);
            Assert.That(StepTestUtility.Validate(new SendSignalStep { Signal = "ok" }).Issues, Is.Empty);
        }

        [Test]
        public void CannotFailOnItsOwn() => Assert.That(new SendSignalStep().CanFail, Is.False);
    }

    public sealed class ChecklistMistakeTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        ScenarioRunner Start(bool ordered)
        {
            var step = new ChecklistStep { Ordered = ordered };
            step.MutableItems.Add(new ChecklistItem("Pull the pin", "pull_pin"));
            step.MutableItems.Add(new ChecklistItem("Aim", "aim"));
            step.MutableItems.Add(new ChecklistItem("", "squeeze"));
            var runner = new ScenarioRunner(scenarios.Create(step), new ScenarioContext());
            runner.Start();
            return runner;
        }

        [Test]
        public void Ordered_ReportsAMistake_ForALaterItemDoneTooEarly()
        {
            using ScenarioRunner runner = Start(true);

            runner.Context.Signals.Raise("aim");

            Assert.That(runner.Mistakes, Has.Count.EqualTo(1));
            Assert.That(runner.Mistakes[0].Message, Does.Contain("\"Aim\" was done out of order").And.Contain("\"Pull the pin\" first"));
        }

        [Test]
        public void Ordered_UsesTheSignal_WhenAnItemHasNoLabel()
        {
            using ScenarioRunner runner = Start(true);

            runner.Context.Signals.Raise("squeeze");

            Assert.That(runner.Mistakes[0].Message, Does.StartWith("\"squeeze\""));
        }

        [Test]
        public void Ordered_DoesNotReport_UnrelatedOrRepeatedSignals()
        {
            using ScenarioRunner runner = Start(true);

            runner.Context.Signals.Raise("something_else");
            runner.Context.Signals.Raise("pull_pin");
            runner.Context.Signals.Raise("pull_pin");

            Assert.That(runner.Mistakes, Is.Empty);
        }

        [Test]
        public void Unordered_NeverReportsMistakes()
        {
            using ScenarioRunner runner = Start(false);

            runner.Context.Signals.Raise("squeeze");
            runner.Context.Signals.Raise("aim");

            Assert.That(runner.Mistakes, Is.Empty);
        }
    }
}
