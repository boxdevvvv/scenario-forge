using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScenarioForge.Dev
{
    /// <summary>
    /// Development only (not part of the package): prepares the project for each portfolio recording
    /// (docs/portfolio/recording-guide.md). Every shot works on a fresh copy in Assets/Dev/Portfolio,
    /// so recordings never change the demo or sample assets.
    /// </summary>
    static class PortfolioShots
    {
        const string Folder = "Assets/Dev/Portfolio";
        const string FireDrill = "Assets/Dev/Demo/FireDrillScenario.asset";
        const string DemoScene = "Assets/Dev/Demo/FireDrillDemo.unity";

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/2 - Reorder Shot")]
        static void ReorderShot()
        {
            string path = CopyFireDrill("ReorderShot.asset");
            OpenInWindow(path);
        }

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/3 - Validation Shot")]
        static void ValidationShot()
        {
            string path = CopyFireDrill("ValidationShot.asset");
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var serialized = new SerializedObject(asset);
            SerializedProperty steps = serialized.FindProperty("steps");
            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(i);
                if (!step.managedReferenceFullTypename.EndsWith("WaitForSignalStep"))
                    continue;

                step.FindPropertyRelative("signal").stringValue = string.Empty;
                break;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            OpenInWindow(path);
        }

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/4 - Debugger Shot")]
        static void DebuggerShot()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(DemoScene);
            OpenInWindow(FireDrill);
        }

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/Delete Shot Copies")]
        static void DeleteCopies()
        {
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        static string CopyFireDrill(string fileName)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Dev", "Portfolio");

            string path = $"{Folder}/{fileName}";
            AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(FireDrill, path))
                throw new IOException($"Could not copy '{FireDrill}' to '{path}'. Check that the demo asset exists.");

            return path;
        }

        static void OpenInWindow(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }
    }
}
