using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// The Play Mode debugger of the Scenario Forge window: a bar with the runner picker and run controls,
    /// and a panel with Blackboard and Event Log tabs. The window refreshes it with <see cref="Refresh"/>
    /// every 100 ms while it is active.
    /// </summary>
    sealed class PlayModeDebugger
    {
        public const string AssetWarning = "Editing the asset does not affect the running instance.";
        const int RunnerListRefreshTicks = 5;

        readonly DebugEventLog log = new();
        readonly List<ScenarioRunnerBehaviour> runners = new();
        readonly List<StepRunState> states = new();
        readonly List<BlackboardRow> blackboardRows = new();
        readonly List<DebugEvent> logRows = new();
        readonly DropdownField runnerField;
        readonly Button pauseButton;
        readonly Button skipButton;
        readonly Button restartButton;
        readonly Button abortButton;
        readonly Label stateLabel;
        readonly VisualElement otherScenarioNotice;
        readonly Label otherScenarioLabel;
        readonly ToolbarToggle blackboardTab;
        readonly ToolbarToggle logTab;
        readonly ListView blackboardView;
        readonly ListView logView;
        readonly Label blackboardEmpty;

        RunnerDebugSession session;
        ScenarioDefinition windowScenario;
        bool userPickedRunner;
        int ticksUntilRunnerRefresh;
        int shownLogVersion = -1;

        public PlayModeDebugger()
        {
            Bar = new VisualElement { name = "debugBar" };
            Bar.AddToClassList("sf-debug-bar");

            var mode = new Label("PLAY MODE");
            mode.AddToClassList("sf-debug-bar__mode");
            Bar.Add(mode);

            runnerField = new DropdownField { name = "runnerField", tooltip = "The ScenarioRunnerBehaviour to debug." };
            runnerField.AddToClassList("sf-debug-bar__runner");
            runnerField.RegisterValueChangedCallback(evt => OnRunnerPicked(runnerField.index));
            Bar.Add(runnerField);

            stateLabel = new Label { name = "runnerState" };
            stateLabel.AddToClassList("sf-debug-bar__state");
            Bar.Add(stateLabel);

            pauseButton = BarButton("pauseButton", "Pause", "Pause or resume the scenario.", TogglePause);
            skipButton = BarButton("skipButton", "Skip Step", "End the active step as Skipped and move on.", () => Runner?.SkipCurrentStep());
            restartButton = BarButton("restartButton", "Restart", "Start the scenario again from the first step.", () => Runner?.Restart());
            abortButton = BarButton("abortButton", "Abort", "Stop the scenario.", () => Runner?.Abort());

            var warning = new Label(AssetWarning) { name = "assetWarning" };
            warning.AddToClassList("sf-debug-bar__warning");
            Bar.Add(warning);

            otherScenarioNotice = new VisualElement();
            otherScenarioNotice.AddToClassList("sf-debug-notice");
            otherScenarioLabel = new Label();
            otherScenarioNotice.Add(otherScenarioLabel);
            otherScenarioNotice.Add(new Button(() => OpenScenarioRequested?.Invoke(Runner?.Definition ?? session?.Behaviour.Definition)) { text = "Open Its Scenario" });

            Panel = new VisualElement { name = "debugPanel" };
            Panel.AddToClassList("sf-debug-panel");
            var tabs = new Toolbar();
            tabs.AddToClassList("sf-debug-panel__tabs");
            blackboardTab = new ToolbarToggle { text = "Blackboard", value = true };
            logTab = new ToolbarToggle { text = "Event Log" };
            blackboardTab.RegisterValueChangedCallback(evt => ShowTab(evt.newValue ? 0 : 1));
            logTab.RegisterValueChangedCallback(evt => ShowTab(evt.newValue ? 1 : 0));
            tabs.Add(blackboardTab);
            tabs.Add(logTab);
            tabs.Add(new ToolbarSpacer { style = { flexGrow = 1 } });
            var clearButton = new ToolbarButton(() => { log.Clear(); RefreshLog(); }) { text = "Clear", name = "clearLogButton", tooltip = "Clear the event log." };
            var copyButton = new ToolbarButton(CopyLog) { text = "Copy", name = "copyLogButton", tooltip = "Copy the event log to the clipboard." };
            tabs.Add(clearButton);
            tabs.Add(copyButton);
            Panel.Add(tabs);
            Panel.Add(otherScenarioNotice);

            blackboardView = new ListView(blackboardRows, 18, MakeBlackboardRow, BindBlackboardRow) { name = "blackboardView", selectionType = SelectionType.None };
            blackboardView.AddToClassList("sf-debug-panel__list");
            blackboardEmpty = new Label("The blackboard is empty.");
            blackboardEmpty.AddToClassList("sf-debug-panel__empty");
            logView = new ListView(logRows, 18, MakeLogRow, BindLogRow) { name = "logView", selectionType = SelectionType.None };
            logView.AddToClassList("sf-debug-panel__list");
            Panel.Add(blackboardEmpty);
            Panel.Add(blackboardView);
            Panel.Add(logView);
            ShowTab(0);
        }

        /// <summary>Raised when the user asks to open the scenario of a runner that plays another asset.</summary>
        public event Action<ScenarioDefinition> OpenScenarioRequested;

        /// <summary>The bar shown under the window toolbar.</summary>
        public VisualElement Bar { get; }

        /// <summary>The Blackboard / Event Log panel.</summary>
        public VisualElement Panel { get; }

        public DebugEventLog Log => log;

        public IReadOnlyList<ScenarioRunnerBehaviour> Runners => runners;

        public ScenarioRunnerBehaviour SelectedBehaviour => session?.Behaviour;

        public ScenarioRunner Runner => session?.Runner;

        /// <summary>Run state per step of the window's scenario; empty when the selected runner plays another asset.</summary>
        public IReadOnlyList<StepRunState> States => states;

        /// <summary>Index of the active step, or -1.</summary>
        public int ActiveIndex { get; private set; } = -1;

        /// <summary>Whether the selected runner plays the scenario open in the window.</summary>
        public bool ShowsWindowScenario { get; private set; }

        internal int BlackboardRowCount => blackboardRows.Count;

        /// <summary>Clears everything and stops watching the runner.</summary>
        public void Stop()
        {
            session?.Dispose();
            session = null;
            runners.Clear();
            states.Clear();
            ActiveIndex = -1;
            userPickedRunner = false;
            ticksUntilRunnerRefresh = 0;
            stateLabel.text = string.Empty;
            otherScenarioNotice.AddToClassList("sf-hidden");
            log.Clear();
            shownLogVersion = -1;
            blackboardRows.Clear();
            blackboardView.RefreshItems();
            logRows.Clear();
            logView.RefreshItems();
        }

        /// <summary>Refreshes the runner list, the controls, the step states and the visible tab.</summary>
        public void Refresh(ScenarioDefinition scenario)
        {
            if (scenario != windowScenario)
            {
                windowScenario = scenario;
                userPickedRunner = false;
                ticksUntilRunnerRefresh = 0;
            }

            if (--ticksUntilRunnerRefresh <= 0 || (session != null && session.Behaviour == null))
            {
                ticksUntilRunnerRefresh = RunnerListRefreshTicks;
                RefreshRunners();
            }

            session?.Poll();
            RefreshControls();
            RefreshStates();
            if (blackboardTab.value)
                RefreshBlackboard();
            else
                RefreshLog();
        }

        /// <summary>Selects a runner from <see cref="Runners"/> as if the user picked it.</summary>
        internal void SelectRunner(int index)
        {
            runnerField.index = index;
            OnRunnerPicked(index);
        }

        void RefreshRunners()
        {
            var found = new List<ScenarioRunnerBehaviour>(UnityEngine.Object.FindObjectsByType<ScenarioRunnerBehaviour>(FindObjectsInactive.Exclude));
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            bool changed = found.Count != runners.Count;
            for (int i = 0; !changed && i < found.Count; i++)
                changed = found[i] != runners[i];

            if (changed)
            {
                runners.Clear();
                runners.AddRange(found);
                var labels = new List<string>();
                foreach (ScenarioRunnerBehaviour runner in runners)
                    labels.Add($"{runner.name} — {(runner.Definition != null ? runner.Definition.name : "(no scenario)")}");
                runnerField.choices = labels;
            }

            ScenarioRunnerBehaviour wanted = session?.Behaviour;
            if (wanted == null || !runners.Contains(wanted) || !userPickedRunner)
                wanted = PickDefault(wanted);

            if (wanted != session?.Behaviour)
                Attach(wanted);

            int index = wanted != null ? runners.IndexOf(wanted) : -1;
            if (index >= 0)
                runnerField.SetValueWithoutNotify(runnerField.choices[index]);
            else
                runnerField.SetValueWithoutNotify(runners.Count == 0 ? "No ScenarioRunnerBehaviour in the open scenes" : string.Empty);
        }

        ScenarioRunnerBehaviour PickDefault(ScenarioRunnerBehaviour current)
        {
            foreach (ScenarioRunnerBehaviour runner in runners)
            {
                if (windowScenario != null && runner.Definition == windowScenario)
                    return runner;
            }

            if (current != null && runners.Contains(current))
                return current;
            return runners.Count > 0 ? runners[0] : null;
        }

        void OnRunnerPicked(int index)
        {
            if (index < 0 || index >= runners.Count)
                return;
            userPickedRunner = true;
            if (runners[index] != session?.Behaviour)
                Attach(runners[index]);
            RefreshControls();
            RefreshStates();
        }

        void Attach(ScenarioRunnerBehaviour behaviour)
        {
            session?.Dispose();
            session = behaviour != null ? new RunnerDebugSession(behaviour, log) : null;
        }

        void RefreshControls()
        {
            ScenarioRunner runner = Runner;
            RunnerState state = runner?.State ?? RunnerState.NotStarted;
            bool active = state == RunnerState.Running || state == RunnerState.Paused;
            stateLabel.text = runner == null ? (session == null ? string.Empty : "Not started") : $"{state}  ·  {runner.Context.TotalElapsed:0.0}s  ·  {runner.Mistakes.Count} mistakes";
            pauseButton.text = state == RunnerState.Paused ? "Resume" : "Pause";
            pauseButton.SetEnabled(active);
            skipButton.SetEnabled(active && runner.CurrentStepIndex >= 0);
            restartButton.SetEnabled(runner != null);
            abortButton.SetEnabled(active);

            ScenarioDefinition playing = runner?.Definition ?? session?.Behaviour?.Definition;
            ShowsWindowScenario = session != null && playing != null && playing == windowScenario;
            bool other = session != null && playing != null && !ShowsWindowScenario;
            otherScenarioNotice.EnableInClassList("sf-hidden", !other);
            if (other)
                otherScenarioLabel.text = $"This runner plays \"{playing.name}\", not the scenario open in this window.";
        }

        void RefreshStates()
        {
            if (!ShowsWindowScenario)
            {
                states.Clear();
                ActiveIndex = -1;
                return;
            }

            StepRunStates.Compute(Runner, windowScenario.Steps.Count, states);
            ActiveIndex = states.IndexOf(StepRunState.Active);
        }

        void TogglePause()
        {
            ScenarioRunner runner = Runner;
            if (runner == null)
                return;
            if (runner.State == RunnerState.Paused)
                runner.Resume();
            else
                runner.Pause();
            RefreshControls();
        }

        void CopyLog()
        {
            EditorGUIUtility.systemCopyBuffer = log.ToText();
        }

        void ShowTab(int tab)
        {
            blackboardTab.SetValueWithoutNotify(tab == 0);
            logTab.SetValueWithoutNotify(tab == 1);
            blackboardView.EnableInClassList("sf-hidden", tab != 0);
            blackboardEmpty.EnableInClassList("sf-hidden", tab != 0 || blackboardRows.Count > 0);
            logView.EnableInClassList("sf-hidden", tab != 1);
            if (tab == 0) RefreshBlackboard();
            else RefreshLog();
        }

        void RefreshBlackboard()
        {
            blackboardRows.Clear();
            Blackboard blackboard = Runner?.Context?.Blackboard;
            if (blackboard != null)
            {
                foreach (string key in blackboard.Keys)
                {
                    blackboard.TryGetValue(key, out BlackboardValue value);
                    blackboardRows.Add(new BlackboardRow(key, value.Type.ToString(), value.ToString()));
                }
                blackboardRows.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            }

            blackboardView.RefreshItems();
            blackboardEmpty.EnableInClassList("sf-hidden", !blackboardTab.value || blackboardRows.Count > 0);
        }

        void RefreshLog()
        {
            if (shownLogVersion == log.Version)
                return;

            shownLogVersion = log.Version;
            logRows.Clear();
            logRows.AddRange(log.Entries);
            logView.RefreshItems();
            if (logRows.Count > 0)
                logView.ScrollToItem(logRows.Count - 1);
        }

        Button BarButton(string name, string text, string tooltip, Action action)
        {
            var button = new Button(action) { name = name, text = text, tooltip = tooltip };
            button.AddToClassList("sf-debug-bar__button");
            Bar.Add(button);
            return button;
        }

        static VisualElement MakeBlackboardRow()
        {
            var row = new VisualElement();
            row.AddToClassList("sf-debug-row");
            foreach (string part in new[] { "key", "type", "value" })
            {
                var label = new Label();
                label.AddToClassList("sf-debug-row__" + part);
                row.Add(label);
            }
            return row;
        }

        void BindBlackboardRow(VisualElement element, int index)
        {
            BlackboardRow row = blackboardRows[index];
            element.Q<Label>(className: "sf-debug-row__key").text = row.Key;
            element.Q<Label>(className: "sf-debug-row__type").text = row.Type;
            element.Q<Label>(className: "sf-debug-row__value").text = row.Value;
        }

        static VisualElement MakeLogRow()
        {
            var row = new VisualElement();
            row.AddToClassList("sf-debug-row");
            foreach (string part in new[] { "time", "kind", "message" })
            {
                var label = new Label();
                label.AddToClassList("sf-debug-row__" + part);
                row.Add(label);
            }
            return row;
        }

        void BindLogRow(VisualElement element, int index)
        {
            DebugEvent entry = logRows[index];
            element.Q<Label>(className: "sf-debug-row__time").text = entry.Time.ToString("0.00") + "s";
            var kind = element.Q<Label>(className: "sf-debug-row__kind");
            kind.text = entry.Kind.ToString();
            kind.EnableInClassList("sf-debug-row__kind--mistake", entry.Kind == DebugEventKind.Mistake);
            element.Q<Label>(className: "sf-debug-row__message").text = entry.Message;
            element.tooltip = entry.Message;
        }

        readonly struct BlackboardRow
        {
            public readonly string Key;
            public readonly string Type;
            public readonly string Value;

            public BlackboardRow(string key, string type, string value)
            {
                Key = key;
                Type = type;
                Value = value;
            }
        }
    }
}
