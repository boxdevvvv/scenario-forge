using System.Collections;
using System.Collections.Generic;
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
    public sealed class DebugEventLogTests
    {
        [Test]
        public void KeepsTheLatest200Events_OldestFirst()
        {
            var log = new DebugEventLog();
            for (int i = 0; i < 250; i++)
                log.Add(i, DebugEventKind.Signal, "event " + i);

            Assert.That(log.Entries, Has.Count.EqualTo(DebugEventLog.Capacity));
            Assert.That(log.Entries[0].Message, Is.EqualTo("event 50"));
            Assert.That(log.Entries[199].Message, Is.EqualTo("event 249"));
        }

        [Test]
        public void ToText_HasOneLinePerEvent_WithTimeAndKind()
        {
            var log = new DebugEventLog();
            log.Add(1.5f, DebugEventKind.StepStarted, "Step 1 \"A\" started.");
            log.Add(12.25f, DebugEventKind.Mistake, "Too early.");

            Assert.That(log.ToText(), Is.EqualTo("1.50s  StepStarted  Step 1 \"A\" started.\n12.25s  Mistake  Too early.\n"));
        }

        [Test]
        public void ClearAndAdd_ChangeTheVersion()
        {
            var log = new DebugEventLog();
            int start = log.Version;
            log.Add(0f, DebugEventKind.Run, "x");
            int afterAdd = log.Version;
            log.Clear();

            Assert.That(afterAdd, Is.Not.EqualTo(start));
            Assert.That(log.Version, Is.Not.EqualTo(afterAdd));
            Assert.That(log.Entries, Is.Empty);
        }
    }

    public sealed class StepRunStatesTests
    {
        TestScenarios scenarios;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown() => scenarios.Dispose();

        [Test]
        public void Compute_ShowsEveryOutcome_TheActiveStep_AndPendingSteps()
        {
            ProbeStep succeeds = TestScenarios.Probe("ok", 1);
            ProbeStep timesOut = TestScenarios.Probe("slow");
            timesOut.TimeoutSeconds = 0.15f;
            timesOut.OnFailure = FailurePolicy.SkipStep;
            ProbeStep fails = TestScenarios.Probe("bad", 1);
            fails.finishWith = StepStatus.Failed;
            fails.OnFailure = FailurePolicy.SkipStep;
            ScenarioDefinition definition = scenarios.Create(succeeds, timesOut, fails, new IdleStep(), new IdleStep(), new IdleStep());
            using var runner = new ScenarioRunner(definition, new ScenarioContext());
            var states = new List<StepRunState>();

            runner.Start();
            runner.Tick(0.1f);
            runner.Tick(0.1f);
            runner.Tick(0.1f);
            runner.Tick(0.1f);
            runner.SkipCurrentStep();
            StepRunStates.Compute(runner, definition.Steps.Count, states);

            Assert.That(states, Is.EqualTo(new[]
            {
                StepRunState.Succeeded, StepRunState.TimedOut, StepRunState.Failed,
                StepRunState.Skipped, StepRunState.Active, StepRunState.Pending
            }));

            runner.Abort();
            StepRunStates.Compute(runner, definition.Steps.Count, states);
            Assert.That(states[4], Is.EqualTo(StepRunState.Aborted));
            Assert.That(states[5], Is.EqualTo(StepRunState.Pending));
        }

        [Test]
        public void Compute_WithoutARunner_IsAllPending()
        {
            var states = new List<StepRunState>();
            StepRunStates.Compute(null, 3, states);

            Assert.That(states, Is.EqualTo(Enumerable.Repeat(StepRunState.Pending, 3)));
        }

        [Test]
        public void Symbols_MatchTheSpec()
        {
            Assert.That(StepRunStates.Symbol(StepRunState.Succeeded), Is.EqualTo("✓"));
            Assert.That(StepRunStates.Symbol(StepRunState.Failed), Is.EqualTo("✕"));
            Assert.That(StepRunStates.Symbol(StepRunState.Skipped), Is.EqualTo("↷"));
            Assert.That(StepRunStates.Symbol(StepRunState.TimedOut), Is.EqualTo("⏱"));
            Assert.That(StepRunStates.Symbol(StepRunState.Pending), Is.Empty);
        }
    }

    public sealed class RunnerDebugSessionTests
    {
        TestScenarios scenarios;
        GameObject runnerObject;

        [SetUp]
        public void SetUp() => scenarios = new TestScenarios();

        [TearDown]
        public void TearDown()
        {
            if (runnerObject != null)
                Object.DestroyImmediate(runnerObject);
            scenarios.Dispose();
        }

        ScenarioRunnerBehaviour CreateRunner(ScenarioDefinition definition)
        {
            runnerObject = new GameObject("Debug Session Runner");
            var behaviour = runnerObject.AddComponent<ScenarioRunnerBehaviour>();
            behaviour.Definition = definition;
            return behaviour;
        }

        [Test]
        public void LogsRunsStatesStepsSignalsAndMistakes()
        {
            ScenarioDefinition definition = scenarios.Create(new WaitForSignalStep { Title = "Door", Signal = "door" }, new IdleStep { Title = "Idle" });
            ScenarioRunnerBehaviour behaviour = CreateRunner(definition);
            var log = new DebugEventLog();
            using var session = new RunnerDebugSession(behaviour, log);

            behaviour.StartScenario();
            behaviour.Runner.Tick(0.5f);
            behaviour.Context.ReportMistake("Too early");
            behaviour.Context.Signals.Raise("door");
            behaviour.Runner.Tick(0.25f);

            Assert.That(log.Entries.Select(e => e.Kind), Is.EqualTo(new[]
            {
                DebugEventKind.Run, DebugEventKind.State, DebugEventKind.StepStarted,
                DebugEventKind.Mistake, DebugEventKind.Signal, DebugEventKind.StepEnded, DebugEventKind.StepStarted
            }));
            Assert.That(log.Entries[2].Message, Is.EqualTo("Step 1 \"Door\" started."));
            Assert.That(log.Entries[3].Message, Is.EqualTo("Step 1 \"Door\": Too early"));
            Assert.That(log.Entries[3].Time, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(log.Entries[5].Message, Does.StartWith("Step 1 \"Door\" Succeeded after 0.8s"));
        }

        [Test]
        public void LogsANewRun_AndStateChanges()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            ScenarioRunnerBehaviour behaviour = CreateRunner(definition);
            var log = new DebugEventLog();
            using var session = new RunnerDebugSession(behaviour, log);
            behaviour.StartScenario();
            ScenarioRunner first = behaviour.Runner;

            first.Pause();
            behaviour.StartScenario();

            Assert.That(behaviour.Runner, Is.Not.SameAs(first));
            Assert.That(log.Entries.Count(e => e.Kind == DebugEventKind.Run), Is.EqualTo(2));
            Assert.That(log.Entries.Any(e => e.Message == "State: Paused."), Is.True);
        }

        [Test]
        public void Dispose_StopsLogging()
        {
            ScenarioDefinition definition = scenarios.Create(new IdleStep());
            ScenarioRunnerBehaviour behaviour = CreateRunner(definition);
            var log = new DebugEventLog();
            var session = new RunnerDebugSession(behaviour, log);
            behaviour.StartScenario();
            int count = log.Entries.Count;

            session.Dispose();
            behaviour.Context.Signals.Raise("ignored");
            behaviour.Runner.Pause();

            Assert.That(log.Entries.Count, Is.EqualTo(count));
        }
    }

    public sealed class PlayModeDebuggerWindowTests
    {
        const string Folder = "Assets/ScenarioForgeDebuggerTestTemp";
        const string PrefKeyPrefix = "ScenarioForge.LastScenarioGuid.";

        readonly List<Object> created = new();
        string savedPref;
        string savedClipboard;
        ScenarioForgeWindow window;

        static string PrefKey => PrefKeyPrefix + PlayerSettings.productGUID;

        [SetUp]
        public void SetUp()
        {
            savedPref = EditorPrefs.GetString(PrefKey, null);
            savedClipboard = EditorGUIUtility.systemCopyBuffer;
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "ScenarioForgeDebuggerTestTemp");
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null)
                window.Close();
            foreach (Object obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
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
            definition.SetContent(name, null, steps);
            AssetDatabase.CreateAsset(definition, $"{Folder}/{name}.asset");
            return definition;
        }

        ScenarioRunnerBehaviour CreateRunner(string name, ScenarioDefinition definition, bool start = true)
        {
            var go = new GameObject(name);
            created.Add(go);
            var behaviour = go.AddComponent<ScenarioRunnerBehaviour>();
            behaviour.Definition = definition;
            if (start)
                behaviour.StartScenario();
            return behaviour;
        }

        IEnumerator OpenDebugging(ScenarioDefinition definition)
        {
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            yield return null;
            window.SetDebugMode(true);
            window.RefreshDebugger();
            yield return null;
            window.RefreshDebugger();
        }

        static void Click(Button button)
        {
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }

        StepRow Row(int index) => window.ListPanel.Query<StepRow>().ToList().First(row => row.StepIndex == index);

        [UnityTest]
        public IEnumerator DebugMode_ShowsRowStates_AndHidesCleanly()
        {
            ScenarioDefinition definition = CreateAsset("Rows", new WaitForSignalStep { Title = "Door", Signal = "door" }, new IdleStep { Title = "Idle" });
            ScenarioRunnerBehaviour runner = CreateRunner("Runner", definition);
            Assert.That(window == null);
            yield return OpenDebugging(definition);

            Assert.That(window.Debugger.Bar.ClassListContains("sf-hidden"), Is.False);
            Assert.That(window.rootVisualElement.Q<Label>("assetWarning").text, Is.EqualTo(PlayModeDebugger.AssetWarning));
            Assert.That(window.Debugger.SelectedBehaviour, Is.SameAs(runner));
            Assert.That(Row(0).ClassListContains("sf-row--run-active"), Is.True);
            Assert.That(Row(0).State.text, Does.EndWith("s"));
            Assert.That(Row(1).ClassListContains("sf-row--run-pending"), Is.True);

            runner.Context.Signals.Raise("door");
            runner.Runner.Tick(0.1f);
            window.RefreshDebugger();
            Assert.That(Row(0).State.text, Is.EqualTo("✓"));
            Assert.That(Row(0).State.tooltip, Does.StartWith("Succeeded after"));
            Assert.That(Row(1).ClassListContains("sf-row--run-active"), Is.True);

            window.SetDebugMode(false);
            Assert.That(window.Debugger.Bar.ClassListContains("sf-hidden"), Is.True);
            Assert.That(window.Debugger.Panel.ClassListContains("sf-hidden"), Is.True);
            Assert.That(Row(0).State.text, Is.Empty);
            Assert.That(Row(0).ClassListContains("sf-row--run-succeeded"), Is.False);
        }

        [UnityTest]
        public IEnumerator Controls_CallTheRunner()
        {
            ScenarioDefinition definition = CreateAsset("Controls", new IdleStep { Title = "A" }, new IdleStep { Title = "B" });
            ScenarioRunnerBehaviour runner = CreateRunner("Runner", definition);
            yield return OpenDebugging(definition);
            VisualElement root = window.rootVisualElement;

            Click(root.Q<Button>("pauseButton"));
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Paused));
            window.RefreshDebugger();
            Assert.That(root.Q<Button>("pauseButton").text, Is.EqualTo("Resume"));
            Click(root.Q<Button>("pauseButton"));
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Running));

            Click(root.Q<Button>("skipButton"));
            Assert.That(runner.Runner.CurrentStepIndex, Is.EqualTo(1));

            Click(root.Q<Button>("restartButton"));
            Assert.That(runner.Runner.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(runner.Runner.Results, Is.Empty);

            window.RefreshDebugger();
            Click(root.Q<Button>("abortButton"));
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Aborted));
            window.RefreshDebugger();
            Assert.That(root.Q<Button>("abortButton").enabledSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator BlackboardAndLog_ShowLiveData_AndTheLogCanBeCopied()
        {
            ScenarioDefinition definition = CreateAsset("Data", new IdleStep { Title = "Only" });
            ScenarioRunnerBehaviour runner = CreateRunner("Runner", definition, start: false);
            window = ScenarioForgeWindow.Open(definition);
            yield return null;
            window.SetDebugMode(true);
            window.RefreshDebugger();
            runner.StartScenario();

            runner.Context.Blackboard.Set("score", 3);
            runner.Context.Blackboard.Set("door_open", true);
            runner.Context.Signals.Raise("bell");
            window.RefreshDebugger();

            Assert.That(window.Debugger.BlackboardRowCount, Is.EqualTo(2));
            Assert.That(window.Debugger.Log.Entries.Any(e => e.Message == "Signal \"bell\" raised."), Is.True);

            Click(window.rootVisualElement.Q<Button>("copyLogButton"));
            Assert.That(EditorGUIUtility.systemCopyBuffer, Does.Contain("Step 1 \"Only\" started.").And.Contain("bell"));

            Click(window.rootVisualElement.Q<Button>("clearLogButton"));
            Assert.That(window.Debugger.Log.Entries, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PicksTheRunnerOfTheOpenScenario_AndFlagsOtherScenarios()
        {
            ScenarioDefinition mine = CreateAsset("Mine", new IdleStep());
            ScenarioDefinition other = CreateAsset("Other", new IdleStep(), new IdleStep());
            ScenarioRunnerBehaviour otherRunner = CreateRunner("A Other Runner", other);
            ScenarioRunnerBehaviour myRunner = CreateRunner("B My Runner", mine);
            yield return OpenDebugging(mine);

            Assert.That(window.Debugger.Runners, Is.EqualTo(new[] { otherRunner, myRunner }));
            Assert.That(window.Debugger.SelectedBehaviour, Is.SameAs(myRunner));
            Assert.That(window.Debugger.ShowsWindowScenario, Is.True);

            window.Debugger.SelectRunner(0);
            window.RefreshDebugger();

            Assert.That(window.Debugger.SelectedBehaviour, Is.SameAs(otherRunner), "The user's pick is kept.");
            Assert.That(window.Debugger.ShowsWindowScenario, Is.False);
            Assert.That(window.Debugger.States, Is.Empty);
            Assert.That(Row(0).State.text, Is.Empty);
            Assert.That(window.Debugger.Panel.Q(className: "sf-debug-notice").ClassListContains("sf-hidden"), Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutRunners_ShowsAHint_AndDisablesControls()
        {
            ScenarioDefinition definition = CreateAsset("Lonely", new IdleStep());
            yield return OpenDebugging(definition);

            Assert.That(window.Debugger.SelectedBehaviour, Is.Null);
            Assert.That(window.rootVisualElement.Q<DropdownField>("runnerField").value, Does.Contain("No ScenarioRunnerBehaviour"));
            Assert.That(window.rootVisualElement.Q<Button>("restartButton").enabledSelf, Is.False);
        }
    }
}
