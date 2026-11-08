using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>
    /// Inspector of <see cref="ScenarioDefinition"/>: title, description, a step summary and a button that
    /// opens the asset in the Scenario Forge window. Steps are edited only in the window.
    /// </summary>
    [CustomEditor(typeof(ScenarioDefinition))]
    sealed class ScenarioDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackagePaths.StyleSheet);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            root.Add(new PropertyField(serializedObject.FindProperty("title")));
            root.Add(new PropertyField(serializedObject.FindProperty("description")));

            var summary = new Label();
            summary.AddToClassList("sf-inspector__summary");
            root.Add(summary);

            var open = new Button(() => ScenarioForgeWindow.Open((ScenarioDefinition)target)) { text = "Open in Scenario Forge" };
            open.AddToClassList("sf-inspector__open");
            root.Add(open);

            UpdateSummary(summary);
            root.TrackSerializedObjectValue(serializedObject, _ => UpdateSummary(summary));
            return root;
        }

        void UpdateSummary(Label summary)
        {
            serializedObject.Update();
            int count = serializedObject.FindProperty(ScenarioDefinition.StepsPropertyName).arraySize;
            summary.text = count == 1 ? "1 step" : $"{count} steps";
        }
    }
}
