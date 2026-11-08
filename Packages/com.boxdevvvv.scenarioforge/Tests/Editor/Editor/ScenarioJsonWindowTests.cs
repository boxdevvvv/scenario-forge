using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Editor;
using ScenarioForge.Steps;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioJsonWindowTests
    {
        const string Folder = "Assets/ScenarioForgeJsonTestTemp";
        const string PrefKeyPrefix = "ScenarioForge.LastScenarioGuid.";

        string savedClipboard;
        string savedPref;
        ScenarioForgeWindow window;

        static string PrefKey => PrefKeyPrefix + PlayerSettings.productGUID;

        [SetUp]
        public void SetUp()
        {
            savedClipboard = EditorGUIUtility.systemCopyBuffer;
            savedPref = EditorPrefs.GetString(PrefKey, null);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgeJsonTestTemp");
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null)
                window.Close();

            AssetDatabase.DeleteAsset(Folder);
            EditorGUIUtility.systemCopyBuffer = savedClipboard;
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
            definition.SetContent(name, "Original description", steps);
            AssetDatabase.CreateAsset(definition, $"{Folder}/{name}.asset");
            return definition;
        }

        IEnumerator Open(ScenarioDefinition definition)
        {
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CopyThenPaste_InsertsACopyWithANewId_AndUndoRemovesIt()
        {
            ScenarioDefinition definition = CreateAsset("CopyPaste", new WaitStep { Title = "First", Duration = 3f }, new WaitStep { Title = "Second" });
            yield return Open(definition);
            window.SelectStep(0);

            Assert.That(window.CopySelected(), Is.True);
            Assert.That(EditorGUIUtility.systemCopyBuffer, Does.Contain("\"title\": \"First\""));
            Assert.That(window.PasteAfterSelected(), Is.True);

            Assert.That(definition.Steps.Select(s => s.Title), Is.EqualTo(new[] { "First", "First", "Second" }));
            Assert.That(definition.Steps[1].Id, Is.Not.EqualTo(definition.Steps[0].Id));
            Assert.That(((WaitStep)definition.Steps[1]).Duration, Is.EqualTo(3f));
            Assert.That(window.SelectedIndex, Is.EqualTo(1));
            Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo("Paste Step"));

            Undo.PerformUndo();
            Assert.That(definition.Steps.Select(s => s.Title), Is.EqualTo(new[] { "First", "Second" }));
        }

        [UnityTest]
        public IEnumerator PastingTheSameClipboardTwice_GivesDifferentIds()
        {
            ScenarioDefinition definition = CreateAsset("PasteTwice", new WaitStep { Title = "Only" });
            yield return Open(definition);
            window.SelectStep(0);
            window.CopySelected();

            window.PasteAfterSelected();
            window.PasteAfterSelected();

            Assert.That(definition.Steps.Select(s => s.Id).Distinct().Count(), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Paste_WithInvalidClipboard_ShowsAnErrorAndChangesNothing()
        {
            ScenarioDefinition definition = CreateAsset("BadPaste", new WaitStep { Title = "Only" });
            yield return Open(definition);
            window.SelectStep(0);

            EditorGUIUtility.systemCopyBuffer = "hello, not json";
            Assert.That(window.PasteAfterSelected(), Is.False);
            Assert.That(window.StatusMessage, Does.StartWith("Nothing to paste"));

            EditorGUIUtility.systemCopyBuffer = "{ \"formatVersion\": 1, \"scenario\": { \"steps\": [ { \"$type\": \"System.IO.FileInfo, mscorlib\" } ] } }";
            Assert.That(window.PasteAfterSelected(), Is.False);
            Assert.That(window.StatusMessage, Does.Contain("is not allowed"));

            Assert.That(definition.Steps, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Copy_OfAMissingTypeStep_ShowsAnError()
        {
            ScenarioDefinition definition = CreateAsset("CopyMissing", new WaitStep());
            definition.SetContent("CopyMissing", null, new ScenarioStep[] { null });
            yield return Open(definition);
            window.SelectStep(0);

            Assert.That(window.CopySelected(), Is.False);
            Assert.That(window.StatusMessage, Does.Contain("missing type"));
        }

        [UnityTest]
        public IEnumerator ImportReplace_ReplacesTitleDescriptionAndSteps_InOneUndoGroup()
        {
            ScenarioDefinition source = CreateAsset("Source", new SendSignalStep { Title = "Alarm", Signal = "alarm" }, new WaitStep { Title = "Pause" });
            string json = ScenarioJson.Export(source);
            ScenarioDefinition target = CreateAsset("Target", new WaitStep { Title = "Old" });
            yield return Open(target);

            Assert.That(window.ImportJson(json, ScenarioForgeWindow.ImportMode.Replace), Is.True);

            Assert.That(target.Title, Is.EqualTo("Source"));
            Assert.That(target.Steps.Select(s => s.Title), Is.EqualTo(new[] { "Alarm", "Pause" }));
            Assert.That(target.Steps[0].Id, Is.EqualTo(source.Steps[0].Id), "Ids that do not collide are kept.");
            Assert.That(window.StatusMessage, Does.Contain("Imported 2 steps").And.Contain("replaced"));
            Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo("Import Scenario (Replace)"));

            Undo.PerformUndo();
            Assert.That(target.Title, Is.EqualTo("Target"));
            Assert.That(target.Steps.Select(s => s.Title), Is.EqualTo(new[] { "Old" }));
        }

        [UnityTest]
        public IEnumerator ImportAppend_RegeneratesCollidingIds_AndReportsHowMany()
        {
            ScenarioDefinition target = CreateAsset("Append", new WaitStep { Title = "A" }, new WaitStep { Title = "B" });
            string json = ScenarioJson.Export(target);
            yield return Open(target);

            Assert.That(window.ImportJson(json, ScenarioForgeWindow.ImportMode.Append), Is.True);

            Assert.That(target.Steps.Select(s => s.Title), Is.EqualTo(new[] { "A", "B", "A", "B" }));
            Assert.That(target.Steps.Select(s => s.Id).Distinct().Count(), Is.EqualTo(4));
            Assert.That(target.Title, Is.EqualTo("Append"), "Append keeps the title.");
            Assert.That(window.StatusMessage, Does.Contain("appended").And.Contain("2 step ids were already in use"));

            Undo.PerformUndo();
            Assert.That(target.Steps, Has.Count.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ImportAppend_RegeneratesIdsRepeatedInsideTheFile()
        {
            ScenarioDefinition target = CreateAsset("Repeated");
            yield return Open(target);
            string step = "{ \"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\", \"id\": \"same\" }";
            string json = "{ \"formatVersion\": 1, \"scenario\": { \"steps\": [" + step + "," + step + ", { \"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\" }] } }";

            window.ImportJson(json, ScenarioForgeWindow.ImportMode.Append);

            Assert.That(target.Steps.Select(s => s.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(target.Steps.All(s => !string.IsNullOrEmpty(s.Id)), Is.True);
            Assert.That(target.Steps[0].Id, Is.EqualTo("same"));
        }

        [UnityTest]
        public IEnumerator ImportInvalidJson_ShowsTheErrorAndChangesNothing()
        {
            ScenarioDefinition target = CreateAsset("Invalid", new WaitStep { Title = "Keep" });
            yield return Open(target);

            Assert.That(window.ImportJson("{ \"formatVersion\": 9, \"scenario\": {} }", ScenarioForgeWindow.ImportMode.Replace), Is.False);

            Assert.That(window.StatusMessage, Does.StartWith("Import failed").And.Contain("newer"));
            Assert.That(target.Steps.Select(s => s.Title), Is.EqualTo(new[] { "Keep" }));
        }

        [UnityTest]
        public IEnumerator ToolbarHasImportAndExport_EnabledOnlyWithAScenario()
        {
            window = ScenarioForgeWindow.Open();
            window.SetScenario(null);
            yield return null;
            var import = window.rootVisualElement.Q("importButton");
            var export = window.rootVisualElement.Q("exportButton");
            Assert.That(import.enabledSelf, Is.False);
            Assert.That(export.enabledSelf, Is.False);

            window.SetScenario(CreateAsset("Toolbar", new WaitStep()));
            yield return null;
            Assert.That(import.enabledSelf, Is.True);
            Assert.That(export.enabledSelf, Is.True);
        }
    }
}
