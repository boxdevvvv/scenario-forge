using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScenarioForge.Steps;
using UnityEngine;

namespace ScenarioForge.Tests
{
    /// <summary>Exercises which fields the JSON serializer picks, like Unity's serializer.</summary>
    [Serializable]
    sealed class JsonFieldsTestStep : ScenarioStep
    {
        public int publicField = 1;
        [SerializeField] string privateSerialized = "kept";
        [NonSerialized] public int nonSerialized = 2;
        string privateNotSerialized = "skipped";
        public readonly int readOnlyField = 3;
        [SerializeField] ScriptableObject assetReference;
        public int Property { get; set; } = 4;
        [SerializeField] internal Vector2 v2;
        [SerializeField] internal Vector3 v3;
        [SerializeField] internal Quaternion rotation = Quaternion.identity;
        [SerializeField] internal Color color = Color.white;

        internal string PrivateSerialized { get => privateSerialized; set => privateSerialized = value; }
        internal string PrivateNotSerialized => privateNotSerialized;
        internal ScriptableObject AssetReference { get => assetReference; set => assetReference = value; }

        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime) => StepStatus.Succeeded;
    }

    public sealed class ScenarioJsonTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        static ScenarioStep[] AllBuiltInSteps()
        {
            var checklist = new ChecklistStep { Title = "Check", Instruction = "Check it", Ordered = true };
            checklist.MutableItems.Add(new ChecklistItem("Pin", "pull_pin"));
            checklist.MutableItems.Add(new ChecklistItem("Aim", "aim"));
            return new ScenarioStep[]
            {
                new WaitStep { Title = "Wait", Duration = 2.5f, TimeoutSeconds = 4f, OnFailure = FailurePolicy.SkipStep },
                new InstructionStep { Title = "Read", Instruction = "Read \"this\"\nnow", ConfirmSignal = "ok", AutoAdvanceSeconds = 3f },
                new WaitForSignalStep { Title = "Door", Signal = "door", RequiredCount = 2, OnFailure = FailurePolicy.Retry, MaxRetries = 3 },
                checklist,
                new ReachTargetStep { Title = "Go", ActorTargetId = "player", DestinationTargetId = "dock", Radius = 1.25f },
                new SetBlackboardValueStep { Title = "Set", Key = "score", Value = BlackboardValue.FromInt(7) },
                new BlackboardConditionStep { Title = "Cond", Key = "speed", Operator = CompareOperator.GreaterOrEqual, Value = BlackboardValue.FromFloat(0.5f), FailIfMissing = true },
                new SendSignalStep { Title = "Alarm", Signal = "alarm" }
            };
        }

        static ScenarioData Import(string json)
        {
            bool ok = ScenarioJson.TryImport(json, out ScenarioData data, out IReadOnlyList<string> errors);
            Assert.That(ok, Is.True, string.Join("\n", errors));
            Assert.That(errors, Is.Empty);
            return data;
        }

        static IReadOnlyList<string> ImportErrors(string json)
        {
            bool ok = ScenarioJson.TryImport(json, out ScenarioData data, out IReadOnlyList<string> errors);
            Assert.That(ok, Is.False);
            Assert.That(data, Is.Null);
            Assert.That(errors, Is.Not.Empty);
            return errors;
        }

        static string Wrap(string steps, int version = 1) =>
            "{ \"formatVersion\": " + version + ", \"scenario\": { \"title\": \"T\", \"description\": \"\", \"steps\": [" + steps + "] } }";

        [Test]
        public void RoundTrip_WithEveryBuiltInStep_ProducesTheSameJson()
        {
            ScenarioDefinition definition = scenarios.Create(AllBuiltInSteps());
            string first = ScenarioJson.Export(definition);

            ScenarioDefinition imported = Import(first).CreateDefinition();
            try
            {
                Assert.That(ScenarioJson.Export(imported), Is.EqualTo(first));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(imported);
            }
        }

        [Test]
        public void RoundTrip_KeepsEveryFieldValue()
        {
            ScenarioStep[] original = AllBuiltInSteps();
            ScenarioData data = Import(ScenarioJson.Export(scenarios.Create(original)));

            Assert.That(data.Title, Is.EqualTo("Test scenario"));
            Assert.That(data.Description, Is.EqualTo("Created by a test."));
            Assert.That(data.Steps.Select(s => s.GetType()), Is.EqualTo(original.Select(s => s.GetType())));
            Assert.That(data.Steps.Select(s => s.Id), Is.EqualTo(original.Select(s => s.Id)));

            var wait = (WaitStep)data.Steps[0];
            Assert.That(wait.Duration, Is.EqualTo(2.5f));
            Assert.That(wait.TimeoutSeconds, Is.EqualTo(4f));
            Assert.That(wait.OnFailure, Is.EqualTo(FailurePolicy.SkipStep));
            Assert.That(((InstructionStep)data.Steps[1]).Instruction, Is.EqualTo("Read \"this\"\nnow"));
            Assert.That(((WaitForSignalStep)data.Steps[2]).MaxRetries, Is.EqualTo(3));
            var checklist = (ChecklistStep)data.Steps[3];
            Assert.That(checklist.Ordered, Is.True);
            Assert.That(checklist.Items.Select(i => i.Label + "=" + i.Signal), Is.EqualTo(new[] { "Pin=pull_pin", "Aim=aim" }));
            Assert.That(((ReachTargetStep)data.Steps[4]).Radius, Is.EqualTo(1.25f));
            Assert.That(((SetBlackboardValueStep)data.Steps[5]).Value, Is.EqualTo(BlackboardValue.FromInt(7)));
            var condition = (BlackboardConditionStep)data.Steps[6];
            Assert.That(condition.Operator, Is.EqualTo(CompareOperator.GreaterOrEqual));
            Assert.That(condition.Value, Is.EqualTo(BlackboardValue.FromFloat(0.5f)));
            Assert.That(condition.FailIfMissing, Is.True);
            Assert.That(((SendSignalStep)data.Steps[7]).Signal, Is.EqualTo("alarm"));
        }

        [Test]
        public void Export_UsesTheDocumentedFormat()
        {
            var step = new WaitStep { Title = "Wait", OnFailure = FailurePolicy.Retry };
            string json = ScenarioJson.Export(scenarios.Create(step));

            Assert.That(json, Does.Contain("\"formatVersion\": 1"));
            Assert.That(json, Does.Contain("\"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\""));
            Assert.That(json, Does.Contain("\"onFailure\": \"Retry\""), "Enums are written as text.");
            int type = json.IndexOf("$type", StringComparison.Ordinal);
            int id = json.IndexOf("\"id\"", StringComparison.Ordinal);
            int duration = json.IndexOf("\"duration\"", StringComparison.Ordinal);
            Assert.That(type < id && id < duration, Is.True, "Base class fields come first, after $type.");
        }

        [Test]
        public void Export_SerializesTheSameFieldsAsUnity()
        {
            var step = new JsonFieldsTestStep { publicField = 10, PrivateSerialized = "yes", nonSerialized = 20, Property = 40 };
            var asset = ScriptableObject.CreateInstance<ScenarioDefinition>();
            step.AssetReference = asset;
            try
            {
                string json = ScenarioJson.Export(scenarios.Create(step));

                Assert.That(json, Does.Contain("\"publicField\": 10"));
                Assert.That(json, Does.Contain("\"privateSerialized\": \"yes\""));
                Assert.That(json, Does.Not.Contain("nonSerialized"));
                Assert.That(json, Does.Not.Contain("privateNotSerialized"));
                Assert.That(json, Does.Not.Contain("readOnlyField"));
                Assert.That(json, Does.Not.Contain("Property"));
                Assert.That(json, Does.Not.Contain("assetReference"), "Asset references cannot travel in JSON.");

                var imported = (JsonFieldsTestStep)Import(json).Steps[0];
                Assert.That(imported.publicField, Is.EqualTo(10));
                Assert.That(imported.PrivateSerialized, Is.EqualTo("yes"));
                Assert.That(imported.nonSerialized, Is.EqualTo(2), "Non-serialized fields keep their default.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void UnityStructs_UseExplicitConverters()
        {
            var step = new JsonFieldsTestStep
            {
                v2 = new Vector2(1f, 2f),
                v3 = new Vector3(1f, -2.5f, 3f),
                rotation = Quaternion.Euler(0f, 90f, 0f),
                color = new Color(0.25f, 0.5f, 0.75f, 0.5f)
            };

            string json = ScenarioJson.Export(scenarios.Create(step));
            var imported = (JsonFieldsTestStep)Import(json).Steps[0];

            Assert.That(json, Does.Not.Contain("normalized").And.Not.Contain("magnitude"));
            Assert.That(imported.v2, Is.EqualTo(step.v2));
            Assert.That(imported.v3, Is.EqualTo(step.v3));
            Assert.That(imported.rotation, Is.EqualTo(step.rotation));
            Assert.That(imported.color, Is.EqualTo(step.color));
        }

        [Test]
        public void UnityStructs_MissingComponents_UseSensibleDefaults()
        {
            string type = typeof(JsonFieldsTestStep).FullName + ", " + typeof(JsonFieldsTestStep).Assembly.GetName().Name;
            var imported = (JsonFieldsTestStep)Import(Wrap($"{{ \"$type\": \"{type}\", \"v3\": {{ \"x\": 5 }}, \"rotation\": {{ }}, \"color\": {{ \"r\": 1 }} }}")).Steps[0];

            Assert.That(imported.v3, Is.EqualTo(new Vector3(5f, 0f, 0f)));
            Assert.That(imported.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(imported.color, Is.EqualTo(new Color(1f, 0f, 0f, 1f)));
        }

        [Test]
        public void MissingFields_KeepTheStepDefaults()
        {
            var imported = (WaitStep)Import(Wrap("{ \"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\", \"id\": \"abc\" }")).Steps[0];

            Assert.That(imported.Id, Is.EqualTo("abc"));
            Assert.That(imported.Duration, Is.EqualTo(1f));
            Assert.That(imported.OnFailure, Is.EqualTo(FailurePolicy.FailScenario));
        }

        [Test]
        public void TypeThatIsNotAStep_IsRejected()
        {
            IReadOnlyList<string> errors = ImportErrors(Wrap("{ \"$type\": \"System.IO.FileInfo, mscorlib\", \"fileName\": \"/etc/passwd\" }"));

            Assert.That(errors[0], Does.Contain("Step 1").And.Contain("System.IO.FileInfo").And.Contain("is not allowed"));
        }

        [Test]
        public void TypeOutsideTheStepList_IsRejected()
        {
            string json = "{ \"formatVersion\": 1, \"scenario\": { \"$type\": \"System.Collections.Generic.List`1[[System.String, mscorlib]], mscorlib\", \"steps\": [] } }";

            IReadOnlyList<string> errors = ImportErrors(json);

            Assert.That(errors[0], Does.Contain("not allowed"));
        }

        [Test]
        public void AbstractStepType_IsRejected()
        {
            IReadOnlyList<string> errors = ImportErrors(Wrap("{ \"$type\": \"ScenarioForge.ScenarioStep, ScenarioForge.Runtime\" }"));

            Assert.That(errors[0], Does.Contain("is not allowed"));
        }

        [Test]
        public void UnknownStepType_SaysHowToFixIt()
        {
            IReadOnlyList<string> errors = ImportErrors(Wrap("{ \"$type\": \"My.Game.DeletedStep, Assembly-CSharp\" }"));

            Assert.That(errors[0], Does.Contain("Unknown step type \"My.Game.DeletedStep, Assembly-CSharp\""));
        }

        [Test]
        public void EveryBadStep_IsReported()
        {
            IReadOnlyList<string> errors = ImportErrors(Wrap(
                "{ \"$type\": \"Nope.A, X\" }, { \"title\": \"no type\" }, 42, { \"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\" }"));

            Assert.That(errors, Has.Count.EqualTo(3));
            Assert.That(errors[0], Does.StartWith("Step 1"));
            Assert.That(errors[1], Does.StartWith("Step 2 has no \"$type\""));
            Assert.That(errors[2], Does.StartWith("Step 3 is not an object"));
        }

        [TestCase(null, "is empty")]
        [TestCase("   ", "is empty")]
        [TestCase("{ \"formatVersion\": 1, ", "not valid JSON")]
        [TestCase("{ \"scenario\": { } }", "\"formatVersion\" is missing")]
        [TestCase("{ \"formatVersion\": \"1\", \"scenario\": { } }", "must be a whole number")]
        [TestCase("{ \"formatVersion\": 2, \"scenario\": { } }", "newer than this version")]
        [TestCase("{ \"formatVersion\": 0, \"scenario\": { } }", "is not valid")]
        [TestCase("{ \"formatVersion\": 1 }", "\"scenario\" object is missing")]
        [TestCase("{ \"formatVersion\": 1, \"scenario\": { \"steps\": 5 } }", "must be an array")]
        [TestCase("{ \"formatVersion\": 1, \"formatVersion\": 1, \"scenario\": { } }", "not valid JSON")]
        public void InvalidFiles_ReportAClearError(string json, string expected)
        {
            IReadOnlyList<string> errors = ImportErrors(json);

            Assert.That(errors[0], Does.Contain(expected));
        }

        [Test]
        public void WrongValueType_IsReportedWithoutThrowing()
        {
            IReadOnlyList<string> errors = ImportErrors(Wrap("{ \"$type\": \"ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime\", \"duration\": \"long\" }"));

            Assert.That(errors[0], Does.Contain("could not be read"));
        }

        [Test]
        public void EmptyStepList_IsAValidScenario()
        {
            ScenarioData data = Import("{ \"formatVersion\": 1, \"scenario\": { \"title\": \"Empty\" } }");

            Assert.That(data.Title, Is.EqualTo("Empty"));
            Assert.That(data.Steps, Is.Empty);
        }

        [Test]
        public void CreateDefinition_GivesARunnableScenario()
        {
            ScenarioData data = Import(Wrap("{ \"$type\": \"ScenarioForge.Steps.SetBlackboardValueStep, ScenarioForge.Runtime\", \"id\": \"s1\", \"key\": \"done\" }"));
            ScenarioDefinition definition = data.CreateDefinition();
            try
            {
                using var runner = new ScenarioRunner(definition, new ScenarioContext());
                runner.Start();
                runner.Tick(0.1f);

                Assert.That(definition.name, Is.EqualTo("T"));
                Assert.That(runner.State, Is.EqualTo(RunnerState.Completed));
                Assert.That(runner.Context.Blackboard.TryGet("done", out bool done) && done, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Export_WithAMissingTypeStep_Throws()
        {
            ScenarioDefinition definition = scenarios.Create(new WaitStep(), null);

            var exception = Assert.Throws<InvalidOperationException>(() => ScenarioJson.Export(definition));
            Assert.That(exception.Message, Does.Contain("Step 2 has a missing type"));
        }

        [Test]
        public void Export_Null_Throws() => Assert.Throws<ArgumentNullException>(() => ScenarioJson.Export(null));
    }
}
