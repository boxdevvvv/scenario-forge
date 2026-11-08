using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>Shows a step picker (from the runner's scenario) instead of a raw step id.</summary>
    [CustomPropertyDrawer(typeof(StepEventBinding))]
    sealed class StepEventBindingDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.marginBottom = 6;

            SerializedProperty stepId = property.FindPropertyRelative("stepId");
            ScenarioDefinition definition = StepChoices.FindDefinition(property.serializedObject.targetObject);

            if (definition == null)
            {
                root.Add(new HelpBox("No ScenarioRunnerBehaviour with a Scenario Definition was found. Assign Runner (or add a runner to the scene) to pick a step from a list.", HelpBoxMessageType.Warning));
                root.Add(new PropertyField(stepId, "Step Id"));
            }
            else
            {
                List<StepChoices.Choice> choices = StepChoices.Build(definition);
                var labels = new List<string> { StepChoices.NoneLabel };
                labels.AddRange(choices.Select(choice => choice.Label));
                string current = StepChoices.LabelFor(choices, stepId.stringValue);
                if (!labels.Contains(current))
                    labels.Add(current);

                var dropdown = new DropdownField("Step", labels, current) { name = "stepDropdown" };
                dropdown.tooltip = $"A step of \"{definition.name}\".";
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    int index = choices.FindIndex(choice => choice.Label == evt.newValue);
                    if (index < 0 && evt.newValue != StepChoices.NoneLabel)
                        return;

                    stepId.serializedObject.Update();
                    stepId.stringValue = index >= 0 ? choices[index].Id : string.Empty;
                    stepId.serializedObject.ApplyModifiedProperties();
                });
                root.Add(dropdown);
            }

            root.Add(new PropertyField(property.FindPropertyRelative("onStarted"), "On Started"));
            root.Add(new PropertyField(property.FindPropertyRelative("onEnded"), "On Ended"));
            return root;
        }
    }
}
