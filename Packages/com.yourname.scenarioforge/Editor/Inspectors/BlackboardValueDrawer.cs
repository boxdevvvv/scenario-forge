using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ScenarioForge.Editor
{
    /// <summary>Draws a <see cref="BlackboardValue"/> as a type selector plus only the field of that type.</summary>
    [CustomPropertyDrawer(typeof(BlackboardValue))]
    sealed class BlackboardValueDrawer : PropertyDrawer
    {
        internal const string TypeField = "type";
        internal static readonly string[] ValueFields = { "boolValue", "intValue", "floatValue", "stringValue" };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.AddToClassList("sf-blackboard-value");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackagePaths.StyleSheet);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            SerializedProperty typeProperty = property.FindPropertyRelative(TypeField);
            var typeField = new PropertyField(typeProperty, property.displayName + " Type");
            root.Add(typeField);

            var valueFields = new PropertyField[ValueFields.Length];
            for (int i = 0; i < ValueFields.Length; i++)
            {
                valueFields[i] = new PropertyField(property.FindPropertyRelative(ValueFields[i]), property.displayName);
                valueFields[i].name = ValueFields[i];
                root.Add(valueFields[i]);
            }

            void Refresh(int typeIndex)
            {
                for (int i = 0; i < valueFields.Length; i++)
                    valueFields[i].EnableInClassList("sf-hidden", i != typeIndex);
            }

            Refresh(typeProperty.enumValueIndex);
            root.TrackPropertyValue(typeProperty, changed => Refresh(changed.enumValueIndex));
            return root;
        }
    }
}
