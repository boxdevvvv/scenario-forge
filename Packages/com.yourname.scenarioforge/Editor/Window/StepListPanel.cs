using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>Commands the step list forwards to the window.</summary>
    enum StepListCommand
    {
        Delete,
        Duplicate,
        Copy,
        Paste,
        MoveUp,
        MoveDown
    }

    /// <summary>
    /// The left panel: a reorderable <see cref="ListView"/> bound to the steps array, with search filtering,
    /// a context menu and keyboard shortcuts.
    /// </summary>
    sealed class StepListPanel : VisualElement
    {
        readonly Label header = new();
        readonly Label filterNotice = new("Filtered: drag reordering is disabled until the search is cleared.");
        readonly ListView listView = new();
        readonly List<int> filteredIndices = new();

        SerializedObject scenario;
        string filter = string.Empty;
        double lastDeleteTime;

        public StepListPanel()
        {
            AddToClassList("sf-list-panel");
            header.AddToClassList("sf-section-header");
            filterNotice.AddToClassList("sf-filter-notice");
            listView.AddToClassList("sf-step-list");

            listView.fixedItemHeight = 24f;
            listView.selectionType = SelectionType.Single;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showBoundCollectionSize = false;
            listView.showAddRemoveFooter = false;
            listView.showBorder = false;
            listView.makeItem = MakeRow;
            listView.bindItem = BindRow;
            listView.selectedIndicesChanged += OnSelectedIndicesChanged;
            listView.itemIndexChanged += OnItemIndexChanged;
            listView.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            listView.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            listView.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            Add(header);
            Add(filterNotice);
            Add(listView);
            UpdateHeader();
        }

        /// <summary>Raised with the scenario index when the user selects a step.</summary>
        public event Action<int> StepSelected;

        /// <summary>Raised after the user dragged a step from one index to another.</summary>
        public event Action<int, int> StepMoved;

        /// <summary>Raised for keyboard shortcuts.</summary>
        public event Action<StepListCommand> CommandRequested;

        /// <summary>Fills the context menu of the row showing the given scenario index.</summary>
        public Action<DropdownMenu, int, Rect> PopulateContextMenu { get; set; }

        /// <summary>Gives each bound row a chance to add validation or runtime state.</summary>
        public Action<StepRow> DecorateRow { get; set; }

        public bool IsFiltering => filter.Length > 0;

        /// <summary>The list control, exposed for focus handling and tests.</summary>
        public ListView ListView => listView;

        public void SetScenario(SerializedObject serializedScenario)
        {
            scenario = serializedScenario;
            Rebuild();
        }

        public void SetFilter(string text)
        {
            filter = text?.Trim() ?? string.Empty;
            Rebuild();
        }

        /// <summary>Re-binds the list after its size or order changed.</summary>
        public void Rebuild()
        {
            listView.Unbind();
            listView.bindingPath = null;
            filterNotice.EnableInClassList("sf-hidden", !IsFiltering);

            if (scenario == null || scenario.targetObject == null)
            {
                listView.itemsSource = null;
                UpdateHeader();
                return;
            }

            scenario.Update();
            if (IsFiltering)
            {
                RebuildFilter();
                listView.reorderable = false;
                listView.itemsSource = filteredIndices;
                listView.Rebuild();
            }
            else
            {
                listView.reorderable = true;
                listView.BindProperty(ScenarioEditOperations.GetSteps(scenario));
            }

            UpdateHeader();
        }

        /// <summary>Redraws the visible rows without rebinding (for text changes).</summary>
        public void RefreshRows()
        {
            if (IsFiltering)
            {
                Rebuild();
                return;
            }

            listView.RefreshItems();
            UpdateHeader();
        }

        /// <summary>Selects the row that shows <paramref name="stepIndex"/> without raising <see cref="StepSelected"/>.</summary>
        public void Select(int stepIndex)
        {
            int row = IsFiltering ? filteredIndices.IndexOf(stepIndex) : stepIndex;
            listView.schedule.Execute(() =>
            {
                if (row < 0 || listView.itemsSource == null || row >= listView.itemsSource.Count)
                {
                    listView.SetSelectionWithoutNotify(Array.Empty<int>());
                    return;
                }

                listView.SetSelectionWithoutNotify(new[] { row });
                listView.ScrollToItem(row);
            });
        }

        public void FocusList() => listView.Focus();

        /// <summary>Visits every bound row, for example to refresh validation icons.</summary>
        public void ForEachRow(Action<StepRow> visitor) => listView.Query<StepRow>().ForEach(visitor);

        VisualElement MakeRow()
        {
            var row = new StepRow();
            row.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (row.StepIndex < 0)
                    return;

                StepSelected?.Invoke(row.StepIndex);
                Select(row.StepIndex);
                PopulateContextMenu?.Invoke(evt.menu, row.StepIndex, row.worldBound);
            }));
            return row;
        }

        void BindRow(VisualElement element, int rowIndex)
        {
            if (element is not StepRow row || scenario == null || scenario.targetObject == null)
                return;

            int stepIndex = IsFiltering ? filteredIndices[rowIndex] : rowIndex;
            SerializedProperty steps = ScenarioEditOperations.GetSteps(scenario);
            if (stepIndex >= steps.arraySize)
                return;

            SerializedProperty element0 = steps.GetArrayElementAtIndex(stepIndex);
            var step = element0.managedReferenceValue as ScenarioStep;
            string missingType = step == null ? MissingTypes.GetTypeNameAt((ScenarioDefinition)scenario.targetObject, stepIndex) : null;
            row.Bind(stepIndex, step, missingType);
            DecorateRow?.Invoke(row);
        }

        void RebuildFilter()
        {
            filteredIndices.Clear();
            SerializedProperty steps = ScenarioEditOperations.GetSteps(scenario);
            for (int i = 0; i < steps.arraySize; i++)
            {
                var step = steps.GetArrayElementAtIndex(i).managedReferenceValue as ScenarioStep;
                if (Matches(step))
                    filteredIndices.Add(i);
            }
        }

        bool Matches(ScenarioStep step)
        {
            if (step == null)
                return "missing".IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

            StepTypeInfo info = StepTypeCatalog.Describe(step.GetType());
            return Contains(step.Title) || Contains(info.DisplayName) || Contains(step.GetType().Name) || Contains(info.Category);
        }

        bool Contains(string text) => !string.IsNullOrEmpty(text) && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        void UpdateHeader()
        {
            int count = scenario != null && scenario.targetObject != null ? ScenarioEditOperations.GetSteps(scenario).arraySize : 0;
            header.text = IsFiltering ? $"STEPS ({filteredIndices.Count} of {count})" : $"STEPS ({count})";
        }

        void OnSelectedIndicesChanged(IEnumerable<int> rows)
        {
            foreach (int row in rows)
            {
                int stepIndex = IsFiltering ? filteredIndices[row] : row;
                StepSelected?.Invoke(stepIndex);
                return;
            }
        }

        void OnItemIndexChanged(int from, int to)
        {
            ScenarioEditOperations.NameCurrentUndoGroup(ScenarioEditOperations.ReorderStepsUndo);
            StepMoved?.Invoke(from, to);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.altKey && (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow))
            {
                CommandRequested?.Invoke(evt.keyCode == KeyCode.UpArrow ? StepListCommand.MoveUp : StepListCommand.MoveDown);
                evt.StopImmediatePropagation();
                return;
            }

            if ((evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) && !evt.actionKey && !evt.altKey && !evt.shiftKey)
            {
                RequestDelete();
                evt.StopImmediatePropagation();
            }
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (ToCommand(evt.commandName).HasValue)
                evt.StopPropagation();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            StepListCommand? command = ToCommand(evt.commandName);
            if (!command.HasValue)
                return;

            if (command.Value == StepListCommand.Delete)
                RequestDelete();
            else
                CommandRequested?.Invoke(command.Value);

            evt.StopPropagation();
        }

        // The Delete key can arrive both as a key event and as a "SoftDelete" command; act only once.
        void RequestDelete()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastDeleteTime < 0.15)
                return;

            lastDeleteTime = now;
            CommandRequested?.Invoke(StepListCommand.Delete);
        }

        static StepListCommand? ToCommand(string commandName)
        {
            return commandName switch
            {
                "Duplicate" => StepListCommand.Duplicate,
                "Copy" => StepListCommand.Copy,
                "Paste" => StepListCommand.Paste,
                "Delete" or "SoftDelete" => StepListCommand.Delete,
                _ => null
            };
        }
    }
}
