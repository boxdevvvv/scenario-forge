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

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/1 - Create Shot")]
        static void CreateShot()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Dev", "Portfolio");

            string path = $"{Folder}/Lockout Procedure.asset";
            AssetDatabase.DeleteAsset(path);
            var template = AssetDatabase.LoadMainAssetAtPath(FireDrill);
            var scenario = ScriptableObject.CreateInstance(template.GetType());
            AssetDatabase.CreateAsset(scenario, path);
            AssetDatabase.SaveAssets();
            OpenInWindow(path);
        }

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/2 - Reorder Shot")]
        static void ReorderShot()
        {
            if (!OpenDemoScene())
                return;

            string path = CopyFireDrill("ReorderShot.asset");
            OpenInWindow(path);
        }

        [MenuItem("Tools/Scenario Forge/Dev/Portfolio/3 - Validation Shot")]
        static void ValidationShot()
        {
            if (!OpenDemoScene())
                return;

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
            if (!OpenDemoScene())
                return;

            OpenInWindow(FireDrill, new Rect(10, 120, 860, 580));
            OpenFloatingGameView(new Rect(880, 120, 550, 580));
        }

        // A floating Game view sized for the recording, next to the Scenario Forge window.
        static void OpenFloatingGameView(Rect position)
        {
            System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            foreach (EditorWindow open in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                if (!open.docked)
                    open.Close();
            }

            var gameView = (EditorWindow)ScriptableObject.CreateInstance(gameViewType);
            gameView.Show();
            gameView.position = position;
        }

        // The demo scene holds the scenario's targets, so only the shot's own issue shows in validation.
        static bool OpenDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(DemoScene);
            return true;
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

        // Opens a fresh Scenario Forge window with the same size for every shot, so recordings line up.
        static void OpenInWindow(string path) => OpenInWindow(path, new Rect(240, 120, 900, 580));

        static void OpenInWindow(string path, Rect position)
        {
            foreach (EditorWindow open in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (open.GetType().Name == "ScenarioForgeWindow")
                    open.Close();
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
            foreach (EditorWindow open in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (open.GetType().Name == "ScenarioForgeWindow")
                    open.position = position;
            }
        }
    }
}
