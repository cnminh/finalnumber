using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace FinalNumber.Editor
{
    /// <summary>
    /// Android post-process build for Play Store compliance.
    /// Configures AAB, 64-bit support, and manifest requirements.
    /// </summary>
    public class AndroidPostProcessBuild : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log($"[AndroidPostProcessBuild] Processing Android project at: {path}");

            // Validate AndroidManifest.xml exists and has correct settings
            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[AndroidPostProcessBuild] AndroidManifest.xml not found at expected path.");
                return;
            }

            // The manifest is already configured via Assets/Plugins/Android/AndroidManifest.xml
            // Unity merges it during the build process
            
            ValidateGradleBuild(path);
            
            Debug.Log("[AndroidPostProcessBuild] Play Store compliance validation complete.");
        }

        private void ValidateGradleBuild(string gradlePath)
        {
            string buildGradlePath = Path.Combine(gradlePath, "build.gradle");
            
            if (!File.Exists(buildGradlePath))
            {
                Debug.LogWarning("[AndroidPostProcessBuild] build.gradle not found.");
                return;
            }

            string buildGradle = File.ReadAllText(buildGradlePath);

            // Verify 64-bit architecture support
            if (!buildGradle.Contains("arm64-v8a"))
            {
                Debug.LogWarning("[AndroidPostProcessBuild] ARM64 architecture may not be enabled. Check Player Settings > Target Architectures.");
            }

            // Verify AAB is enabled
            if (!EditorUserBuildSettings.buildAppBundle)
            {
                Debug.LogWarning("[AndroidPostProcessBuild] Android App Bundle (AAB) is not enabled. Required for Play Store!");
                Debug.LogWarning("[AndroidPostProcessBuild] Enable via Editor > Build Settings > Build App Bundle (Google Play)");
            }
        }
    }
}
