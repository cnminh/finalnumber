using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using FinalNumber.Build;

namespace FinalNumber.Editor
{
    /// <summary>
    /// Pre-build validation and version synchronization.
    /// Ensures version numbers are consistent across platforms.
    /// </summary>
    public class BuildPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100; // Run early

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = LoadBuildConfig();
            if (config == null)
            {
                Debug.LogWarning("[BuildPreprocess] No BuildConfig found. Create one via Assets > Create > Final Number > Build Configuration");
                return;
            }

            // Validate for store submission
            if (config.buildType == FinalNumber.Build.BuildConfig.BuildType.Release)
            {
                if (!config.IsValidForStore())
                {
                    throw new BuildFailedException("Build validation failed. Check console for details.");
                }
            }

            // Apply version to PlayerSettings
            ApplyVersionSettings(config, report.summary.platform);

            Debug.Log($"[BuildPreprocess] Building version {config.FullVersion} for {report.summary.platform}");
        }

        private BuildConfig LoadBuildConfig()
        {
            var guids = AssetDatabase.FindAssets("t:BuildConfig");
            if (guids.Length == 0) return null;
            
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BuildConfig>(path);
        }

        private void ApplyVersionSettings(BuildConfig config, BuildTarget platform)
        {
            // Always set product name
            PlayerSettings.productName = config.GetProductName();

            switch (platform)
            {
                case BuildTarget.iOS:
                    PlayerSettings.bundleVersion = config.iOSBundleVersion;
                    PlayerSettings.iOS.buildNumber = config.iOSBuildNumber;
                    PlayerSettings.applicationIdentifier = config.GetBundleId();
                    PlayerSettings.iOS.targetOSVersionString = config.iOSTargetVersion;
                    PlayerSettings.iOS.appleDeveloperTeamID = ""; // Set via external config or UI
                    
                    break;

                case BuildTarget.Android:
                    PlayerSettings.bundleVersion = config.AndroidVersionName;
                    PlayerSettings.Android.bundleVersionCode = config.AndroidVersionCode;
                    PlayerSettings.applicationIdentifier = config.GetBundleId();
                    PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)config.androidMinSdk;
                    PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)config.androidTargetSdk;
                    
                    // ARM64 only for smaller builds (ARMv7 deprecated on Play Store for new apps)
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    
                    // Android App Bundle (AAB) - required for Play Store, smaller than APK
                    EditorUserBuildSettings.buildAppBundle = true;
                    
                    break;

                case BuildTarget.StandaloneOSX:
                    PlayerSettings.bundleVersion = config.iOSBundleVersion;
                    PlayerSettings.macOS.buildNumber = config.iOSBuildNumber;
                    PlayerSettings.applicationIdentifier = config.GetBundleId();
                    break;
            }
        }
    }
}
