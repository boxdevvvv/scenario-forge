using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>Commands from the details header menu.</summary>
    enum StepDetailsCommand
    {
        ChangeType,
        Copy,
        Duplicate,
        Delete,
        ResetToDefaults,
        RemoveMissing
    }

    /// <summary>
    /// The right panel: header with type and category, and one <see cref="PropertyField"/> per field of the
    /// selected step. It is rebuilt from scratch every time, never re-bound, to avoid stale managed reference
    /// bindings.
    /// </summary>
    sealed class StepDetailsPanel : VisualElement
    {
        readonly Label sectionHeader = new("STEP DETAILS");
        readonly VisualElement header = new();
        readonly Label typeLabel = new();
        readonly Button menuButton;
        readonly ScrollView body = new(ScrollViewMode.Vertical);
        readonly Label placeholder = new("Select a step to edit it.");

        public StepDetailsPanel()
        {
            AddToClassList("sf-details-panel");
            sectionHeader.AddToClassList("sf-section-header");
            header.AddToClassList("sf-details__header");
            typeLabel.AddToClassList("sf-details__type");
            body.AddToClassList("sf-details__body");
            placeholder.AddToClassList("sf-placeholder");

            menuButton = new Button(ShowMenu) { text = "⋮", tooltip = "Step actions" };
            menuButton.AddToClassList("sf-details__menu");

            header.Add(typeLabel);
            header.Add(menuButton);
            Add(sectionHeader);
            Add(header);
            Add(body);
            Add(placeholder);
            ShowNothing();
        }

        /// <summary>Raised when the user picks a header menu command.</summary>
        public event Action<StepDetailsCommand> CommandRequested;

        /// <summary>Whether the panel currently shows a valid step.</summary>
        public bool HasStep { get; private set; }

        /// <summary>The <see cref="PropertyField"/>s of the last build, for tests.</summary>
        public int FieldCount => body.childCount;

        /// <summary>Builds the panel for the step at <paramref name="index"/>, creating new fields.</summary>
        public void Show(SerializedObject scenario, int index)
        {
            body.Clear();
            HasStep = false;

            if (scenario == null || scenario.targetObject == null)
            {
                ShowNothing();
                return;
            }

            scenario.Update();
            SerializedProperty steps = ScenarioEditOperations.GetSteps(scenario);
            if (index < 0 || index >= steps.arraySize)
            {
                ShowNothing();
                return;
            }

            SetVisible(true);
            SerializedProperty element = steps.GetArrayElementAtIndex(index);
            if (element.managedReferenceValue is not ScenarioStep step)
            {
                ShowMissing(scenario, element);
                return;
            }

            HasStep = true;
            StepTypeInfo info = StepTypeCatalog.Describe(step.GetType());
            typeLabel.text = $"{info.DisplayName}  ({info.Category})";
            typeLabel.tooltip = string.IsNullOrEmpty(info.Tooltip) ? step.GetType().FullName : info.Tooltip;

            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                var field = new PropertyField(iterator.Copy());
                field.Bind(scenario);
                body.Add(field);
            }
        }

        public void ShowNothing()
        {
            body.Clear();
            HasStep = false;
            SetVisible(false);
        }

        void ShowMissing(SerializedObject scenario, SerializedProperty element)
        {
            string typeName = MissingTypes.GetTypeName(scenario.targetObject, element.managedReferenceId) ?? "(unknown)";
            typeLabel.text = "Missing type";
            typeLabel.tooltip = typeName;

            var message = new HelpBox(
                $"Missing type: {typeName}.\nThe class was deleted or renamed. Restore it, add [MovedFrom] to the renamed class, or remove this step.",
                HelpBoxMessageType.Error);
            var remove = new Button(() => CommandRequested?.Invoke(StepDetailsCommand.RemoveMissing)) { text = "Remove Step" };
            remove.AddToClassList("sf-details__remove");
            body.Add(message);
            body.Add(remove);
        }

        void SetVisible(bool hasSelection)
        {
            header.EnableInClassList("sf-hidden", !hasSelection);
            body.EnableInClassList("sf-hidden", !hasSelection);
            placeholder.EnableInClassList("sf-hidden", hasSelection);
        }

        void ShowMenu()
        {
            var menu = new GenericDropdownMenu();
            if (HasStep)
            {
                menu.AddItem("Change Type…", false, () => CommandRequested?.Invoke(StepDetailsCommand.ChangeType));
                menu.AddItem("Copy", false, () => CommandRequested?.Invoke(StepDetailsCommand.Copy));
                menu.AddItem("Duplicate", false, () => CommandRequested?.Invoke(StepDetailsCommand.Duplicate));
                menu.AddItem("Reset to Defaults", false, () => CommandRequested?.Invoke(StepDetailsCommand.ResetToDefaults));
                menu.AddSeparator(string.Empty);
            }

            menu.AddItem("Delete", false, () => CommandRequested?.Invoke(StepDetailsCommand.Delete));
            menu.DropDown(menuButton.worldBound, menuButton, DropdownMenuSizeMode.Auto);
        }

        /// <summary>Where to open pickers that belong to this panel.</summary>
        public UnityEngine.Rect MenuButtonRect => menuButton.worldBound;
    }
}
