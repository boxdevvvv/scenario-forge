using System;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Steps;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioValidatorTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        static WaitStep ValidWait(string title = "Wait") => new() { Title = title, Duration = 1f };

        [Test]
        public void ValidScenario_HasNoIssues()
        {
            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(ValidWait(), ValidWait("Again")));

            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void NullDefinition_IsAnError()
        {
            Assert.That(ScenarioValidator.Validate(null).HasErrors, Is.True);
        }

        [Test]
        public void EmptyScenario_IsAWarning()
        {
            ValidationReport report = ScenarioValidator.Validate(scenarios.Create());

            Assert.That(report.Has(ValidationSeverity.Warning, "no steps"), Is.True);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void NullElement_IsAnErrorWithItsIndex()
        {
            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(ValidWait(), null));

            ValidationIssue issue = report.Issues.Single(i => i.Severity == ValidationSeverity.Error);
            Assert.That(issue.Message, Does.Contain("Step 2 has a missing type"));
            Assert.That(issue.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void MissingTitle_IsAWarning()
        {
            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(new WaitStep { Duration = 1f }));

            Assert.That(report.Has(ValidationSeverity.Warning, "has no title"), Is.True);
        }

        [Test]
        public void DuplicateIds_AreAnError()
        {
            WaitStep first = ValidWait("First");
            WaitStep second = ValidWait("Second");
            first.AssignNewId();
            second.Id = first.Id;

            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(first, second));

            ValidationIssue issue = report.Issues.Single(i => i.Severity == ValidationSeverity.Error);
            Assert.That(issue.Message, Does.Contain("same id as step 1"));
            Assert.That(issue.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void EmptyId_IsAnError()
        {
            ScenarioDefinition definition = scenarios.Create(ValidWait());
            definition.Steps[0].Id = "";

            Assert.That(ScenarioValidator.Validate(definition).Has(ValidationSeverity.Error, "has no id"), Is.True);
        }

        [Test]
        public void RetryWithoutRetries_IsAWarning()
        {
            WaitForSignalStep step = new() { Title = "Retry", Signal = "go", OnFailure = FailurePolicy.Retry, MaxRetries = 0 };

            Assert.That(ScenarioValidator.Validate(scenarios.Create(step)).Has(ValidationSeverity.Warning, "never retries"), Is.True);
        }

        [Test]
        public void TimeoutOnAStepThatCannotFail_IsInfo()
        {
            WaitStep step = ValidWait();
            step.TimeoutSeconds = 5f;

            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(step));

            Assert.That(report.Has(ValidationSeverity.Info, "has a timeout"), Is.True);
            Assert.That(report.Count(ValidationSeverity.Info), Is.EqualTo(1));
        }

        [Test]
        public void TimeoutOnAStepThatCanFail_IsNotReported()
        {
            ReachTargetStep step = new() { Title = "Reach", ActorTargetId = "a", DestinationTargetId = "b", TimeoutSeconds = 5f };

            Assert.That(ScenarioValidator.Validate(scenarios.Create(step)).Issues, Is.Empty);
        }

        [Test]
        public void StepRules_AreIncludedWithTheStepIndexAndId()
        {
            WaitForSignalStep broken = new() { Title = "Broken" };

            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(ValidWait(), broken));

            ValidationIssue issue = report.Issues.Single(i => i.Message.Contains("Signal is empty"));
            Assert.That(issue.StepIndex, Is.EqualTo(1));
            Assert.That(issue.StepId, Is.EqualTo(broken.Id));
            Assert.That(report.GetWorstSeverity(1), Is.EqualTo(ValidationSeverity.Error));
            Assert.That(report.GetWorstSeverity(0), Is.Null);
        }

        [Test]
        public void StepWhoseValidateThrows_IsReportedAsAnError()
        {
            ValidationReport report = ScenarioValidator.Validate(scenarios.Create(new ThrowingValidationStep { Title = "Throws" }));

            Assert.That(report.Has(ValidationSeverity.Error, "Validate method threw InvalidOperationException"), Is.True);
        }

        [Test]
        public void Describe_UsesTheTitleWhenThereIsOne()
        {
            Assert.That(ScenarioValidator.Describe(new WaitStep { Title = "Drive to dock" }, 3), Is.EqualTo("Step 4 \"Drive to dock\""));
            Assert.That(ScenarioValidator.Describe(new WaitStep(), 0), Is.EqualTo("Step 1"));
            Assert.That(ScenarioValidator.Describe(null, 1), Is.EqualTo("Step 2"));
        }

        [Serializable]
        sealed class ThrowingValidationStep : ScenarioStep
        {
            protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Succeeded;

            public override void Validate(ValidationReport report) => throw new InvalidOperationException("boom");
        }
    }
}
