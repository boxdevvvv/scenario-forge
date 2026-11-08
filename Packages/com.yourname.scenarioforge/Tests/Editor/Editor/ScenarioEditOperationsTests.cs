using System.Linq;
using NUnit.Framework;
using ScenarioForge.Editor;
using ScenarioForge.Steps;
using UnityEditor;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioEditOperationsTests
    {
        TestScenarios scenarios;
        ScenarioDefinition definition;
        SerializedObject serialized;

        [SetUp]
        public void SetUp()
        {
            scenarios = new TestScenarios();
            definition = scenarios.Create(
                new WaitStep { Title = "A", Duration = 2f },
                new WaitForSignalStep { Title = "B", Signal = "door" },
                new InstructionStep { Title = "C", Instruction = "Read" });
            serialized = new SerializedObject(definition);
            Undo.ClearUndo(definition);
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearUndo(definition);
            scenarios.Dispose();
        }

        string[] Titles() => definition.Steps.Select(step => step?.Title).ToArray();

        [Test]
        public void AddStep_InsertsANewStepWithAUniqueId_AndUndoRemovesIt()
        {
            int index = ScenarioEditOperations.AddStep(serialized, typeof(ReachTargetStep), 1);

            Assert.That(index, Is.EqualTo(1));
            Assert.That(definition.Steps.Count, Is.EqualTo(4));
            Assert.That(definition.Steps[1], Is.TypeOf<ReachTargetStep>());
            Assert.That(definition.Steps[1].Id, Is.Not.Empty);
            Assert.That(definition.Steps.Select(s => s.Id).Distinct().Count(), Is.EqualTo(4));
            Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo(ScenarioEditOperations.AddStepUndo));

            Undo.PerformUndo();

            Assert.That(Titles(), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void AddStep_PastTheEnd_AppendsAndIntoEmptyListWorks()
        {
            int index = ScenarioEditOperations.AddStep(serialized, typeof(WaitStep), int.MaxValue);
            Assert.That(index, Is.EqualTo(3));

            ScenarioDefinition empty = scenarios.Create();
            var emptySerialized = new SerializedObject(empty);
            Assert.That(ScenarioEditOperations.AddStep(emptySerialized, typeof(WaitStep), 0), Is.Zero);
            Assert.That(empty.Steps.Count, Is.EqualTo(1));
            Undo.ClearUndo(empty);
        }

        [Test]
        public void AddStep_WithANonCreatableType_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => ScenarioEditOperations.AddStep(serialized, typeof(ScenarioStep), 0));
        }

        [Test]
        public void DeleteStep_RemovesIt_AndUndoRestoresIt()
        {
            string removedId = definition.Steps[1].Id;

            int next = ScenarioEditOperations.DeleteStep(serialized, 1);

            Assert.That(next, Is.EqualTo(1));
            Assert.That(Titles(), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo(ScenarioEditOperations.DeleteStepUndo));

            Undo.PerformUndo();

            Assert.That(Titles(), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(definition.Steps[1].Id, Is.EqualTo(removedId));
        }

        [Test]
        public void DeleteStep_Last_SelectsThePreviousOne()
        {
            Assert.That(ScenarioEditOperations.DeleteStep(serialized, 2), Is.EqualTo(1));
        }

        [Test]
        public void DuplicateStep_InsertsACopyWithANewId_AndUndoRemovesIt()
        {
            int copy = ScenarioEditOperations.DuplicateStep(serialized, 0);

            Assert.That(copy, Is.EqualTo(1));
            var original = (WaitStep)definition.Steps[0];
            var duplicate = (WaitStep)definition.Steps[1];
            Assert.That(duplicate, Is.Not.SameAs(original));
            Assert.That(duplicate.Title, Is.EqualTo("A"));
            Assert.That(duplicate.Duration, Is.EqualTo(2f));
            Assert.That(duplicate.Id, Is.Not.EqualTo(original.Id));

            Undo.PerformUndo();

            Assert.That(Titles(), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void DuplicateStep_CopyIsIndependentOfTheOriginal()
        {
            ScenarioEditOperations.DuplicateStep(serialized, 0);

            ((WaitStep)definition.Steps[1]).Duration = 9f;

            Assert.That(((WaitStep)definition.Steps[0]).Duration, Is.EqualTo(2f));
        }

        [Test]
        public void MoveStep_Reorders_AndUndoRestoresTheOrder()
        {
            int moved = ScenarioEditOperations.MoveStep(serialized, 0, 2);

            Assert.That(moved, Is.EqualTo(2));
            Assert.That(Titles(), Is.EqualTo(new[] { "B", "C", "A" }));

            Undo.PerformUndo();

            Assert.That(Titles(), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void MoveStep_OutOfRange_DoesNothing()
        {
            Assert.That(ScenarioEditOperations.MoveStep(serialized, 0, -1), Is.Zero);
            Assert.That(ScenarioEditOperations.MoveStep(serialized, 2, 3), Is.EqualTo(2));
            Assert.That(Titles(), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void ChangeStepType_KeepsCommonFields_AndUndoRestoresTheOldStep()
        {
            ScenarioStep old = definition.Steps[0];
            old.Instruction = "Wait here";
            old.TimeoutSeconds = 5f;
            old.OnFailure = FailurePolicy.Retry;
            old.MaxRetries = 2;
            serialized.Update();

            bool changed = ScenarioEditOperations.ChangeStepType(serialized, 0, typeof(WaitForSignalStep));

            Assert.That(changed, Is.True);
            ScenarioStep replacement = definition.Steps[0];
            Assert.That(replacement, Is.TypeOf<WaitForSignalStep>());
            Assert.That(replacement.Id, Is.EqualTo(old.Id));
            Assert.That(replacement.Title, Is.EqualTo("A"));
            Assert.That(replacement.Instruction, Is.EqualTo("Wait here"));
            Assert.That(replacement.TimeoutSeconds, Is.EqualTo(5f));
            Assert.That(replacement.OnFailure, Is.EqualTo(FailurePolicy.Retry));
            Assert.That(replacement.MaxRetries, Is.EqualTo(2));

            Undo.PerformUndo();

            Assert.That(definition.Steps[0], Is.TypeOf<WaitStep>());
            Assert.That(((WaitStep)definition.Steps[0]).Duration, Is.EqualTo(2f));
        }

        [Test]
        public void ResetStep_RestoresDefaultsButKeepsTheId()
        {
            string id = definition.Steps[0].Id;

            ScenarioEditOperations.ResetStep(serialized, 0);

            var reset = (WaitStep)definition.Steps[0];
            Assert.That(reset.Id, Is.EqualTo(id));
            Assert.That(reset.Title, Is.Null.Or.Empty);
            Assert.That(reset.Duration, Is.EqualTo(new WaitStep().Duration));
        }

        [Test]
        public void Operations_MarkTheAssetDirty()
        {
            EditorUtility.ClearDirty(definition);

            ScenarioEditOperations.AddStep(serialized, typeof(WaitStep), 0);

            Assert.That(EditorUtility.IsDirty(definition), Is.True);
        }
    }
}
