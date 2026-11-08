using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// The collapsible bottom panel of the window: counts per severity, severity filters and the list of issues.
    /// Clicking an issue selects its step.
    /// </summary>
    sealed class ValidationPanel : VisualElement
    {
        const string CollapsedPrefKey = "ScenarioForge.ValidationCollapsed";

        readonly Button collapseButton;
        readonly Label errorCount = new();
        readonly Label warningCount = new();
        readonly Label infoCount = new();
        readonly ToolbarToggle showErrors = new() { text = "Errors", value = true };
        readonly ToolbarToggle showWarnings = new() { text = "Warnings", value = true };
        readonly ToolbarToggle showInfo = new() { text = "Info", value = true };
        readonly ScrollView list = new(ScrollViewMode.Vertical);
        readonly Label emptyLabel = new("No problems found.");

        ValidationReport report;
        ScenarioDefinition definition;
        bool collapsed;

        public ValidationPanel()
        {
            AddToClassList("sf-validation");

            var header = new VisualElement();
            header.AddToClassList("sf-validation__header");
            collapseButton = new Button(ToggleCollapsed);
            collapseButton.AddToClassList("sf-validation__collapse");
            header.Add(collapseButton);

            header.Add(CountBadge(errorCount, "console.erroricon.sml"));
            header.Add(CountBadge(warningCount, "console.warnicon.sml"));
            header.Add(CountBadge(infoCount, "console.infoicon.sml"));

            var spacer = new VisualElement();
            spacer.AddToClassList("sf-validation__spacer");
            header.Add(spacer);

            foreach (ToolbarToggle toggle in new[] { showErrors, showWarnings, showInfo })
            {
                toggle.AddToClassList("sf-validation__filter");
                toggle.RegisterValueChangedCallback(_ => Rebuild());
                header.Add(toggle);
            }

            list.AddToClassList("sf-validation__list");
            emptyLabel.AddToClassList("sf-validation__empty");
            Add(header);
            Add(list);

            collapsed = EditorPrefs.GetBool(CollapsedPrefKey, false);
            ApplyCollapsed();
            SetReport(null, null);
        }

        /// <summary>Raised with the step index when the user clicks an issue that belongs to a step.</summary>
        public event Action<int> IssueClicked;

        /// <summary>Number of issue rows shown with the current filters, for tests.</summary>
        public int VisibleIssueCount => list.Query(className: "sf-validation__issue").ToList().Count;

        public bool ShowErrors
        {
            get => showErrors.value;
            set => showErrors.value = value;
        }

        public bool ShowWarnings
        {
            get => showWarnings.value;
            set => showWarnings.value = value;
        }

        public bool ShowInfo
        {
            get => showInfo.value;
            set => showInfo.value = value;
        }

        public void SetReport(ValidationReport newReport, ScenarioDefinition scenario)
        {
            report = newReport;
            definition = scenario;
            int errors = report?.Count(ValidationSeverity.Error) ?? 0;
            int warnings = report?.Count(ValidationSeverity.Warning) ?? 0;
            int infos = report?.Count(ValidationSeverity.Info) ?? 0;
            errorCount.text = errors == 1 ? "1 error" : $"{errors} errors";
            warningCount.text = warnings == 1 ? "1 warning" : $"{warnings} warnings";
            infoCount.text = $"{infos} info";
            Rebuild();
        }

        void Rebuild()
        {
            list.Clear();
            if (report == null)
                return;

            foreach (ValidationIssue issue in report.Issues)
            {
                if (!IsVisible(issue.Severity))
                    continue;

                list.Add(CreateIssueRow(issue));
            }

            if (list.childCount == 0)
                list.Add(emptyLabel);
        }

        bool IsVisible(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => showErrors.value,
                ValidationSeverity.Warning => showWarnings.value,
                _ => showInfo.value
            };
        }

        VisualElement CreateIssueRow(ValidationIssue issue)
        {
            var row = new VisualElement();
            row.AddToClassList("sf-validation__issue");
            row.Add(Icon(SeverityIcon(issue.Severity)));
            var text = new Label(EditorScenarioValidation.FormatIssue(definition, issue));
            text.AddToClassList("sf-validation__message");
            row.Add(text);

            if (issue.StepIndex >= 0)
            {
                row.AddToClassList("sf-validation__issue--clickable");
                row.tooltip = "Click to select the step.";
                row.RegisterCallback<ClickEvent>(_ => IssueClicked?.Invoke(issue.StepIndex));
            }

            return row;
        }

        void ToggleCollapsed()
        {
            collapsed = !collapsed;
            EditorPrefs.SetBool(CollapsedPrefKey, collapsed);
            ApplyCollapsed();
        }

        void ApplyCollapsed()
        {
            collapseButton.text = collapsed ? "▸ VALIDATION" : "▾ VALIDATION";
            list.EnableInClassList("sf-hidden", collapsed);
            EnableInClassList("sf-validation--collapsed", collapsed);
        }

        internal static string SeverityIcon(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => "console.erroricon.sml",
                ValidationSeverity.Warning => "console.warnicon.sml",
                _ => "console.infoicon.sml"
            };
        }

        internal static Image Icon(string iconName)
        {
            var image = new Image { image = EditorGUIUtility.FindTexture(iconName), scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList("sf-icon");
            return image;
        }

        static VisualElement CountBadge(Label label, string iconName)
        {
            var badge = new VisualElement();
            badge.AddToClassList("sf-validation__count");
            badge.Add(Icon(iconName));
            badge.Add(label);
            return badge;
        }
    }
}
