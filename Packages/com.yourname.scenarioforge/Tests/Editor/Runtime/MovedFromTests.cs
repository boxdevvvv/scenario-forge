using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ScenarioForge.Tests.Moved
{
    /// <summary>
    /// Stands in for a step class that was renamed from <c>ScenarioForge.Tests.Legacy.LegacyRenamedStep</c>.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "ScenarioForge.Tests.Legacy", sourceAssembly: null, sourceClassName: "LegacyRenamedStep")]
    sealed class RenamedStep : ScenarioStep
    {
        [SerializeField] internal int payload;

        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Succeeded;
    }
}

namespace ScenarioForge.Tests
{
    /// <summary>
    /// Shows that <see cref="MovedFromAttribute"/> keeps assets loading after a step class is renamed or moved.
    /// </summary>
    public sealed class MovedFromTests
    {
        const string Folder = "Assets/ScenarioForgeTestTemp";
        const string AssetPath = Folder + "/MovedStep.asset";
        const string CurrentTypeEntry = "class: RenamedStep, ns: ScenarioForge.Tests.Moved";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgeTestTemp");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Folder);

        [Test]
        public void AssetWithOldTypeName_LoadsAsTheRenamedType()
        {
            RewriteSavedTypeEntry("class: LegacyRenamedStep, ns: ScenarioForge.Tests.Legacy");

            var loaded = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(AssetPath);

            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(loaded), Is.False);
            Assert.That(loaded.Steps[0], Is.TypeOf<Moved.RenamedStep>());
            Assert.That(((Moved.RenamedStep)loaded.Steps[0]).payload, Is.EqualTo(42));
        }

        [Test]
        public void AssetWithUnknownTypeName_HasAMissingType()
        {
            RewriteSavedTypeEntry("class: TypeThatNeverExisted, ns: ScenarioForge.Tests.Legacy");

            var loaded = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(AssetPath);

            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(loaded), Is.True);
            Assert.That(loaded.Steps[0], Is.Null);
        }

        static void RewriteSavedTypeEntry(string replacement)
        {
            var step = new Moved.RenamedStep { payload = 42, Title = "Renamed" };
            step.AssignNewId();
            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.SetContent("Moved", null, new ScenarioStep[] { step });
            AssetDatabase.CreateAsset(definition, AssetPath);
            AssetDatabase.SaveAssets();

            string fullPath = Path.GetFullPath(AssetPath);
            string yaml = File.ReadAllText(fullPath);
            Assert.That(yaml, Does.Contain(CurrentTypeEntry), "The managed reference type entry format changed; update this test.");

            File.WriteAllText(fullPath, yaml.Replace(CurrentTypeEntry, replacement));
            Resources.UnloadAsset(definition);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
