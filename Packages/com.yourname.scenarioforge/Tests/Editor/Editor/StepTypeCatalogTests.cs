using System;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Editor;
using ScenarioForge.Steps;
using UnityEditor.IMGUI.Controls;

namespace ScenarioForge.Tests
{
    public sealed class StepTypeCatalogTests
    {
        static readonly Type[] BuiltInSteps =
        {
            typeof(WaitStep), typeof(InstructionStep), typeof(WaitForSignalStep), typeof(ChecklistStep), typeof(SendSignalStep),
            typeof(ReachTargetStep), typeof(SetBlackboardValueStep), typeof(BlackboardConditionStep)
        };

        [Test]
        public void GetStepTypes_FindsEveryBuiltInStep()
        {
            Type[] found = StepTypeCatalog.GetStepTypes().Select(info => info.Type).ToArray();

            Assert.That(found, Is.SupersetOf(BuiltInSteps));
        }

        [Test]
        public void GetStepTypes_ExcludesAbstractAndNonPublicTypes()
        {
            Type[] found = StepTypeCatalog.GetStepTypes().Select(info => info.Type).ToArray();

            Assert.That(found, Has.None.Matches<Type>(type => type.IsAbstract));
            Assert.That(found, Has.No.Member(typeof(ScenarioStep)));
            Assert.That(found, Has.No.Member(typeof(AbstractTestStep)));
            Assert.That(found, Has.No.Member(typeof(ProbeStep)), "Internal test steps must not appear in the menu.");
            Assert.That(found, Has.No.Member(typeof(NoDefaultConstructorStep)));
            Assert.That(found, Has.No.Member(typeof(NotSerializableStep)));
        }

        [Test]
        public void Describe_UsesTheMenuAttribute()
        {
            StepTypeInfo info = StepTypeCatalog.Describe(typeof(WaitStep));

            Assert.That(info.Category, Is.EqualTo("Flow"));
            Assert.That(info.DisplayName, Is.EqualTo("Wait"));
            Assert.That(info.Tooltip, Is.Not.Empty);
        }

        [Test]
        public void Describe_WithoutAttribute_UsesOtherAndTheClassName()
        {
            StepTypeInfo info = StepTypeCatalog.Describe(typeof(UnattributedTestStep));

            Assert.That(info.Category, Is.EqualTo("Other"));
            Assert.That(info.DisplayName, Is.EqualTo("Unattributed Test Step"));
        }

        [Test]
        public void Dropdown_GroupsTypesByCategory_AndShowsTheTooltip()
        {
            var dropdown = new StepTypeDropdown(new AdvancedDropdownState(), "Add Step", _ => { });

            AdvancedDropdownItem root = dropdown.BuildTree();

            AdvancedDropdownItem flow = root.childList.Single(child => child.name == "Flow");
            Assert.That(flow.childList.Select(child => child.name), Has.Some.StartsWith("Wait  —  "));
            Assert.That(root.childList.Select(child => child.name), Is.SupersetOf(new[] { "Flow", "Signals", "Spatial", "Blackboard" }));
        }

        [Serializable]
        public abstract class AbstractTestStep : ScenarioStep
        {
        }

        [Serializable]
        public sealed class NoDefaultConstructorStep : ScenarioStep
        {
            public NoDefaultConstructorStep(int value)
            {
            }

            protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Running;
        }

        public sealed class NotSerializableStep : ScenarioStep
        {
            protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Running;
        }
    }

    /// <summary>A public step without menu attribute. It does appear in the menu, under "Other".</summary>
    [Serializable]
    public sealed class UnattributedTestStep : ScenarioStep
    {
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Succeeded;
    }
}
