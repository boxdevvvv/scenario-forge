using System.Collections;
using System.IO;
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
    public sealed class EditorValidationTests
    {
        const string Folder = "Assets/ScenarioForgeValidationTestTemp";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgeValidationTestTemp");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Folder);

        static ScenarioDefinition CreateAsset(string name, params ScenarioStep[] steps)
        {
            foreach (ScenarioStep step in steps)
                step.AssignNewId();

            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.SetContent(name, null, steps);
            AssetDatabase.CreateAsset(definition, $"{Folder}/{name}.asset");
            AssetDatabase.SaveAssets();
            return definition;
        }

        /// <summary>Saves an asset whose second step refers to a class that does not exist.</summary>
        static ScenarioDefinition CreateAssetWithMissingType()
        {
            ScenarioDefinition definition = CreateAsset("MissingType", new WaitStep { Title = "Fine", Duration = 1f }, new MutatingStep { Title = "Doomed" });
            string path = AssetDatabase.GetAssetPath(definition);
            string fullPath = Path.GetFullPath(path);
            const string entry = "class: MutatingStep, ns: ScenarioForge.Tests";
            string yaml = File.ReadAllText(fullPath);
            Assert.That(yaml, Does.Contain(entry));
            File.WriteAllText(fullPath, yaml.Replace(entry, "class: DeletedStep, ns: ScenarioForge.Tests"));
            Resources.UnloadAsset(definition);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
        }

        [Test]
        public void MissingTypes_AreOneErrorNamingTheType()
        {
            ScenarioDefinition definition = CreateAssetWithMissingType();

            ValidationReport report = EditorScenarioValidation.Validate(definition, checkSceneTargets: false);

            ValidationIssue[] errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToArray();
            Assert.That(errors.Length, Is.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("Missing type: ScenarioForge.Tests.DeletedStep"));
            Assert.That(errors[0].StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void RemoveMissingStep_RemovesTheElementAndItsData()
        {
            ScenarioDefinition definition = CreateAssetWithMissingType();
            var serialized = new SerializedObject(definition);

            ScenarioEditOperations.RemoveMissingStep(serialized, 1);

            Assert.That(definition.Steps.Count, Is.EqualTo(1));
            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(definition), Is.False);
            Assert.That(EditorScenarioValidation.Validate(definition, false).HasErrors, Is.False);
            Undo.ClearUndo(definition);
        }

        [Test]
        public void SceneTargets_MissingFromOpenScenes_AreWarnings()
        {
            var go = new GameObject("Dock");
            go.AddComponent<ScenarioTarget>().TargetId = "dock";
            try
            {
                ScenarioDefinition definition = CreateAsset("Targets", new ReachTargetStep { Title = "Drive", ActorTargetId = "forklift", DestinationTargetId = "dock" });

                ValidationReport withScenes = EditorScenarioValidation.Validate(definition, checkSceneTargets: true);
                ValidationReport withoutScenes = EditorScenarioValidation.Validate(definition, checkSceneTargets: false);

                ValidationIssue warning = withScenes.Issues.Single(i => i.Severity == ValidationSeverity.Warning);
                Assert.That(warning.Message, Does.Contain("'forklift' is not in the open scenes"));
                Assert.That(warning.StepIndex, Is.Zero);
                Assert.That(withScenes.HasErrors, Is.False);
                Assert.That(withoutScenes.Issues, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Summarize_CountsEachSeverity()
        {
            var report = new ValidationReport();
            Assert.That(EditorScenarioValidation.Summarize(report), Is.EqualTo("No problems found"));

            report.Add(ValidationSeverity.Error, "e");
            report.Add(ValidationSeverity.Warning, "w1");
            report.Add(ValidationSeverity.Warning, "w2");

            Assert.That(EditorScenarioValidation.Summarize(report), Is.EqualTo("1 error, 2 warnings"));
        }

        [Test]
        public void FormatIssue_PrefixesTheStep()
        {
            ScenarioDefinition definition = CreateAsset("Format", new ReachTargetStep { Title = "Drive to dock" });
            ValidationReport report = EditorScenarioValidation.Validate(definition, false);

            ValidationIssue issue = report.Issues.First(i => i.Message.Contains("Destination target is empty"));

            Assert.That(EditorScenarioValidation.FormatIssue(definition, issue), Is.EqualTo("Step 1 \"Drive to dock\": " + issue.Message));
        }

        [Test]
        public void BatchValidation_CountsErrorsAcrossTheProject()
        {
            LogAssert.ignoreFailingMessages = true;
            int before = BatchValidation.Run(out _);
            CreateAsset("BatchBroken", new WaitForSignalStep { Title = "No signal" }, new ChecklistStep { Title = "Empty" });

            int after = BatchValidation.Run(out string summary);

            Assert.That(after - before, Is.EqualTo(2));
            Assert.That(summary, Does.StartWith("Validated "));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BatchBroken.asset: Step 1 \"No signal\": Signal is empty"));
        }

        [Test]
        public void TheProjectsOwnScenarios_HaveNoErrors()
        {
            LogAssert.ignoreFailingMessages = true;

            Assert.That(BatchValidation.Run(out string summary), Is.Zero, summary);
        }
    }

    public sealed class ValidationPanelTests
    {
        const string Folder = "Assets/ScenarioForgePanelTestTemp";
        ScenarioForgeWindow window;
        string savedPref;

        static string PrefKey => "ScenarioForge.LastScenarioGuid." + PlayerSettings.productGUID;

        [SetUp]
        public void SetUp()
        {
            savedPref = EditorPrefs.GetString(PrefKey, null);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgePanelTestTemp");
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

        ScenarioDefinition CreateBrokenAsset()
        {
            var steps = new ScenarioStep[]
            {
                new WaitStep { Title = "Fine", Duration = 1f },
                new WaitStep { Duration = 1f },
                new ReachTargetStep { Title = "Drive to dock", ActorTargetId = "cart" }
            };
            foreach (ScenarioStep step in steps)
                step.AssignNewId();

            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.SetContent("Broken", null, steps);
            AssetDatabase.CreateAsset(definition, Folder + "/Broken.asset");
            return definition;
        }

        [UnityTest]
        public IEnumerator Panel_ShowsIssues_FiltersBySeverity_AndClickSelectsTheStep()
        {
            window = ScenarioForgeWindow.Open(CreateBrokenAsset());
            yield return null;
            ValidationPanel panel = window.ValidationPanel;
            panel.ShowErrors = panel.ShowWarnings = panel.ShowInfo = true;

            ValidationReport report = window.LastReport;
            Assert.That(report.Count(ValidationSeverity.Error), Is.EqualTo(1), "destination target is empty");
            Assert.That(report.Count(ValidationSeverity.Warning), Is.GreaterThanOrEqualTo(1), "step 2 has no title");
            Assert.That(panel.VisibleIssueCount, Is.EqualTo(report.Issues.Count));

            panel.ShowWarnings = false;
            panel.ShowInfo = false;
            Assert.That(panel.VisibleIssueCount, Is.EqualTo(1));

            var errorRow = panel.Q(className: "sf-validation__issue");
            using (var click = ClickEvent.GetPooled())
            {
                click.target = errorRow;
                errorRow.SendEvent(click);
            }

            yield return null;
            Assert.That(window.SelectedIndex, Is.EqualTo(2));
            panel.ShowWarnings = panel.ShowInfo = true;
        }

        [UnityTest]
        public IEnumerator Rows_ShowAnIconForTheirWorstIssue()
        {
            window = ScenarioForgeWindow.Open(CreateBrokenAsset());
            yield return null;
            yield return null;

            var rows = window.ListPanel.Query<StepRow>().ToList().OrderBy(r => r.StepIndex).ToArray();

            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[0].Issues.childCount, Is.Zero);
            Assert.That(rows[1].Issues.childCount, Is.EqualTo(1));
            Assert.That(rows[2].Issues.childCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FixingAProblem_RemovesItAfterRevalidation()
        {
            ScenarioDefinition definition = CreateBrokenAsset();
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            Assert.That(window.LastReport.HasErrors, Is.True);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("steps.Array.data[2].destinationTargetId").stringValue = "dock";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            window.RunValidation();

            Assert.That(window.LastReport.HasErrors, Is.False);
        }
    }
}
