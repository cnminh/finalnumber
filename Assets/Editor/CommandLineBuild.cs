using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FinalNumber.Editor
{
    /// <summary>
    /// Command-line build interface for CI/CD pipelines.
    /// Supports headless builds via game-ci/unity-actions.
    /// </summary>
    public static class CommandLineBuild
    {
        // Build target constants
        private const string OUTPUT_DIR = "build";
        
        /// <summary>
        /// Build Android AAB and APK from command line.
        /// Usage: unity -quit -batchmode -executeMethod FinalNumber.Editor.CommandLineBuild.BuildAndroid -version 1.0.0 -buildNumber 42 -buildType Release
        /// </summary>
        public static void BuildAndroid()
        {
            try
            {
                var args = ParseCommandLineArgs();
                
                Debug.Log($"[CommandLineBuild] Starting Android build...");
                Debug.Log($"[CommandLineBuild] Version: {args.version}, Build: {args.buildNumber}, Type: {args.buildType}");

                // Apply build configuration
                ApplyBuildConfig(BuildTarget.Android, args);

                // Setup Android-specific settings
                EditorUserBuildSettings.androidBuildType = args.buildType == "Release" 
                    ? AndroidBuildType.Release 
                    : AndroidBuildType.Debug;
                
                // Enable AAB (Android App Bundle) for Play Store
                EditorUserBuildSettings.buildAppBundle = true;
                
                // Set IL2CPP for better performance and 64-bit support
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
                
                // Set Android target architectures (ARMv7 and ARM64 for maximum compatibility)
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                
                // Ensure minimum API level supports both architectures
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;

                // Build paths
                string buildDir = Path.Combine(OUTPUT_DIR, "Android");
                Directory.CreateDirectory(buildDir);
                
                string aabPath = Path.Combine(buildDir, $"FinalNumber-{args.version}-{args.buildNumber}.aab");
                string apkPath = Path.Combine(buildDir, $"FinalNumber-{args.version}-{args.buildNumber}.apk");

                // Build AAB
                BuildReport aabReport = BuildPipeline.BuildPlayer(
                    GetEnabledScenes(),
                    aabPath,
                    BuildTarget.Android,
                    BuildOptions.None
                );

                if (aabReport.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[CommandLineBuild] AAB build failed!");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"[CommandLineBuild] AAB build succeeded: {aabPath}");

                // Also build APK for local testing (optional, only for dev builds)
                if (args.buildType == "Development")
                {
                    EditorUserBuildSettings.buildAppBundle = false;
                    BuildReport apkReport = BuildPipeline.BuildPlayer(
                        GetEnabledScenes(),
                        apkPath,
                        BuildTarget.Android,
                        BuildOptions.None
                    );

                    if (apkReport.summary.result == BuildResult.Succeeded)
                    {
                        Debug.Log($"[CommandLineBuild] APK build succeeded: {apkPath}");
                    }
                }

                Debug.Log($"[CommandLineBuild] Android build completed successfully!");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandLineBuild] Build failed with exception: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Build WebGL for browser testing from command line.
        /// Usage: unity -quit -batchmode -executeMethod FinalNumber.Editor.CommandLineBuild.BuildWebGL -version 1.0.0 -buildNumber 42 -buildType Development
        /// </summary>
        public static void BuildWebGL()
        {
            try
            {
                var args = ParseCommandLineArgs();
                
                Debug.Log($"[CommandLineBuild] Starting WebGL build...");
                Debug.Log($"[CommandLineBuild] Version: {args.version}, Build: {args.buildNumber}, Type: {args.buildType}");

                // Apply build configuration
                ApplyBuildConfig(BuildTarget.WebGL, args);

                // Setup WebGL-specific settings
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.exceptionSupport = args.buildType == "Development" 
                    ? WebGLExceptionSupport.FullWithStacktrace 
                    : WebGLExceptionSupport.None;

                // Build path
                string buildDir = Path.Combine(OUTPUT_DIR, "WebGL");
                Directory.CreateDirectory(buildDir);

                BuildReport report = BuildPipeline.BuildPlayer(
                    GetEnabledScenes(),
                    buildDir,
                    BuildTarget.WebGL,
                    args.buildType == "Development" ? BuildOptions.Development : BuildOptions.None
                );

                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[CommandLineBuild] WebGL build failed!");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"[CommandLineBuild] WebGL build succeeded: {buildDir}");
                Debug.Log($"[CommandLineBuild] Open {buildDir}/index.html in a browser to test.");
                
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandLineBuild] Build failed with exception: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Build iOS Xcode project from command line.
        /// Usage: unity -quit -batchmode -executeMethod FinalNumber.Editor.CommandLineBuild.BuildiOS -version 1.0.0 -buildNumber 42 -buildType Release
        /// </summary>
        public static void BuildiOS()
        {
            try
            {
                var args = ParseCommandLineArgs();
                
                Debug.Log($"[CommandLineBuild] Starting iOS build...");
                Debug.Log($"[CommandLineBuild] Version: {args.version}, Build: {args.buildNumber}, Type: {args.buildType}");

                // Apply build configuration
                ApplyBuildConfig(BuildTarget.iOS, args);

                // Setup iOS-specific settings
                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
                
                // Set IL2CPP for iOS (required for App Store)
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.Mono2x);
                
                // Enable bitcode (Apple requirement for some architectures)
                PlayerSettings.SetAdditionalIl2CppArgs("--compiler-flags=\"-fembed-bitcode\"");

                // Build path
                string buildDir = Path.Combine(OUTPUT_DIR, "iOS");
                Directory.CreateDirectory(buildDir);

                BuildReport report = BuildPipeline.BuildPlayer(
                    GetEnabledScenes(),
                    buildDir,
                    BuildTarget.iOS,
                    BuildOptions.None
                );

                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[CommandLineBuild] iOS build failed!");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"[CommandLineBuild] iOS build succeeded: {buildDir}");
                Debug.Log($"[CommandLineBuild] Open the Xcode project and archive for App Store submission.");
                
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandLineBuild] Build failed with exception: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Parse command line arguments passed to Unity.
        /// </summary>
        private static BuildArgs ParseCommandLineArgs()
        {
            var args = new BuildArgs
            {
                version = GetArg("-version") ?? "1.0.0",
                buildNumber = GetArg("-buildNumber") ?? "1",
                buildType = GetArg("-buildType") ?? "Development"
            };

            return args;
        }

        /// <summary>
        /// Get a specific argument from command line.
        /// </summary>
        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Apply version and build settings to PlayerSettings.
        /// </summary>
        private static void ApplyBuildConfig(BuildTarget target, BuildArgs args)
        {
            // Parse version components
            var versionParts = args.version.Split('.');
            int major = versionParts.Length > 0 ? int.Parse(versionParts[0]) : 1;
            int minor = versionParts.Length > 1 ? int.Parse(versionParts[1]) : 0;
            int patch = versionParts.Length > 2 ? int.Parse(versionParts[2]) : 0;
            int buildNum = int.Parse(args.buildNumber);

            // Calculate Android version code
            int versionCode = major * 1000000 + minor * 10000 + patch * 100 + (buildNum % 100);

            // Set bundle ID with build type suffix
            string bundleSuffix = args.buildType switch
            {
                "Development" => ".dev",
                "Beta" => ".beta",
                "Release" => "",
                _ => ""
            };
            PlayerSettings.applicationIdentifier = $"com.finalnumber.game{bundleSuffix}";

            switch (target)
            {
                case BuildTarget.Android:
                    PlayerSettings.bundleVersion = args.version;
                    PlayerSettings.Android.bundleVersionCode = versionCode;
                    
                    // Product name with build type indicator
                    PlayerSettings.productName = args.buildType == "Release" 
                        ? "Final Number" 
                        : $"Final Number [{args.buildType}]";
                    break;

                case BuildTarget.iOS:
                    PlayerSettings.bundleVersion = args.version;
                    PlayerSettings.iOS.buildNumber = args.buildNumber;
                    
                    PlayerSettings.productName = args.buildType == "Release" 
                        ? "Final Number" 
                        : $"Final Number [{args.buildType}]";
                    break;

                case BuildTarget.WebGL:
                    PlayerSettings.bundleVersion = args.version;
                    
                    PlayerSettings.productName = args.buildType == "Release" 
                        ? "Final Number" 
                        : $"Final Number [{args.buildType}]";
                    break;
            }

            Debug.Log($"[CommandLineBuild] Applied config: BundleID={PlayerSettings.applicationIdentifier}, Version={PlayerSettings.bundleVersion}");
        }

        /// <summary>
        /// Get all enabled scenes from Build Settings.
        /// </summary>
        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        /// <summary>
        /// Build arguments container.
        /// </summary>
        private class BuildArgs
        {
            public string version;
            public string buildNumber;
            public string buildType;
        }
    }
}
