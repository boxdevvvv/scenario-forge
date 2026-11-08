using NUnit.Framework;
using ScenarioForge.Editor;
using ScenarioForge.Steps;

namespace ScenarioForge.Tests
{
    public sealed class StepChoicesTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        [Test]
        public void Build_LabelsStepsByNumberAndTitle_FallingBackToTheTypeName()
        {
            var titled = new WaitStep { Title = "Breathe" };
            var untitled = new SendSignalStep();
            ScenarioDefinition definition = scenarios.Create(titled, null, untitled);

            var choices = StepChoices.Build(definition);

            Assert.That(choices, Has.Count.EqualTo(2), "Missing-type steps have no id and are not offered.");
            Assert.That(choices[0].Id, Is.EqualTo(titled.Id));
            Assert.That(choices[0].Label, Is.EqualTo("1. Breathe"));
            Assert.That(choices[1].Label, Is.EqualTo("3. Send Signal"));
        }

        [Test]
        public void LabelFor_HandlesEmptyKnownAndMissingIds()
        {
            var step = new WaitStep { Title = "Breathe" };
            var choices = StepChoices.Build(scenarios.Create(step));

            Assert.That(StepChoices.LabelFor(choices, ""), Is.EqualTo(StepChoices.NoneLabel));
            Assert.That(StepChoices.LabelFor(choices, step.Id), Is.EqualTo("1. Breathe"));
            Assert.That(StepChoices.LabelFor(choices, "0123456789abcdef"), Is.EqualTo("(missing step 01234567)"));
        }

        [Test]
        public void Build_WithNoDefinition_IsEmpty() => Assert.That(StepChoices.Build(null), Is.Empty);
    }
}
