using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Dev
{
    /// <summary>
    /// Development only (not part of the package): copies the sample from Assets/Samples to the package's
    /// Samples~ folder, which Unity hides from the project, keeping the .meta files so references survive.
    /// </summary>
    static class SampleSync
    {
        internal const string Source = "Assets/Samples/ScenarioForge/AssemblyProcedure";
        internal const string Destination = "Packages/com.boxdevvvv.scenarioforge/Samples~/AssemblyProcedure";

        [MenuItem("Tools/Scenario Forge/Dev/Sync Sample To Package")]
        static void SyncMenu()
        {
            int files = Sync();
            Debug.Log($"[Scenario Forge] Synced the sample: {files} files copied from {Source} to {Destination}.");
        }

        /// <summary>Replaces the destination with a copy of the source. Returns how many files were copied.</summary>
        internal static int Sync()
        {
            if (!Directory.Exists(Source))
                throw new DirectoryNotFoundException($"The sample source folder '{Source}' does not exist. Restore it before syncing.");

            AssetDatabase.SaveAssets();
            if (Directory.Exists(Destination))
                Directory.Delete(Destination, true);

            int count = 0;
            foreach (string file in Directory.GetFiles(Source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(Source, file);
                string target = Path.Combine(Destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target);
                count++;
            }

            return count;
        }
    }
}
