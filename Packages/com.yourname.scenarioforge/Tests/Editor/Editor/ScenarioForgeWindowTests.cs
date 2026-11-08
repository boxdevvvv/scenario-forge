using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Editor;
using ScenarioForge.Steps;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioForgeWindowTests
    {
        const string PrefKeyPrefix = "ScenarioForge.LastScenarioGuid.";
        const string Folder = "Assets/ScenarioForgeWindowTestTemp";

        string savedPref;
        ScenarioForgeWindow window;

        static string PrefKey => PrefKeyPrefix + PlayerSettings.productGUID;

        [SetUp]
        public void SetUp()
        {
            savedPref = EditorPrefs.GetString(PrefKey, null);
            EditorPrefs.DeleteKey(PrefKey);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgeWindowTestTemp");
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null)
                window.Close();

            AssetDatabase.DeleteAsset(Folder);
            if (string.IsNullOrEmpty(savedPref))
                EditorPrefs.DeleteKey(PrefKey);
            else
                EditorPrefs.SetString(PrefKey, savedPref);
        }

        static ScenarioDefinition CreateAsset(string name, params ScenarioStep[] steps)
        {
            foreach (ScenarioStep step in steps)
                step.AssignNewId();

            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.SetContent(name, null, steps);
            AssetDatabase.CreateAsset(definition, $"{Folder}/{name}.asset");
            return definition;
        }

        [UnityTest]
        public IEnumerator Opens_WithoutScenario_ShowsTheEmptyState()
        {
            window = ScenarioForgeWindow.Open();
            yield return null;

            VisualElement empty = window.rootVisualElement.Q(className: "sf-empty");
            VisualElement editor = window.rootVisualElement.Q(className: "sf-editor-area");
            Assert.That(window.Scenario, Is.Null);
            Assert.That(empty.ClassListContains("sf-hidden"), Is.False);
            Assert.That(editor.ClassListContains("sf-hidden"), Is.True);
            Assert.That(empty.Query<Button>().ToList().Select(b => b.text), Is.EquivalentTo(new[] { "Create New Scenario", "Open Existing…" }));
        }

        [UnityTest]
        public IEnumerator Opens_WithScenario_ListsItsSteps()
        {
            ScenarioDefinition definition = CreateAsset("WithSteps", new WaitStep { Title = "Wait" }, new WaitForSignalStep { Title = "Signal" });

            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            yield return null;

            Assert.That(window.Scenario, Is.SameAs(definition));
            Assert.That(window.rootVisualElement.Q(className: "sf-editor-area").ClassListContains("sf-hidden"), Is.False);
            Assert.That(window.ListPanel.ListView.itemsSource.Count, Is.EqualTo(2));
            Assert.That(window.rootVisualElement.Q<ObjectField>().value, Is.SameAs(definition));
        }

        [UnityTest]
        public IEnumerator RemembersTheLastScenario()
        {
            ScenarioDefinition definition = CreateAsset("Remembered", new WaitStep());
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            window.Close();
            window = null;
            yield return null;

            window = ScenarioForgeWindow.Open();
            yield return null;

            Assert.That(window.Scenario, Is.SameAs(definition));
        }

        [UnityTest]
        public IEnumerator SelectingAStep_BuildsNewDetailFieldsEachTime()
        {
            ScenarioDefinition definition = CreateAsset("Details", new WaitStep { Title = "Wait" }, new ReachTargetStep { Title = "Reach" });
            window = ScenarioForgeWindow.Open(definition);
            yield return null;

            window.SelectStep(0);
            PropertyField firstField = window.DetailsPanel.Q<PropertyField>();
            int waitFields = window.DetailsPanel.FieldCount;
            window.SelectStep(1);
            int reachFields = window.DetailsPanel.FieldCount;
            window.SelectStep(0);

            Assert.That(window.DetailsPanel.HasStep, Is.True);
            Assert.That(waitFields, Is.EqualTo(6), "title, instruction, timeout, on failure, max retries and duration (id is hidden)");
            Assert.That(reachFields, Is.EqualTo(8));
            Assert.That(window.DetailsPanel.Q<PropertyField>(), Is.Not.SameAs(firstField));
        }

        [UnityTest]
        public IEnumerator AddStep_InsertsAfterTheSelection_AndSelectsIt()
        {
            ScenarioDefinition definition = CreateAsset("Add", new WaitStep { Title = "First" }, new WaitStep { Title = "Last" });
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            window.SelectStep(0);

            window.AddStep(typeof(ChecklistStep));
            yield return null;

            Assert.That(definition.Steps[1], Is.TypeOf<ChecklistStep>());
            Assert.That(window.SelectedIndex, Is.EqualTo(1));
            Assert.That(window.ListPanel.ListView.itemsSource.Count, Is.EqualTo(3));

            Undo.PerformUndo();
            yield return null;

            Assert.That(definition.Steps.Count, Is.EqualTo(2));
            Assert.That(window.SelectedIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Filter_DisablesReorderingUntilCleared()
        {
            ScenarioDefinition definition = CreateAsset("Filter", new WaitStep { Title = "Alpha" }, new WaitForSignalStep { Title = "Beta" });
            window = ScenarioForgeWindow.Open(definition);
            yield return null;

            window.ListPanel.SetFilter("beta");

            Assert.That(window.ListPanel.ListView.reorderable, Is.False);
            Assert.That(window.ListPanel.ListView.itemsSource.Count, Is.EqualTo(1));

            window.ListPanel.SetFilter("");

            Assert.That(window.ListPanel.ListView.reorderable, Is.True);
            Assert.That(window.ListPanel.ListView.itemsSource.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DeletingTheAsset_ReturnsToTheEmptyState()
        {
            ScenarioDefinition definition = CreateAsset("Deleted", new WaitStep());
            window = ScenarioForgeWindow.Open(definition);
            yield return null;

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(definition));
            yield return null;
            yield return null;

            Assert.That(window.Scenario == null, Is.True);
            Assert.That(window.rootVisualElement.Q(className: "sf-empty").ClassListContains("sf-hidden"), Is.False);
        }
    }

    public sealed class BlackboardValueDrawerTests
    {
        sealed class Holder : ScriptableObject
        {
            public BlackboardValue value = BlackboardValue.FromInt(3);
        }

        [UnityTest]
        public IEnumerator ShowsOnlyTheFieldOfTheSelectedType()
        {
            var holder = ScriptableObject.CreateInstance<Holder>();
            var serialized = new SerializedObject(holder);
            var window = EditorWindow.GetWindow<ScenarioForgeWindow>();
            var field = new PropertyField(serialized.FindProperty("value"));
            window.rootVisualElement.Add(field);
            field.Bind(serialized);
            yield return null;
            yield return null;

            try
            {
                VisualElement Visible(string name) => field.Q<PropertyField>(name);
                Assert.That(Visible("intValue").ClassListContains("sf-hidden"), Is.False);
                Assert.That(Visible("boolValue").ClassListContains("sf-hidden"), Is.True);
                Assert.That(Visible("stringValue").ClassListContains("sf-hidden"), Is.True);

                serialized.FindProperty("value.type").enumValueIndex = (int)BlackboardValueType.String;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                yield return null;
                yield return null;

                Assert.That(Visible("stringValue").ClassListContains("sf-hidden"), Is.False);
                Assert.That(Visible("intValue").ClassListContains("sf-hidden"), Is.True);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(holder);
            }
        }
    }
}
