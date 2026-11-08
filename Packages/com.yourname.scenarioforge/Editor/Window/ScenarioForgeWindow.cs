using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// The Scenario Forge editor window: edit the steps of a <see cref="ScenarioDefinition"/>.
    /// </summary>
    public sealed class ScenarioForgeWindow : EditorWindow
    {
        const string WindowTitle = "Scenario Forge";
        const string LastScenarioPrefKeyPrefix = "ScenarioForge.LastScenarioGuid.";

        [SerializeField] ScenarioDefinition scenario;
        [SerializeField] int selectedIndex = -1;

        readonly AdvancedDropdownState addStepState = new();
        readonly AdvancedDropdownState changeTypeState = new();

        SerializedObject serializedScenario;
        ObjectField scenarioField;
        ToolbarButton addStepButton;
        ToolbarButton importButton;
        ToolbarButton exportButton;
        ToolbarSearchField searchField;
        VisualElement emptyState;
        VisualElement editorArea;
        StepListPanel listPanel;
        StepDetailsPanel detailsPanel;
        Label statusLabel;
        VisualElement changeTracker;
        ValidationPanel validationPanel;
        ValidationReport lastReport;
        IVisualElementScheduledItem pendingValidation;

        /// <summary>The scenario being edited, or <c>null</c> for the empty state.</summary>
        public ScenarioDefinition Scenario => scenario;

        /// <summary>Index of the selected step, or <c>-1</c>.</summary>
        public int SelectedIndex => selectedIndex;

        internal SerializedObject SerializedScenario => serializedScenario;

        internal StepListPanel ListPanel => listPanel;

        internal StepDetailsPanel DetailsPanel => detailsPanel;

        internal string StatusMessage => statusLabel?.text;

        internal ValidationPanel ValidationPanel => validationPanel;

        /// <summary>The result of the last validation, or <c>null</c> when no scenario is open.</summary>
        internal ValidationReport LastReport => lastReport;

        static string LastScenarioPrefKey => LastScenarioPrefKeyPrefix + PlayerSettings.productGUID;

        /// <summary>Opens the window (menu Window/Scenario Forge).</summary>
        [MenuItem("Window/Scenario Forge")]
        public static ScenarioForgeWindow Open()
        {
            var window = GetWindow<ScenarioForgeWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(560f, 320f);
            window.Show();
            return window;
        }

        /// <summary>Opens the window and edits <paramref name="definition"/>.</summary>
        public static ScenarioForgeWindow Open(ScenarioDefinition definition)
        {
            ScenarioForgeWindow window = Open();
            window.SetScenario(definition);
            window.Focus();
            return window;
        }

        [OnOpenAsset]
        static bool OnOpenAsset(EntityId entityId, int line)
        {
            if (EditorUtility.EntityIdToObject(entityId) is not ScenarioDefinition definition)
                return false;

            Open(definition);
            return true;
        }

        /// <summary>Edits <paramref name="definition"/>, or shows the empty state when it is <c>null</c>.</summary>
        public void SetScenario(ScenarioDefinition definition)
        {
            bool changed = definition != scenario;
            scenario = definition;
            serializedScenario = definition != null ? new SerializedObject(definition) : null;
            if (changed)
                selectedIndex = -1;

            RememberScenario(definition);
            if (rootVisualElement.childCount == 0)
                return;

            scenarioField.SetValueWithoutNotify(definition);
            RebuildAll();
        }

        /// <summary>Selects the step at <paramref name="index"/> and rebuilds the details panel.</summary>
        public void SelectStep(int index)
        {
            int count = serializedScenario != null ? ScenarioEditOperations.GetStepCount(serializedScenario) : 0;
            selectedIndex = index >= 0 && index < count ? index : -1;
            listPanel?.Select(selectedIndex);
            detailsPanel?.Show(serializedScenario, selectedIndex);
        }

        /// <summary>Adds a step of <paramref name="type"/> after the selected one (or at the end) and selects it.</summary>
        public void AddStep(Type type) => AddStepAt(type, selectedIndex >= 0 ? selectedIndex + 1 : int.MaxValue);

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.hierarchyChanged += ScheduleValidation;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.hierarchyChanged -= ScheduleValidation;
        }

        void CreateGUI()
        {
            titleContent = new GUIContent(WindowTitle);
            VisualElement root = rootVisualElement;
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackagePaths.StyleSheet);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            root.AddToClassList("sf-root");

            root.Add(BuildToolbar());

            emptyState = BuildEmptyState();
            root.Add(emptyState);

            editorArea = new VisualElement();
            editorArea.AddToClassList("sf-editor-area");
            var split = new TwoPaneSplitView(0, 300f, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "scenario-forge-split" };
            split.AddToClassList("sf-split");
            listPanel = new StepListPanel();
            detailsPanel = new StepDetailsPanel();
            split.Add(listPanel);
            split.Add(detailsPanel);
            editorArea.Add(split);
            validationPanel = new ValidationPanel();
            validationPanel.IssueClicked += OnIssueClicked;
            editorArea.Add(validationPanel);
            root.Add(editorArea);

            statusLabel = new Label();
            statusLabel.AddToClassList("sf-status");
            root.Add(statusLabel);

            listPanel.StepSelected += OnStepSelectedInList;
            listPanel.StepMoved += OnStepMoved;
            listPanel.CommandRequested += OnListCommand;
            listPanel.PopulateContextMenu = PopulateRowMenu;
            listPanel.DecorateRow = DecorateRow;
            detailsPanel.CommandRequested += OnDetailsCommand;
            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

            if (scenario == null)
                scenario = LoadRememberedScenario();

            serializedScenario = scenario != null ? new SerializedObject(scenario) : null;
            scenarioField.SetValueWithoutNotify(scenario);
            RebuildAll();
        }

        VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("sf-toolbar");

            scenarioField = new ObjectField { objectType = typeof(ScenarioDefinition), allowSceneObjects = false, tooltip = "The scenario being edited." };
            scenarioField.AddToClassList("sf-toolbar__scenario");
            scenarioField.RegisterValueChangedCallback(evt => SetScenario(evt.newValue as ScenarioDefinition));
            toolbar.Add(scenarioField);

            addStepButton = new ToolbarButton(ShowAddStepMenu) { text = "+ Add Step", tooltip = "Add a step after the selected one." };
            toolbar.Add(addStepButton);

            var validateButton = new ToolbarButton(ValidateNow) { text = "Validate", tooltip = "Check the scenario for problems." };
            validateButton.name = "validateButton";
            toolbar.Add(validateButton);

            importButton = new ToolbarButton(ImportFromFile) { text = "Import", tooltip = "Import steps from a JSON file (replace or append)." };
            importButton.name = "importButton";
            toolbar.Add(importButton);

            exportButton = new ToolbarButton(ExportToFile) { text = "Export", tooltip = "Export this scenario to a JSON file." };
            exportButton.name = "exportButton";
            toolbar.Add(exportButton);

            var spacer = new ToolbarSpacer();
            spacer.AddToClassList("sf-toolbar__spacer");
            toolbar.Add(spacer);

            searchField = new ToolbarSearchField { tooltip = "Filter steps by title or type." };
            searchField.AddToClassList("sf-toolbar__search");
            searchField.RegisterValueChangedCallback(evt => listPanel.SetFilter(evt.newValue));
            toolbar.Add(searchField);
            return toolbar;
        }

        VisualElement BuildEmptyState()
        {
            var container = new VisualElement();
            container.AddToClassList("sf-empty");
            var title = new Label("No scenario selected");
            title.AddToClassList("sf-empty__title");
            var hint = new Label("Create a new scenario or open an existing one to start adding steps.");
            hint.AddToClassList("sf-empty__hint");
            var buttons = new VisualElement();
            buttons.AddToClassList("sf-empty__buttons");
            buttons.Add(new Button(CreateNewScenario) { text = "Create New Scenario" });
            buttons.Add(new Button(OpenExistingScenario) { text = "Open Existing…" });
            container.Add(title);
            container.Add(hint);
            container.Add(buttons);
            return container;
        }

        void RebuildAll()
        {
            if (editorArea == null)
                return;

            bool hasScenario = scenario != null;
            emptyState.EnableInClassList("sf-hidden", hasScenario);
            editorArea.EnableInClassList("sf-hidden", !hasScenario);
            addStepButton.SetEnabled(hasScenario);
            importButton.SetEnabled(hasScenario);
            exportButton.SetEnabled(hasScenario);
            searchField.SetEnabled(hasScenario);

            TrackChanges();
            RunValidation();
            listPanel.SetScenario(serializedScenario);
            SelectStep(selectedIndex);
        }

        /// <summary>Validates the scenario now and refreshes the validation panel and row icons.</summary>
        internal void RunValidation()
        {
            pendingValidation?.Pause();
            pendingValidation = null;
            lastReport = scenario != null ? EditorScenarioValidation.Validate(scenario, checkSceneTargets: true) : null;
            validationPanel?.SetReport(lastReport, scenario);
            listPanel?.ForEachRow(DecorateRow);
        }

        void ValidateNow()
        {
            RunValidation();
            if (lastReport != null)
                ShowStatus("Validation: " + EditorScenarioValidation.Summarize(lastReport) + ".", lastReport.HasErrors);
        }

        void ScheduleValidation()
        {
            if (scenario == null || rootVisualElement.panel == null)
                return;

            if (pendingValidation != null)
            {
                pendingValidation.ExecuteLater(250);
                return;
            }

            pendingValidation = rootVisualElement.schedule.Execute(RunValidation).StartingIn(250);
        }

        void DecorateRow(StepRow row)
        {
            row.Issues.Clear();
            ValidationSeverity? worst = lastReport?.GetWorstSeverity(row.StepIndex);
            if (!worst.HasValue)
                return;

            Image icon = ValidationPanel.Icon(ValidationPanel.SeverityIcon(worst.Value));
            icon.AddToClassList("sf-row__issue");
            icon.tooltip = string.Join("\n", System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(lastReport.Issues, issue => issue.StepIndex == row.StepIndex),
                issue => issue.Message));
            row.Issues.Add(icon);
        }

        void OnIssueClicked(int index)
        {
            if (listPanel.IsFiltering)
            {
                searchField.value = string.Empty;
                listPanel.SetFilter(string.Empty);
            }

            SelectStep(index);
            listPanel.FocusList();
        }

        void TrackChanges()
        {
            changeTracker?.RemoveFromHierarchy();
            changeTracker = null;
            if (serializedScenario == null)
                return;

            changeTracker = new VisualElement();
            changeTracker.AddToClassList("sf-hidden");
            rootVisualElement.Add(changeTracker);
            changeTracker.TrackSerializedObjectValue(serializedScenario, _ =>
            {
                listPanel.RefreshRows();
                ScheduleValidation();
            });
        }

        void AddStepAt(Type type, int index)
        {
            if (serializedScenario == null)
                return;

            int newIndex = ScenarioEditOperations.AddStep(serializedScenario, type, index);
            AfterStructureChange(newIndex);
            ShowStatus($"Added {StepTypeCatalog.Describe(type).DisplayName} step.");
        }

        void AfterStructureChange(int indexToSelect)
        {
            RunValidation();
            listPanel.Rebuild();
            SelectStep(indexToSelect);
        }

        void ShowAddStepMenu() => ShowTypePicker(addStepButton.worldBound, int.MinValue);

        /// <summary>Opens the type picker. <paramref name="insertIndex"/> of <c>int.MinValue</c> means "after the selection".</summary>
        void ShowTypePicker(Rect anchor, int insertIndex)
        {
            if (serializedScenario == null)
                return;

            new StepTypeDropdown(addStepState, "Add Step", type =>
            {
                if (insertIndex == int.MinValue)
                    AddStep(type);
                else
                    AddStepAt(type, insertIndex);
            }).Show(anchor);
        }

        void OnStepSelectedInList(int index)
        {
            if (index == selectedIndex && detailsPanel.HasStep)
                return;

            selectedIndex = index;
            detailsPanel.Show(serializedScenario, selectedIndex);
        }

        void OnStepMoved(int from, int to)
        {
            serializedScenario.Update();
            selectedIndex = to;
            listPanel.RefreshRows();
            SelectStep(to);
            ShowStatus($"Moved step {from + 1} to position {to + 1}.");
        }

        void OnListCommand(StepListCommand command)
        {
            switch (command)
            {
                case StepListCommand.Delete:
                    DeleteSelected();
                    break;
                case StepListCommand.Duplicate:
                    DuplicateSelected();
                    break;
                case StepListCommand.MoveUp:
                    MoveSelected(-1);
                    break;
                case StepListCommand.MoveDown:
                    MoveSelected(1);
                    break;
                case StepListCommand.Copy:
                    CopySelected();
                    break;
                case StepListCommand.Paste:
                    PasteAfterSelected();
                    break;
            }
        }

        void OnDetailsCommand(StepDetailsCommand command)
        {
            switch (command)
            {
                case StepDetailsCommand.ChangeType:
                    ShowChangeTypePicker();
                    break;
                case StepDetailsCommand.Duplicate:
                    DuplicateSelected();
                    break;
                case StepDetailsCommand.Delete:
                case StepDetailsCommand.RemoveMissing:
                    DeleteSelected();
                    break;
                case StepDetailsCommand.ResetToDefaults:
                    ResetSelected();
                    break;
                case StepDetailsCommand.Copy:
                    OnListCommand(StepListCommand.Copy);
                    break;
            }
        }

        void PopulateRowMenu(DropdownMenu menu, int index, Rect rowRect)
        {
            selectedIndex = index;
            bool filtering = listPanel.IsFiltering;
            int count = ScenarioEditOperations.GetStepCount(serializedScenario);
            bool hasStep = ScenarioEditOperations.GetStep(serializedScenario, index) != null;

            menu.AppendAction("Insert Step Above…", _ => ShowTypePicker(rowRect, index));
            menu.AppendAction("Insert Step Below…", _ => ShowTypePicker(rowRect, index + 1));
            menu.AppendSeparator();
            menu.AppendAction("Duplicate\tCtrl/Cmd+D", _ => DuplicateSelected(), hasStep ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("Copy\tCtrl/Cmd+C", _ => OnListCommand(StepListCommand.Copy), hasStep ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("Paste\tCtrl/Cmd+V", _ => OnListCommand(StepListCommand.Paste));
            menu.AppendSeparator();
            menu.AppendAction("Move Up\tAlt+↑", _ => MoveSelected(-1), !filtering && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction("Move Down\tAlt+↓", _ => MoveSelected(1), !filtering && index < count - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendSeparator();
            menu.AppendAction("Delete\tDelete", _ => DeleteSelected());
        }

        void DeleteSelected()
        {
            if (serializedScenario == null || selectedIndex < 0)
                return;

            int deleted = selectedIndex;
            bool missing = ScenarioEditOperations.GetStep(serializedScenario, selectedIndex) == null;
            if (missing && !EditorUtility.DisplayDialog(
                    "Remove missing step",
                    "Remove this step and its stored data? Its class does not exist, so this cannot be undone.",
                    "Remove",
                    "Cancel"))
                return;

            int next = missing
                ? ScenarioEditOperations.RemoveMissingStep(serializedScenario, selectedIndex)
                : ScenarioEditOperations.DeleteStep(serializedScenario, selectedIndex);
            AfterStructureChange(next);
            ShowStatus($"Deleted step {deleted + 1}.");
            listPanel.FocusList();
        }

        void DuplicateSelected()
        {
            if (serializedScenario == null || selectedIndex < 0)
                return;

            int copy = ScenarioEditOperations.DuplicateStep(serializedScenario, selectedIndex);
            if (copy < 0)
            {
                ShowStatus("A step with a missing type cannot be duplicated.", true);
                return;
            }

            AfterStructureChange(copy);
            ShowStatus($"Duplicated step {selectedIndex + 1} as step {copy + 1}.");
        }

        /// <summary>Copies the selected step to the system clipboard as Scenario Forge JSON.</summary>
        internal bool CopySelected()
        {
            if (serializedScenario == null || selectedIndex < 0)
                return false;

            serializedScenario.Update();
            ScenarioStep step = ScenarioEditOperations.GetStep(serializedScenario, selectedIndex);
            if (step == null)
            {
                ShowStatus("A step with a missing type cannot be copied.", true);
                return false;
            }

            EditorGUIUtility.systemCopyBuffer = ScenarioJson.ExportSteps(string.Empty, string.Empty, new[] { step });
            ShowStatus($"Copied step {selectedIndex + 1}.");
            return true;
        }

        /// <summary>
        /// Pastes the steps on the system clipboard after the selected step (or at the end), with new ids.
        /// Invalid clipboard content shows an error in the status bar.
        /// </summary>
        internal bool PasteAfterSelected()
        {
            if (serializedScenario == null)
                return false;

            if (!ScenarioJson.TryImport(EditorGUIUtility.systemCopyBuffer, out ScenarioData data, out var errors))
            {
                ShowStatus("Nothing to paste: the clipboard does not hold Scenario Forge steps. " + errors[0], true);
                return false;
            }

            if (data.Steps.Count == 0)
            {
                ShowStatus("Nothing to paste: the clipboard holds a scenario with no steps.", true);
                return false;
            }

            int count = ScenarioEditOperations.GetStepCount(serializedScenario);
            int index = selectedIndex >= 0 ? Mathf.Min(selectedIndex + 1, count) : count;
            ScenarioEditOperations.InsertSteps(serializedScenario, data.Steps, index, alwaysNewIds: true, ScenarioEditOperations.PasteStepUndo);
            AfterStructureChange(index);
            ShowStatus(data.Steps.Count == 1 ? $"Pasted step {index + 1}." : $"Pasted {data.Steps.Count} steps at {index + 1}.");
            return true;
        }

        /// <summary>How imported steps are merged into the scenario.</summary>
        internal enum ImportMode
        {
            Replace,
            Append
        }

        /// <summary>
        /// Imports JSON into the scenario as one Undo group. Repeated ids get new ids and the status bar says how many.
        /// </summary>
        internal bool ImportJson(string json, ImportMode mode, string sourceName = "the JSON")
        {
            if (serializedScenario == null)
                return false;

            if (!ScenarioJson.TryImport(json, out ScenarioData data, out var errors))
            {
                ShowStatus($"Import failed: {errors[0]}", true);
                return false;
            }

            int regenerated;
            int selectAfter;
            if (mode == ImportMode.Replace)
            {
                regenerated = ScenarioEditOperations.ReplaceContent(serializedScenario, data.Title, data.Description, data.Steps);
                selectAfter = data.Steps.Count > 0 ? 0 : -1;
            }
            else
            {
                int count = ScenarioEditOperations.GetStepCount(serializedScenario);
                regenerated = ScenarioEditOperations.InsertSteps(serializedScenario, data.Steps, count, alwaysNewIds: false, ScenarioEditOperations.ImportAppendUndo);
                selectAfter = data.Steps.Count > 0 ? count : selectedIndex;
            }

            AfterStructureChange(selectAfter);
            string idNote = regenerated == 0 ? string.Empty : $" {regenerated} step {(regenerated == 1 ? "id was" : "ids were")} already in use and got a new id.";
            ShowStatus($"Imported {data.Steps.Count} {(data.Steps.Count == 1 ? "step" : "steps")} from {sourceName} ({(mode == ImportMode.Replace ? "replaced" : "appended")}).{idNote}");
            return true;
        }

        void ImportFromFile()
        {
            if (serializedScenario == null)
                return;

            string path = EditorUtility.OpenFilePanel("Import Scenario", string.Empty, "json");
            if (string.IsNullOrEmpty(path))
                return;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                ShowStatus($"Could not read {Path.GetFileName(path)}: {exception.Message}", true);
                return;
            }

            if (!ScenarioJson.TryImport(json, out ScenarioData data, out var errors))
            {
                string shown = string.Join("\n", errors.Count > 5 ? new List<string>(errors).GetRange(0, 5) : errors);
                EditorUtility.DisplayDialog("Import Failed", $"{Path.GetFileName(path)} could not be imported:\n\n{shown}", "OK");
                ShowStatus($"Import failed: {errors[0]}", true);
                return;
            }

            var mode = ImportMode.Replace;
            if (ScenarioEditOperations.GetStepCount(serializedScenario) > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex("Import Scenario",
                    $"Import {data.Steps.Count} steps from {Path.GetFileName(path)}.\n\nReplace the current title, description and steps, or append the steps at the end?",
                    "Replace", "Cancel", "Append");
                if (choice == 1)
                    return;
                mode = choice == 0 ? ImportMode.Replace : ImportMode.Append;
            }

            ImportJson(json, mode, Path.GetFileName(path));
        }

        void ExportToFile()
        {
            if (scenario == null)
                return;

            string json;
            try
            {
                json = ScenarioJson.Export(scenario);
            }
            catch (InvalidOperationException exception)
            {
                ShowStatus(exception.Message, true);
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Scenario", string.Empty, scenario.name, "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, json);
                ShowStatus($"Exported {scenario.Steps.Count} steps to {Path.GetFileName(path)}.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                ShowStatus($"Could not write {Path.GetFileName(path)}: {exception.Message}", true);
            }
        }

        void MoveSelected(int offset)
        {
            if (serializedScenario == null || selectedIndex < 0)
                return;

            if (listPanel.IsFiltering)
            {
                ShowStatus("Clear the search to reorder steps.", true);
                return;
            }

            int moved = ScenarioEditOperations.MoveStep(serializedScenario, selectedIndex, selectedIndex + offset);
            AfterStructureChange(moved);
        }

        void ResetSelected()
        {
            if (serializedScenario == null || selectedIndex < 0)
                return;

            if (!EditorUtility.DisplayDialog("Reset step", "Reset every field of this step to its default value? The step keeps its id.", "Reset", "Cancel"))
                return;

            ScenarioEditOperations.ResetStep(serializedScenario, selectedIndex);
            AfterStructureChange(selectedIndex);
            ShowStatus("Step reset to defaults.");
        }

        void ShowChangeTypePicker()
        {
            if (serializedScenario == null || selectedIndex < 0)
                return;

            int index = selectedIndex;
            Rect anchor = detailsPanel.MenuButtonRect;
            new StepTypeDropdown(changeTypeState, "Change Type", type => ChangeType(index, type)).Show(anchor);
        }

        void ChangeType(int index, Type newType)
        {
            ScenarioStep current = ScenarioEditOperations.GetStep(serializedScenario, index);
            if (current == null || current.GetType() == newType)
                return;

            string from = StepTypeCatalog.Describe(current.GetType()).DisplayName;
            string to = StepTypeCatalog.Describe(newType).DisplayName;
            bool confirmed = EditorUtility.DisplayDialog(
                "Change step type",
                $"Change this step from {from} to {to}?\n\nTitle, instruction, timeout and failure settings are kept. Fields specific to {from} are lost.",
                "Change Type",
                "Cancel");
            if (!confirmed)
                return;

            ScenarioEditOperations.ChangeStepType(serializedScenario, index, newType);
            AfterStructureChange(index);
            ShowStatus($"Changed step {index + 1} to {to}.");
        }

        void OnUndoRedo()
        {
            if (serializedScenario == null)
                return;

            if (scenario == null)
            {
                SetScenario(null);
                return;
            }

            serializedScenario.Update();
            RunValidation();
            listPanel?.Rebuild();
            SelectStep(Mathf.Min(selectedIndex, ScenarioEditOperations.GetStepCount(serializedScenario) - 1));
        }

        void OnProjectChanged()
        {
            if (serializedScenario != null && scenario == null)
            {
                SetScenario(null);
                ShowStatus("The scenario asset was deleted.");
                return;
            }

            scenarioField?.SetValueWithoutNotify(null);
            scenarioField?.SetValueWithoutNotify(scenario);
        }

        void OnRootKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey && evt.keyCode == KeyCode.S && scenario != null)
            {
                AssetDatabase.SaveAssetIfDirty(scenario);
                ShowStatus("Saved " + scenario.name + ".");
                evt.StopPropagation();
            }
        }

        void CreateNewScenario()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Scenario", "NewScenario", "asset", "Choose where to save the new scenario.");
            if (string.IsNullOrEmpty(path))
                return;

            var definition = CreateInstance<ScenarioDefinition>();
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            SetScenario(definition);
            ShowStatus("Created " + path + ".");
        }

        void OpenExistingScenario()
        {
            string absolute = EditorUtility.OpenFilePanel("Open Scenario", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(absolute))
                return;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/') + "/";
            string normalized = Path.GetFullPath(absolute).Replace('\\', '/');
            if (!normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                ShowStatus("Pick a scenario inside this project.", true);
                return;
            }

            var definition = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(normalized.Substring(projectRoot.Length));
            if (definition == null)
            {
                ShowStatus("That file is not a Scenario Forge scenario.", true);
                return;
            }

            SetScenario(definition);
        }

        void ShowStatus(string message, bool isError = false)
        {
            if (statusLabel == null)
                return;

            statusLabel.text = message;
            statusLabel.EnableInClassList("sf-status--error", isError);
        }

        static void RememberScenario(ScenarioDefinition definition)
        {
            if (definition == null)
            {
                EditorPrefs.DeleteKey(LastScenarioPrefKey);
                return;
            }

            string path = AssetDatabase.GetAssetPath(definition);
            if (!string.IsNullOrEmpty(path))
                EditorPrefs.SetString(LastScenarioPrefKey, AssetDatabase.AssetPathToGUID(path));
        }

        static ScenarioDefinition LoadRememberedScenario()
        {
            string guid = EditorPrefs.GetString(LastScenarioPrefKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
        }
    }
}
