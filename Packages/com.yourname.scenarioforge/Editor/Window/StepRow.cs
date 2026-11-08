using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>One row of the step list: number, category badge, title, type and issue icons.</summary>
    sealed class StepRow : VisualElement
    {
        public const string UssClass = "sf-row";
        const string MissingClass = "sf-row--missing";
        const string PlaceholderClass = "sf-row__title--placeholder";

        readonly Label indexLabel = new();
        readonly Label badge = new();
        readonly Label titleLabel = new();
        readonly Label typeLabel = new();
        string badgeClass;

        public StepRow()
        {
            AddToClassList(UssClass);
            indexLabel.AddToClassList("sf-row__index");
            badge.AddToClassList("sf-row__badge");
            titleLabel.AddToClassList("sf-row__title");
            typeLabel.AddToClassList("sf-row__type");
            Issues = new VisualElement();
            Issues.AddToClassList("sf-row__issues");
            State = new Label();
            State.AddToClassList("sf-row__state");

            Add(indexLabel);
            Add(badge);
            Add(titleLabel);
            Add(typeLabel);
            Add(Issues);
            Add(State);
        }

        /// <summary>The scenario index of the step shown in this row.</summary>
        public int StepIndex { get; private set; } = -1;

        /// <summary>Container for validation icons.</summary>
        public VisualElement Issues { get; }

        /// <summary>Runtime state shown by the Play Mode debugger.</summary>
        public Label State { get; }

        public void Bind(int stepIndex, ScenarioStep step, string missingTypeName)
        {
            StepIndex = stepIndex;
            indexLabel.text = (stepIndex + 1).ToString();

            if (step == null)
            {
                AddToClassList(MissingClass);
                SetBadge("missing", "!");
                titleLabel.text = "Missing type: " + (string.IsNullOrEmpty(missingTypeName) ? "(unknown)" : missingTypeName);
                titleLabel.RemoveFromClassList(PlaceholderClass);
                typeLabel.text = string.Empty;
                return;
            }

            RemoveFromClassList(MissingClass);
            StepTypeInfo info = StepTypeCatalog.Describe(step.GetType());
            SetBadge(info.RootCategory.ToLowerInvariant(), info.RootCategory.Length > 0 ? info.RootCategory.Substring(0, 1).ToUpperInvariant() : "?");
            badge.tooltip = info.Category;

            bool hasTitle = !string.IsNullOrWhiteSpace(step.Title);
            titleLabel.text = hasTitle ? step.Title : info.DisplayName;
            titleLabel.EnableInClassList(PlaceholderClass, !hasTitle);
            typeLabel.text = info.DisplayName;
        }

        void SetBadge(string category, string text)
        {
            if (badgeClass != null)
                badge.RemoveFromClassList(badgeClass);

            badgeClass = "sf-row__badge--" + category switch
            {
                "flow" or "signals" or "spatial" or "blackboard" or "missing" => category,
                _ => "other"
            };
            badge.AddToClassList(badgeClass);
            badge.text = text;
        }
    }
}
